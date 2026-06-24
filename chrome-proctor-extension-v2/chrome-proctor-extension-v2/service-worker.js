/**
 * Risebird Proctoring — MV3 service worker.
 * Coordinates session state, DNR rules, backend posts, heartbeat, and screenshots.
 */
importScripts('capture-schedule.js', 'proctoring-flow-state.js');

/** Backend API paths (appended to /api/proctoring/session). All requests use Bearer sessionToken. */
const PROCTORING_API_BASE = '/api/proctoring/session';
const PROCTORING_API = {
  join: '/join',
  start: '/start',
  event: '/event',
  screenshot: '/screenshot',
  screenChunk: '/screen-chunk',
  heartbeat: '/heartbeat',
  stop: '/stop',
};

const DYNAMIC_RULE_ID_START = 10000;
const HEARTBEAT_MS = 30000;
const SCREENSHOT_MS = CAPTURE_BASE_MS;
const SCREEN_CHUNK_MS = CAPTURE_BASE_MS;
const SCREEN_CHUNK_FAILURE_COOLDOWN_MS = 60000;
/** Joined + /start but no media yet — allow capture recovery after this delay. */
const CAPTURE_STALE_MS = 22000;
const CAPTURE_REPICK_COOLDOWN_MS = 45000;
const SESSION_EXPIRY_CHECK_MS = 15000;
const EVENT_LOG_STORAGE_KEY = 'proctoringEvents';
const MAX_STORED_EVENTS = 400;
const UPLOAD_QUEUE_DB = 'risebird-proctoring-uploads';
const UPLOAD_QUEUE_STORE = 'pending';
const UPLOAD_QUEUE_ALARM = 'proctoring-upload-queue';
const MAX_QUEUE_DB_ITEMS = 120;
const MAX_UPLOAD_ATTEMPTS = 20;
const IMMEDIATE_UPLOAD_RETRIES = 3;
const IMMEDIATE_UPLOAD_RETRIES_CHUNK = 6;
const CHUNK_CAPTURE_CONTEXT_CACHE_MS = 45000;
const UPLOAD_QUEUE_PROCESSING = { active: false };
const captureContextCache = { value: null, at: 0 };

const state = {
  joined: false,
  sessionActive: false,
  sessionActiveSince: 0,
  lastCaptureRecoveryAt: 0,
  violations: 0,
  settings: { clipboardBlocking: true },
  events: [],
  sessionToken: null,
  interviewId: null,
  name: '',
  email: '',
  policy: {
    restrictedDomains: [],
    allowedExamHosts: [],
    clipboardBlocking: true,
  },
  apiOrigin: '',
  heartbeatTimer: null,
  screenshotTimer: null,
  screenshotIntervalMs: SCREENSHOT_MS,
  screenChunkIntervalMs: SCREEN_CHUNK_MS,
  proctoringTabId: null,
  proctoringWindowId: null,
  dynamicRuleIds: [],
  screenshotUploads: 0,
  lastScreenshotAt: null,
  screenChunkUploads: 0,
  lastScreenChunkAt: null,
  lastScreenChunkFailureAt: 0,
  lastScreenshotFailureAt: 0,
  sessionExpiresAtMs: null,
  sessionExpiryTimer: null,
  sessionEnding: false,
  autoPopupShownForSessionKey: '',
  /** Last http(s) tab the user had focused — used when Chrome loses focus (no live URL). */
  lastKnownTabContext: {
    hostname: '',
    url: '',
    title: '',
    tabId: null,
    windowId: null,
    updatedAt: 0,
  },
  /** Recent distinct hostnames from tab switches (newest last), for blur metadata. */
  recentTabHosts: [],
  /** Content-script monitor share (getDisplayMedia) — distinct from offscreen desktopCapture. */
  contentScreenShareActive: false,
  flowPhase: PROCTORING_FLOW_PHASE.NOT_JOINED,
  joinInFlight: false,
  initProctoringCompletedKey: '',
  backendJoinConfirmedKey: '',
  backendStartConfirmedKey: '',
};

let initProctoringSessionInFlight = false;

let tabActivatedListener = null;
let windowFocusListener = null;
let completeTestSetupInFlight = false;
let completeTestSetupStartedAt = 0;
const SETUP_LOCK_STALE_MS = 90000;
/** Set when the picker was shown for this session — prevents repeated share dialogs. */
let screenPickerAttemptedKey = null;
let screenPickerInFlight = false;
/** Join + /start + offscreen warm complete; screen picker runs in phase 2. */
let proctoringPrepareKey = null;

/** Broad http(s) access — optional; requested at join/start (user gesture). Required for tab URL logging & screenshots on arbitrary sites during proctoring. */
const OPTIONAL_WEB_ORIGINS = ['http://*/*', 'https://*/*'];

async function hasOptionalWebOrigins() {
  try {
    return await chrome.permissions.contains({ origins: OPTIONAL_WEB_ORIGINS });
  } catch {
    return false;
  }
}

/** Optional web hosts are requested from the popup (user gesture); see popup.js. */

function pushEvent(entry) {
  state.events.push(entry);
  if (entry.level === 'violation') state.violations += 1;
  if (state.events.length > MAX_STORED_EVENTS) {
    state.events = state.events.slice(-MAX_STORED_EVENTS);
  }
  void persistEventsToStorage();
}

async function persistEventsToStorage() {
  try {
    await chrome.storage.local.set({ [EVENT_LOG_STORAGE_KEY]: state.events });
  } catch (e) {
    console.warn('persistEventsToStorage', e);
  }
}

async function loadEventsFromStorageIfEmpty() {
  if (state.events.length > 0) return;
  try {
    const data = await chrome.storage.local.get([EVENT_LOG_STORAGE_KEY]);
    const list = data[EVENT_LOG_STORAGE_KEY];
    if (Array.isArray(list) && list.length) {
      state.events = list.slice(-MAX_STORED_EVENTS);
      state.violations = state.events.filter((e) => e.level === 'violation').length;
    }
  } catch (e) {
    console.warn('loadEventsFromStorageIfEmpty', e);
  }
}

function isExamHostAllowed(hostname) {
  const allowed = state.policy.allowedExamHosts || [];
  const h = (hostname || '').toLowerCase();
  return allowed.some((a) => a && h === String(a).toLowerCase());
}

function describeTabUrl(url) {
  if (!url) return { hostname: '(unknown)', origin: '' };
  try {
    if (url.startsWith('chrome://')) return { hostname: 'chrome-ui', origin: 'chrome://' };
    if (url.startsWith('chrome-extension://')) {
      const u = new URL(url);
      return { hostname: u.hostname || 'extension', origin: u.origin };
    }
    if (url.startsWith('edge://')) return { hostname: 'edge-ui', origin: 'edge://' };
    if (!/^https?:/i.test(url)) return { hostname: '(restricted)', origin: '' };
    const u = new URL(url);
    return { hostname: u.hostname, origin: u.origin };
  } catch {
    return { hostname: '(invalid)', origin: '' };
  }
}

/** Proctoring session is only allowed on the AI Interview page (/AIInterview/...), not human interview or dashboard. */
function isAllowedProctoringPageUrl(url) {
  if (!url || !/^https?:/i.test(url)) return false;
  try {
    const u = new URL(url);
    const path = (u.pathname || '').toLowerCase();
    if (!path.includes('/aiinterview')) return false;
    const host = (u.hostname || '').toLowerCase();
    if (host === 'localhost' || host === '127.0.0.1') return true;
    if (host === 'portal.risebird.io' || host.endsWith('.risebird.io')) return true;
    return false;
  } catch {
    return false;
  }
}

async function recordWorkerEvent(level, reason, metadata) {
  const entry = {
    type: level,
    level,
    reason,
    timestamp: Date.now(),
    metadata: metadata || {},
  };
  pushEvent(entry);
  try {
    await apiPost(
      PROCTORING_API.event,
      {
        interviewId: state.interviewId,
        level,
        reason,
        metadata,
      },
      true
    );
  } catch (e) {
    console.warn('proctoring event POST failed (event still stored locally)', e);
  }
}

                                                                                                                                                                                                                                                                                        async function logActiveTabFromTabId(tabId, windowId, source, options = {}) {
  const recordEvent = options.recordEvent !== false;
  if (!state.sessionToken) return;
  if (!state.joined && !state.sessionActive) return;
  try {
    const tab = await chrome.tabs.get(tabId);
    const url = tab.url || tab.pendingUrl || '';
    const { hostname, origin } = describeTabUrl(url);
    state.lastKnownTabContext = {
      hostname,
      url: url.slice(0, 2000),
      title: (tab.title || '').trim(),
      tabId,
      windowId,
      updatedAt: Date.now(),
    };
    const rh = state.recentTabHosts;
    if (hostname && rh[rh.length - 1] !== hostname) {
      rh.push(hostname);
      if (rh.length > 20) rh.shift();
    }
    if (!recordEvent) return;
    const tabTitle = (tab.title || '').trim();
    const titleShort = tabTitle.length > 80 ? tabTitle.slice(0, 77) + '…' : tabTitle;
    const isAllowedExamHost = isExamHostAllowed(hostname);
    const reason = `Tab switch — user is now on host: ${hostname}${titleShort ? ` | tab: "${titleShort}"` : ''} | ${isAllowedExamHost ? 'allowed exam host' : 'NOT an allowed exam host (violation)'}`;
    const metadata = {
      eventCategory: 'tab_switch',
      event: 'tab_focus_change',
      source: source || 'tabs.onActivated',
      activeTabUrl: url.slice(0, 800),
      activeTabHostname: hostname,
      activeTabOrigin: origin || undefined,
      tabTitle: tab.title || '',
      tabId,
      windowId,
      isAllowedExamHost,
      recentTabHosts: state.recentTabHosts.slice(-10),
    };
    const level = isAllowedExamHost ? 'info' : 'violation';
    await recordWorkerEvent(level, reason, metadata);
  } catch (e) {
    console.warn('proctoring tab log', e);
  }
}

async function refreshLastKnownTabOnly() {
  if (!state.sessionToken) return;
  if (!state.joined && !state.sessionActive) return;
  try {
    const w = await chrome.windows.getLastFocused({ populate: true });
    if (w?.tabs?.length) {
      const active = w.tabs.find((t) => t.active);
      if (active?.id != null) {
        await logActiveTabFromTabId(active.id, w.id, 'init_last_known', { recordEvent: false });
        return;
      }
    }
    const tabs = await chrome.tabs.query({ active: true, lastFocusedWindow: true });
    const t = tabs[0];
    if (t?.id != null) await logActiveTabFromTabId(t.id, t.windowId, 'init_last_known', { recordEvent: false });
  } catch (e) {
    console.warn('refreshLastKnownTabOnly', e);
  }
}

function startTabAndWindowFocusMonitoring() {
  stopTabAndWindowFocusMonitoring();

  tabActivatedListener = (activeInfo) => {
    if (!state.joined && !state.sessionActive) return;
    void logActiveTabFromTabId(activeInfo.tabId, activeInfo.windowId, 'tabs.onActivated').catch(() => {});
  };
  chrome.tabs.onActivated.addListener(tabActivatedListener);

  windowFocusListener = (windowId) => {
    if (!state.joined && !state.sessionActive) return;
    void (async () => {
      try {
        if (windowId === chrome.windows.WINDOW_ID_NONE) {
          const ctx = state.lastKnownTabContext;
          const host = ctx.hostname || '(unknown)';
          const urlShort = (ctx.url || '').slice(0, 120);
          const reason = `Focus left Chrome — last active tab before blur: ${host}${
            urlShort ? ` | ${urlShort}` : ''
          }${ctx.title ? ` | "${ctx.title.slice(0, 60)}${ctx.title.length > 60 ? '…' : ''}"` : ''}`;
          await recordWorkerEvent('violation', reason, {
            eventCategory: 'window_focus',
            event: 'window_blur',
            focusTarget: 'outside_chrome',
            lastActiveTabHostname: ctx.hostname || null,
            lastActiveTabUrl: ctx.url ? ctx.url.slice(0, 800) : undefined,
            lastTabTitle: ctx.title || undefined,
            tabId: ctx.tabId,
            windowId: ctx.windowId,
            recentTabHosts: state.recentTabHosts.slice(-10),
          });
          return;
        }
        const tabs = await chrome.tabs.query({ active: true, windowId });
        const t = tabs[0];
        if (t?.id != null) await logActiveTabFromTabId(t.id, windowId, 'windows.onFocusChanged');
      } catch (e) {
        console.warn('proctoring window focus', e);
      }
    })();
  };
  chrome.windows.onFocusChanged.addListener(windowFocusListener);
  void refreshLastKnownTabOnly();
}

function stopTabAndWindowFocusMonitoring() {
  if (tabActivatedListener) {
    chrome.tabs.onActivated.removeListener(tabActivatedListener);
    tabActivatedListener = null;
  }
  if (windowFocusListener) {
    chrome.windows.onFocusChanged.removeListener(windowFocusListener);
    windowFocusListener = null;
  }
}

function parseInterviewId(value) {
  const n = Number(value);
  return Number.isFinite(n) && n > 0 ? n : null;
}

function parseSessionExpiryMs(rawValue) {
  if (rawValue === undefined || rawValue === null || rawValue === '') return null;
  if (typeof rawValue === 'number' && Number.isFinite(rawValue)) {
    return rawValue > 1000000000000 ? Math.round(rawValue) : null;
  }
  const text = String(rawValue).trim();
  if (!text) return null;
  const asNum = Number(text);
  if (Number.isFinite(asNum) && asNum > 1000000000000) return Math.round(asNum);
  const parsed = Date.parse(text);
  return Number.isFinite(parsed) ? parsed : null;
}

function stopSessionExpiryTimer() {
  if (state.sessionExpiryTimer) {
    clearInterval(state.sessionExpiryTimer);
    state.sessionExpiryTimer = null;
  }
}

function startSessionExpiryTimer() {
  stopSessionExpiryTimer();
  if (!state.sessionExpiresAtMs) return;
  state.sessionExpiryTimer = setInterval(() => {
    void maybeEndSessionIfExpired('session_expiry_timer');
  }, SESSION_EXPIRY_CHECK_MS);
}

function setSessionExpiry(rawValue) {
  state.sessionExpiresAtMs = parseSessionExpiryMs(rawValue);
  startSessionExpiryTimer();
}

async function maybeEndSessionIfExpired(source) {
  if (!state.sessionExpiresAtMs) return false;
  if (!state.joined && !state.sessionActive) return false;
  if (Date.now() < state.sessionExpiresAtMs) return false;
  if (state.sessionEnding) return true;
  state.sessionEnding = true;
  try {
    await recordWorkerEvent('info', 'Session ended automatically — meeting window expired.', {
      eventCategory: 'session_lifecycle',
      event: 'session_auto_end_expired',
      source,
      sessionExpiresAtMs: state.sessionExpiresAtMs,
    });
    await endExamInternal();
  } finally {
    state.sessionEnding = false;
  }
  return true;
}

function isRetryableApiError(err) {
  if (!err) return false;
  const status = Number(err.status) || 0;
  if (status === 401 || status === 403 || status === 400 || status === 404) return false;
  if (status >= 500 && status < 600) return true;
  if (status === 502 || status === 503 || status === 504 || status === 429) return true;
  const msg = String(err.message || err).toLowerCase();
  return (
    msg.includes('failed to fetch') ||
    msg.includes('networkerror') ||
    msg.includes('network request failed') ||
    msg.includes('load failed') ||
    msg.includes('timeout') ||
    msg.includes('econnreset') ||
    msg.includes('enotfound') ||
    msg.includes('aborted')
  );
}

function apiPostMaxAttempts(path) {
  if (path === PROCTORING_API.join || path === PROCTORING_API.start) return 8;
  if (path === PROCTORING_API.screenChunk) return 6;
  if (path === PROCTORING_API.screenshot) return 5;
  return 3;
}

async function apiPostOnce(path, body, useBearer) {
  const base = (state.apiOrigin || '').replace(/\/$/, '');
  if (!base) {
    throw new Error('API origin is missing. Open the Risebird portal in a tab and try again.');
  }
  const headers = { 'Content-Type': 'application/json' };
  if (useBearer && state.sessionToken) {
    headers.Authorization = 'Bearer ' + state.sessionToken;
  }
  const url = base + PROCTORING_API_BASE + path;
  let res;
  try {
    res = await fetch(url, {
      method: 'POST',
      headers,
      body: JSON.stringify(body ?? {}),
    });
  } catch (fetchErr) {
    const err = new Error(fetchErr?.message || 'Network request failed.');
    err.cause = fetchErr;
    throw err;
  }
  if (!res.ok) {
    const t = await res.text();
    const err = new Error(t || res.statusText || 'Request failed');
    err.status = res.status;
    throw err;
  }
  try {
    return await res.json();
  } catch {
    return {};
  }
}

async function apiPost(path, body, useBearer) {
  const maxAttempts = apiPostMaxAttempts(path);
  let lastError = null;
  for (let attempt = 0; attempt < maxAttempts; attempt++) {
    try {
      return await apiPostOnce(path, body, useBearer);
    } catch (e) {
      lastError = e;
      if (!isRetryableApiError(e) || attempt >= maxAttempts - 1) {
        throw e;
      }
      await new Promise((r) => setTimeout(r, Math.min(10000, 600 + attempt * 900)));
    }
  }
  throw lastError || new Error('API request failed.');
}

async function onProctoringNetworkOnline() {
  try {
    if (state.joined && !state.sessionActive) {
      await ensureProctoringSessionStartedWithRetry();
    }
    if (state.sessionActive) {
      scheduleOffscreenMediaKeepAlive();
      void processUploadQueue();
    }
    if (state.proctoringTabId != null) {
      try {
        await chrome.tabs.sendMessage(state.proctoringTabId, { type: 'NOTIFY_NETWORK_ONLINE' });
      } catch (_) {
        /* tab may be closed */
      }
    }
  } catch (e) {
    console.warn('onProctoringNetworkOnline', e);
  }
}

function openUploadQueueDb() {
  return new Promise((resolve, reject) => {
    const req = indexedDB.open(UPLOAD_QUEUE_DB, 1);
    req.onerror = () => reject(req.error || new Error('IndexedDB open failed'));
    req.onupgradeneeded = () => {
      const db = req.result;
      if (!db.objectStoreNames.contains(UPLOAD_QUEUE_STORE)) {
        db.createObjectStore(UPLOAD_QUEUE_STORE, { keyPath: 'id', autoIncrement: true });
      }
    };
    req.onsuccess = () => resolve(req.result);
  });
}

async function dbQueueAdd(item) {
  const db = await openUploadQueueDb();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(UPLOAD_QUEUE_STORE, 'readwrite');
    const store = tx.objectStore(UPLOAD_QUEUE_STORE);
    const req = store.add(item);
    req.onsuccess = () => resolve(req.result);
    req.onerror = () => reject(req.error);
  });
}

async function dbQueueGetAll() {
  const db = await openUploadQueueDb();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(UPLOAD_QUEUE_STORE, 'readonly');
    const req = tx.objectStore(UPLOAD_QUEUE_STORE).getAll();
    req.onsuccess = () => resolve(req.result || []);
    req.onerror = () => reject(req.error);
  });
}

async function dbQueueDelete(id) {
  const db = await openUploadQueueDb();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(UPLOAD_QUEUE_STORE, 'readwrite');
    const req = tx.objectStore(UPLOAD_QUEUE_STORE).delete(id);
    req.onsuccess = () => resolve();
    req.onerror = () => reject(req.error);
  });
}

async function dbQueueTrimOldest(maxItems) {
  const all = await dbQueueGetAll();
  if (all.length <= maxItems) return;
  const screenshots = all
    .filter((r) => r.kind === 'screenshot')
    .sort((a, b) => (a.createdAt || 0) - (b.createdAt || 0));
  const chunks = all
    .filter((r) => r.kind !== 'screenshot')
    .sort((a, b) => (a.createdAt || 0) - (b.createdAt || 0));
  const keep = [...chunks, ...screenshots].slice(-maxItems);
  const keepIds = new Set(keep.map((r) => r.id).filter((id) => id != null));
  for (const row of all) {
    if (row.id != null && !keepIds.has(row.id)) await dbQueueDelete(row.id);
  }
}

function scheduleUploadQueueAlarm() {
  try {
    chrome.alarms.create(UPLOAD_QUEUE_ALARM, { periodInMinutes: 0.5 });
  } catch (e) {
    console.warn('scheduleUploadQueueAlarm', e);
  }
}

function clearUploadQueueAlarm() {
  try {
    chrome.alarms.clear(UPLOAD_QUEUE_ALARM);
  } catch (_) {
    /* ignore */
  }
}

async function buildCaptureContext(pageContext) {
  const ctx = Object.assign({}, pageContext || {});
  ctx.enrichedAt = Date.now();
  try {
    const tabs = await chrome.tabs.query({ active: true, lastFocusedWindow: true });
    const t = tabs[0];
    if (t) {
      const { hostname } = describeTabUrl(t.url || t.pendingUrl || '');
      ctx.focusedTabId = t.id;
      ctx.focusedTabWindowId = t.windowId;
      ctx.focusedTabHostname = hostname;
      ctx.focusedTabUrl = (t.url || t.pendingUrl || '').slice(0, 500);
      ctx.focusedTabTitle = (t.title || '').slice(0, 120);
      ctx.isInterviewTabFocused =
        state.proctoringTabId != null && t.id === state.proctoringTabId;
    }
  } catch (e) {
    ctx.focusLookupError = e?.message || String(e);
  }
  if (state.proctoringTabId != null) ctx.proctoringTabId = state.proctoringTabId;
  if (state.proctoringWindowId != null) ctx.proctoringWindowId = state.proctoringWindowId;
  return ctx;
}

async function buildCaptureContextForChunk(pageContext) {
  const now = Date.now();
  if (
    captureContextCache.value &&
    now - captureContextCache.at < CHUNK_CAPTURE_CONTEXT_CACHE_MS
  ) {
    return Object.assign({}, captureContextCache.value, pageContext || {}, {
      enrichedAt: now,
      contextCacheHit: true,
    });
  }
  const ctx = await buildCaptureContext(pageContext);
  captureContextCache.value = ctx;
  captureContextCache.at = now;
  return ctx;
}

async function postScreenshotToApi(payload) {
  const captureContext = await buildCaptureContext(payload.captureContext);
  const capturedAtMs = toEpochMs(payload.capturedAt);
  const body = {
    interviewId: state.interviewId,
    imageBase64: payload.imageBase64,
    capturedAt: capturedAtMs,
    captureSource: payload.captureSource || 'display_media',
    captureContext,
  };
  await apiPost(PROCTORING_API.screenshot, body, true);
  state.screenshotUploads += 1;
  state.lastScreenshotAt = Date.now();
  await syncDiagnosticsToStorage();
}

async function postScreenChunkToApi(payload) {
  const captureContext = await buildCaptureContextForChunk(payload.captureContext);
  const capturedAtMs = toEpochMs(payload.capturedAt);
  await apiPost(
    PROCTORING_API.screenChunk,
    {
      interviewId: state.interviewId,
      chunkSeq: payload.chunkSeq,
      mimeType: payload.mimeType,
      capturedAt: capturedAtMs,
      data: payload.data,
      captureSource: payload.captureSource || 'display_media',
      captureContext,
    },
    true
  );
  state.screenChunkUploads += 1;
  state.lastScreenChunkAt = Date.now();
  await syncDiagnosticsToStorage();
}

async function tryUploadMediaImmediate(kind, payload) {
  let lastErr = null;
  let lastStatus = null;
  const maxAttempts =
    kind === 'screen_chunk' ? IMMEDIATE_UPLOAD_RETRIES_CHUNK : IMMEDIATE_UPLOAD_RETRIES;
  for (let attempt = 1; attempt <= maxAttempts; attempt++) {
    try {
      if (kind === 'screenshot') await postScreenshotToApi(payload);
      else await postScreenChunkToApi(payload);
      return { ok: true };
    } catch (e) {
      lastErr = e;
      if (e?.status != null) lastStatus = e.status;
      if (attempt < maxAttempts) {
        const backoff = kind === 'screen_chunk' ? 800 * attempt : 400 * attempt;
        await new Promise((r) => setTimeout(r, backoff));
      }
    }
  }
  return {
    ok: false,
    error: lastErr?.message || String(lastErr),
    status: lastStatus,
  };
}

async function enqueueFailedMediaUpload(kind, payload, lastError) {
  try {
    await dbQueueTrimOldest(MAX_QUEUE_DB_ITEMS - 1);
    await dbQueueAdd({
      kind,
      payload,
      attempts: 0,
      lastError: lastError || '',
      createdAt: Date.now(),
      interviewId: state.interviewId,
    });
    scheduleUploadQueueAlarm();
  } catch (e) {
    console.warn('enqueueFailedMediaUpload', e);
  }
}

async function processUploadQueue() {
  if (UPLOAD_QUEUE_PROCESSING.active) return;
  if (!state.sessionToken) return;
  UPLOAD_QUEUE_PROCESSING.active = true;
  try {
    const rows = await dbQueueGetAll();
    if (!rows.length) return;
    rows.sort((a, b) => {
      const kindOrder = (r) => (r.kind === 'screen_chunk' ? 0 : 1);
      const ko = kindOrder(a) - kindOrder(b);
      if (ko !== 0) return ko;
      return (a.createdAt || 0) - (b.createdAt || 0);
    });
    for (const row of rows) {
      if (row.interviewId && state.interviewId && row.interviewId !== state.interviewId) {
        await dbQueueDelete(row.id);
        continue;
      }
      const attempts = (row.attempts || 0) + 1;
      if (attempts > MAX_UPLOAD_ATTEMPTS) {
        await dbQueueDelete(row.id);
        continue;
      }
      const result = await tryUploadMediaImmediate(row.kind, row.payload);
      if (result.ok) {
        await dbQueueDelete(row.id);
        continue;
      }
      try {
        await dbQueueDelete(row.id);
        await dbQueueAdd({
          kind: row.kind,
          payload: row.payload,
          attempts,
          lastError: result.error || '',
          createdAt: row.createdAt || Date.now(),
          interviewId: state.interviewId,
        });
      } catch (_) {
        /* ignore */
      }
      await new Promise((r) => setTimeout(r, 300));
    }
  } catch (e) {
    console.warn('processUploadQueue', e);
  } finally {
    UPLOAD_QUEUE_PROCESSING.active = false;
  }
}

async function ensureSessionActiveForMediaUpload() {
  if (state.sessionActive && state.backendStartConfirmedKey === getSessionKey()) {
    return { ok: true };
  }
  if (!state.joined || state.backendJoinConfirmedKey !== getSessionKey()) {
    const joinRes = await completeBackendJoinAfterShare();
    if (!joinRes.ok) return joinRes;
  }
  return ensureProctoringSessionStarted();
}

async function uploadScreenshotPayload(payload) {
  if (!state.sessionToken) {
    return { ok: false, retry: false };
  }
  if (!state.sessionActive) {
    const armed = await ensureSessionActiveForMediaUpload();
    if (!armed.ok) {
      if (!state.joined) {
        return { ok: false, retry: false };
      }
      await enqueueFailedMediaUpload('screenshot', payload, armed.error || 'session_not_active_yet');
      return { ok: false, retry: armed.retry !== false, error: armed.error || 'session_not_active_yet' };
    }
  }
  const result = await tryUploadMediaImmediate('screenshot', payload);
  if (result.ok) return { ok: true };
  await enqueueFailedMediaUpload('screenshot', payload, result.error);
  const now = Date.now();
  if (now - (state.lastScreenshotFailureAt || 0) >= SCREEN_CHUNK_FAILURE_COOLDOWN_MS) {
    state.lastScreenshotFailureAt = now;
    await recordWorkerEvent('violation', 'Screenshot upload failed', {
      eventCategory: 'screenshot',
      event: 'screenshot_upload_failed',
      violationType: 'screenshot_upload_failed',
      error: result.error,
      statusCode: result.status != null ? result.status : undefined,
      capturedAt: payload.capturedAt,
    });
  }
  return { ok: false, retry: true, error: result.error };
}

async function uploadScreenChunkPayload(payload) {
  if (!state.sessionToken) {
    return { ok: false, retry: false };
  }
  if (!state.sessionActive) {
    const armed = await ensureSessionActiveForMediaUpload();
    if (!armed.ok) {
      if (!state.joined) {
        return { ok: false, retry: false };
      }
      await enqueueFailedMediaUpload('screen_chunk', payload, armed.error || 'session_not_active_yet');
      return { ok: false, retry: armed.retry !== false, error: armed.error || 'session_not_active_yet' };
    }
  }
  const result = await tryUploadMediaImmediate('screen_chunk', payload);
  if (result.ok) {
    void processUploadQueue();
    return { ok: true };
  }
  await enqueueFailedMediaUpload('screen_chunk', payload, result.error);
  const now = Date.now();
  if (now - state.lastScreenChunkFailureAt >= SCREEN_CHUNK_FAILURE_COOLDOWN_MS) {
    state.lastScreenChunkFailureAt = now;
    await recordWorkerEvent('violation', 'Screen chunk upload failed', {
      eventCategory: 'screen_chunk',
      event: 'screen_chunk_upload_failed',
      violationType: 'screen_chunk_upload_failed',
      error: typeof result.error === 'string' ? result.error : result.error?.message || String(result.error),
      statusCode: result.status != null ? result.status : undefined,
      chunkSeq: payload.chunkSeq,
      capturedAt: payload.capturedAt,
    });
  }
  return { ok: false, retry: true, error: result.error };
}

function sanitizeMs(value, fallback, min, max) {
  const n = Number(value);
  if (!Number.isFinite(n)) return fallback;
  return Math.min(Math.max(Math.round(n), min), max);
}

function toEpochMs(value) {
  if (value == null || value === '') return Date.now();
  if (typeof value === 'number' && Number.isFinite(value)) return Math.round(value);
  const asNum = Number(value);
  if (Number.isFinite(asNum)) return Math.round(asNum);
  const parsed = Date.parse(String(value));
  if (Number.isFinite(parsed)) return parsed;
  return Date.now();
}

function getSessionKey() {
  if (!state.interviewId) return '';
  return `interview:${state.interviewId}`;
}

function computeFlowPhase(contentFlow) {
  if (state.sessionEnding) return PROCTORING_FLOW_PHASE.SESSION_ENDED;
  if (state.joinInFlight) return PROCTORING_FLOW_PHASE.JOINING_SESSION;
  if (!state.joined) return PROCTORING_FLOW_PHASE.NOT_JOINED;
  if (!state.sessionActive) return PROCTORING_FLOW_PHASE.WAITING_FOR_SCREEN_SHARE;
  if (contentFlow?.shareStopped && !state.contentScreenShareActive) {
    return PROCTORING_FLOW_PHASE.SCREEN_SHARE_STOPPED;
  }
  if (state.contentScreenShareActive && state.sessionActive) {
    if (contentFlow?.isProctoringActive === false) return PROCTORING_FLOW_PHASE.SCREEN_SHARING_ACTIVE;
    return PROCTORING_FLOW_PHASE.PROCTORING_ACTIVE;
  }
  if (contentFlow?.isPromptingScreenShare) return PROCTORING_FLOW_PHASE.WAITING_FOR_SCREEN_SHARE;
  return PROCTORING_FLOW_PHASE.WAITING_FOR_SCREEN_SHARE;
}

async function syncFlowPhaseToStorage(contentFlow) {
  state.flowPhase = computeFlowPhase(contentFlow);
  try {
    await chrome.storage.local.set({
      proctoringFlowPhase: state.flowPhase,
      proctoringContentFlow: contentFlow || {},
    });
  } catch (e) {
    console.warn('syncFlowPhaseToStorage', e);
  }
}

async function applyJoinPayloadFromHandshake(p, sender) {
  const interviewId = parseInterviewId(p.interviewId);
  if (!interviewId) throw new Error('Invalid Interview ID.');
  if (!p.sessionToken || !String(p.sessionToken).trim()) throw new Error('Missing session token.');
  state.sessionToken = p.sessionToken;
  state.interviewId = interviewId;
  state.name = p.name || '';
  state.email = p.email || '';
  state.policy.restrictedDomains = p.restrictedDomains || [];
  state.policy.allowedExamHosts = p.allowedExamHosts || [];
  state.policy.clipboardBlocking = p.clipboardBlocking !== false;
  state.screenshotIntervalMs = sanitizeMs(p.screenshotIntervalMs, SCREENSHOT_MS, 10000, 300000);
  state.screenChunkIntervalMs = sanitizeMs(p.screenChunkIntervalMs, SCREEN_CHUNK_MS, 10000, 120000);
  setSessionExpiry(p.sessionExpiresAtMs || p.meetingEndIso || p.meetingEndMs);
  state.settings.clipboardBlocking = state.policy.clipboardBlocking;
  state.apiOrigin = (p.apiOrigin || '').replace(/\/$/, '');
  await resolveApiOrigin(sender?.tab);
  if (!state.apiOrigin) throw new Error('Could not resolve API origin.');
  await persistSessionToStorage();
  try {
    await syncDynamicRules(state.policy.restrictedDomains);
  } catch (dnrErr) {
    console.warn('applyJoinPayload DNR', dnrErr);
  }
  return interviewId;
}

async function completeBackendJoinAfterShare() {
  const sessionKey = getSessionKey();
  if (state.joined && state.backendJoinConfirmedKey === sessionKey) {
    return { ok: true, alreadyJoined: true };
  }
  if (!state.sessionToken || !state.interviewId) {
    return { ok: false, error: 'Missing session token or interview id for join.' };
  }
  if (!state.apiOrigin) {
    return { ok: false, error: 'API origin is missing. Reload the interview page and try again.' };
  }
  let joinApiError = null;
  for (let attempt = 0; attempt < 8; attempt++) {
    try {
      await apiPost(
        PROCTORING_API.join,
        {
          interviewId: state.interviewId,
          sessionToken: state.sessionToken,
          name: state.name,
          email: state.email,
        },
        false
      );
      joinApiError = null;
      break;
    } catch (err) {
      joinApiError = err;
      if (!isRetryableApiError(err) || attempt >= 7) break;
      await new Promise((r) => setTimeout(r, Math.min(4000, 350 + attempt * 300)));
    }
  }
  if (joinApiError) {
    return { ok: false, error: joinApiError?.message || String(joinApiError) };
  }
  state.joined = true;
  state.backendJoinConfirmedKey = sessionKey;
  if (!state.sessionActive) await resetSessionDiagnostics();
  await persistSessionToStorage();
  startTabAndWindowFocusMonitoring();
  return { ok: true };
}

async function initProctoringSessionInternal(p, sender) {
  if (initProctoringSessionInFlight) {
    return { ok: false, inProgress: true, error: 'Proctoring initialization already in progress.' };
  }
  const incomingId = parseInterviewId(p?.interviewId);
  const incomingKey = incomingId ? `interview:${incomingId}` : '';
  if (state.joined && state.sessionToken && incomingKey && state.initProctoringCompletedKey === incomingKey) {
    if (state.backendJoinConfirmedKey !== incomingKey) {
      const joinRes = await completeBackendJoinAfterShare();
      if (!joinRes.ok) return joinRes;
    }
    const startRes =
      state.sessionActive && state.backendStartConfirmedKey === incomingKey
        ? { ok: true }
        : await ensureProctoringSessionStartedWithRetry();
    rememberProctoringTab(sender.tab?.id, sender.tab?.windowId);
    return {
      ok: true,
      alreadyInitialized: true,
      joined: true,
      sessionStarted: startRes.ok === true,
      proctoringReady: startRes.ok === true,
    };
  }
  initProctoringSessionInFlight = true;
  state.joinInFlight = true;
  await syncFlowPhaseToStorage({ isPromptingScreenShare: false });
  try {
    await applyJoinPayloadFromHandshake(p, sender);
    void ensureOffscreenDocument().catch((e) => console.warn('offscreen prewarm', e));
    let joinApiError = null;
    for (let attempt = 0; attempt < 8; attempt++) {
      try {
        await apiPost(
          PROCTORING_API.join,
          {
            interviewId: state.interviewId,
            sessionToken: state.sessionToken,
            name: state.name,
            email: state.email,
          },
          false
        );
        joinApiError = null;
        break;
      } catch (err) {
        joinApiError = err;
        if (!isRetryableApiError(err) || attempt >= 7) break;
        await new Promise((r) => setTimeout(r, Math.min(4000, 350 + attempt * 300)));
      }
    }
    if (joinApiError) {
      return { ok: false, error: joinApiError?.message || String(joinApiError) };
    }
    state.joined = true;
    state.backendJoinConfirmedKey = getSessionKey();
    if (!state.sessionActive) await resetSessionDiagnostics();
    await persistSessionToStorage();
    startTabAndWindowFocusMonitoring();
    const deferSessionStart = p.deferSessionStart === true;
    if (deferSessionStart) {
      rememberProctoringTab(sender.tab?.id, sender.tab?.windowId);
      await syncFlowPhaseToStorage({ isPromptingScreenShare: false });
      return {
        ok: true,
        joined: true,
        sessionStarted: false,
        proctoringReady: false,
        deferredStart: true,
      };
    }
    const startRes = await ensureProctoringSessionStartedWithRetry();
    if (startRes.ok) {
      proctoringPrepareKey = getSessionKey();
      state.initProctoringCompletedKey = proctoringPrepareKey;
      await chrome.storage.local.set({ proctoringPrepareKey });
    }
    rememberProctoringTab(sender.tab?.id, sender.tab?.windowId);
    await syncFlowPhaseToStorage({ isPromptingScreenShare: false });
    return {
      ok: true,
      joined: true,
      sessionStarted: startRes.ok === true,
      proctoringReady: startRes.ok === true,
      startError: startRes.ok ? null : startRes.error,
    };
  } finally {
    state.joinInFlight = false;
    initProctoringSessionInFlight = false;
  }
}

async function resetSessionDiagnostics() {
  state.screenshotUploads = 0;
  state.lastScreenshotAt = null;
  state.screenChunkUploads = 0;
  state.lastScreenChunkAt = null;
  state.lastScreenChunkFailureAt = 0;
  state.lastScreenshotFailureAt = 0;
  await syncDiagnosticsToStorage();
}

async function syncDynamicRules(domains) {
  const removeIds = [...state.dynamicRuleIds];
  state.dynamicRuleIds = [];
  const raw = Array.isArray(domains) ? domains : [];
  const list = raw
    .map((d) =>
      String(d || '')
        .replace(/^https?:\/\//, '')
        .split('/')[0]
        .replace(/^www\./, '')
        .trim()
    )
    .filter(Boolean);
  const addRules = list.map((clean, i) => {
    const id = DYNAMIC_RULE_ID_START + i;
    state.dynamicRuleIds.push(id);
    return {
      id,
      priority: 1,
      action: { type: 'block' },
      condition: {
        urlFilter: '||' + clean + '^',
        resourceTypes: ['main_frame', 'sub_frame'],
      },
    };
  });
  try {
    await chrome.declarativeNetRequest.updateDynamicRules({
      removeRuleIds: removeIds,
      addRules,
    });
  } catch (e) {
    console.warn('declarativeNetRequest.updateDynamicRules failed (session continues)', e);
  }
}

function isTrustedProctoringExamHost(hostname) {
  if (!hostname) return false;
  const host = String(hostname).toLowerCase();
  if (host === 'localhost' || host === '127.0.0.1' || host === '[::1]' || host.endsWith('.localhost')) {
    return true;
  }
  if (host === 'portal.risebird.io' || host.endsWith('.risebird.io')) return true;
  return false;
}

function validateHost(tabUrl) {
  try {
    const host = new URL(tabUrl).hostname;
    if (isTrustedProctoringExamHost(host)) return true;
    const allowed = state.policy.allowedExamHosts || [];
    return allowed.some((h) => h && h.toLowerCase() === host.toLowerCase());
  } catch {
    return false;
  }
}

function stopHeartbeat() {
  if (state.heartbeatTimer) {
    clearInterval(state.heartbeatTimer);
    state.heartbeatTimer = null;
  }
}

function getHeartbeatStatus() {
  if (!state.joined) return 'stopped';
  if (!state.sessionActive) return 'stopped';
  if (!state.contentScreenShareActive) return 'screen_share_missing';
  return 'active';
}

function startHeartbeat() {
  stopHeartbeat();
  state.heartbeatTimer = setInterval(() => {
    void maybeEndSessionIfExpired('heartbeat_tick');
    if (!state.sessionActive) return;
    if (!state.apiOrigin) return;
    void processUploadQueue();
    apiPost(
      PROCTORING_API.heartbeat,
      {
        interviewId: state.interviewId,
        status: getHeartbeatStatus(),
        violationCount: state.violations,
        timestamp: new Date().toISOString(),
        utcTicks: Date.now(),
      },
      true
    ).catch((e) =>
      console.warn('proctoring heartbeat', e)
    );
  }, HEARTBEAT_MS);
}

function stopScreenshotTimer() {
  if (state.screenshotTimer) {
    clearTimeout(state.screenshotTimer);
    state.screenshotTimer = null;
  }
}

const MIN_SCREENSHOT_BASE64_LEN = 800;

function isLikelyBlankScreenshotBase64(base64) {
  if (!base64 || base64.length < MIN_SCREENSHOT_BASE64_LEN) return true;
  return false;
}

/** Capture interview tab via chrome.tabs.captureVisibleTab while content-script share is active. */
async function captureAndPostScreenshotFallback() {
  if (!state.apiOrigin || !state.sessionActive) return;
  try {
    const windowId =
      state.proctoringWindowId != null
        ? state.proctoringWindowId
        : (await chrome.windows.getLastFocused())?.id;
    if (windowId == null) return;
    const dataUrl = await chrome.tabs.captureVisibleTab(windowId, { format: 'jpeg', quality: 60 });
    if (!dataUrl || !dataUrl.startsWith('data:image')) return;
    const base64 = dataUrl.split(',')[1];
    if (isLikelyBlankScreenshotBase64(base64)) return;
    let tabId = state.proctoringTabId;
    let tabUrl = '';
    if (tabId != null) {
      try {
        const tab = await chrome.tabs.get(tabId);
        tabUrl = (tab.url || tab.pendingUrl || '').slice(0, 500);
      } catch (_) {
        /* ignore */
      }
    }
    await uploadScreenshotPayload({
      imageBase64: base64,
      capturedAt: new Date().toISOString(),
      captureSource: 'tabs_capture_visible_tab',
      captureContext: { tabId, url: tabUrl },
      tabId,
      url: tabUrl,
    });
  } catch (e) {
    console.warn('proctoring screenshot fallback', e);
  }
}

function startScreenshotFallbackTimer() {
  stopScreenshotTimer();
  const tick = (firstPass) => {
    if (!state.sessionActive) {
      stopScreenshotTimer();
      return;
    }
    void captureAndPostScreenshotFallback();
    const delay = firstPass
      ? CAPTURE_FIRST_SCREENSHOT_MS
      : interviewScreenshotDelayMs(state.screenshotIntervalMs || SCREENSHOT_MS);
    state.screenshotTimer = setTimeout(() => tick(false), delay);
  };
  tick(true);
}

function getContentMonitoringSettings() {
  return {
    clipboardBlocking: state.policy.clipboardBlocking,
    screenshotIntervalMs: state.screenshotIntervalMs || SCREENSHOT_MS,
    screenChunkIntervalMs: state.screenChunkIntervalMs || SCREEN_CHUNK_MS,
  };
}

async function armMediaCaptureAfterSessionStart() {
  if (!state.sessionActive) return;
  try {
    await messageOffscreen({
      type: 'OFFSCREEN_SESSION_STARTED',
      settings: getContentMonitoringSettings(),
    });
  } catch (e) {
    console.warn('armMediaCaptureAfterSessionStart offscreen', e);
  }
  void processUploadQueue();
}

let offscreenMediaKeepAliveTimer = null;

function stopOffscreenMediaKeepAlive() {
  if (offscreenMediaKeepAliveTimer) {
    clearTimeout(offscreenMediaKeepAliveTimer);
    offscreenMediaKeepAliveTimer = null;
  }
}

/** Keep offscreen recorder healthy; screenshots/chunks use jittered timers in offscreen.js. */
function scheduleOffscreenMediaKeepAlive() {
  stopOffscreenMediaKeepAlive();
  const tick = () => {
    if (!state.sessionActive) {
      stopOffscreenMediaKeepAlive();
      return;
    }
    void (async () => {
      const st = await getOffscreenCaptureState();
      if (st?.hasLiveStream && !st?.pipelineReady) {
        await messageOffscreen({ type: 'OFFSCREEN_ENSURE_RECORDING' }).catch(() => {});
        await messageOffscreen({ type: 'OFFSCREEN_CAPTURE_SCREENSHOT_NOW' }).catch(() => {});
      } else {
        await runWorkerCaptureRecoveryIfStale('keepalive');
      }
      void processUploadQueue();
    })();
    const base = state.screenshotIntervalMs || SCREENSHOT_MS;
    offscreenMediaKeepAliveTimer = setTimeout(
      tick,
      interviewCaptureDelayMs(base)
    );
  };
  tick();
}

function rememberProctoringTab(tabId, windowId) {
  if (tabId != null) state.proctoringTabId = tabId;
  if (windowId != null) state.proctoringWindowId = windowId;
}

async function ensureOffscreenDocument() {
  if (!chrome.offscreen?.createDocument) {
    throw new Error('Offscreen API not available. Please use Chrome 109 or newer.');
  }
  const offscreenUrl = chrome.runtime.getURL('offscreen.html');
  const contexts = await chrome.runtime.getContexts({
    contextTypes: ['OFFSCREEN_DOCUMENT'],
    documentUrls: [offscreenUrl],
  });
  if (contexts?.length > 0) return;
  await chrome.offscreen.createDocument({
    url: 'offscreen.html',
    reasons: ['USER_MEDIA'],
    justification: 'Record entire-screen proctoring while the candidate uses other tabs.',
  });
}

async function closeOffscreenDocument() {
  try {
    if (chrome.offscreen?.closeDocument) {
      const contexts = await chrome.runtime.getContexts({ contextTypes: ['OFFSCREEN_DOCUMENT'] });
      if (contexts?.length > 0) await chrome.offscreen.closeDocument();
    }
  } catch (e) {
    console.warn('closeOffscreenDocument', e);
  }
}

async function resolveOffscreenDocumentId(maxAttempts = 12) {
  const offscreenUrl = chrome.runtime.getURL('offscreen.html');
  for (let i = 0; i < maxAttempts; i++) {
    try {
      const contexts = await chrome.runtime.getContexts({
        contextTypes: ['OFFSCREEN_DOCUMENT'],
        documentUrls: [offscreenUrl],
      });
      const documentId = contexts?.[0]?.documentId;
      if (documentId) return documentId;
    } catch (e) {
      console.warn('resolveOffscreenDocumentId', e);
    }
    await new Promise((r) => setTimeout(r, 50 + i * 25));
  }
  return null;
}

async function messageOffscreen(payload) {
  await ensureOffscreenDocument();
  const documentId = await resolveOffscreenDocumentId();
  if (!documentId) {
    throw new Error('Offscreen capture document is not ready. Reload the interview page and try again.');
  }
  const msg = Object.assign({ target: 'offscreen' }, payload || {});
  // Broadcast to extension contexts — documentId in sendMessage options is not supported on all Chrome builds.
  return chrome.runtime.sendMessage(msg);
}

async function chooseEntireScreenStreamId(tab) {
  if (!chrome.desktopCapture?.chooseDesktopMedia) {
    throw new Error('Screen capture is not available in this browser.');
  }
  if (!tab || tab.id == null) {
    throw new Error('Missing interview tab for screen share.');
  }
  try {
    await chrome.tabs.update(tab.id, { active: true });
    if (tab.windowId != null) await chrome.windows.update(tab.windowId, { focused: true });
  } catch (_) {
    /* ignore */
  }
  return new Promise((resolve, reject) => {
    chrome.desktopCapture.chooseDesktopMedia(['screen', 'window', 'tab'], tab, (streamId) => {
      if (chrome.runtime.lastError) {
        reject(new Error(chrome.runtime.lastError.message));
        return;
      }
      if (!streamId) {
        reject(new Error('Screen share was cancelled.'));
        return;
      }
      resolve(streamId);
    });
  });
}

async function getOffscreenCaptureState() {
  try {
    return await messageOffscreen({ type: 'OFFSCREEN_GET_STATE' });
  } catch (_) {
    return null;
  }
}

async function offscreenCaptureActive() {
  const res = await getOffscreenCaptureState();
  return Boolean(res?.hasLiveStream);
}

/** Live share with video dimensions — safe to skip picker and ensure recorder. */
async function offscreenPipelineReady() {
  const res = await getOffscreenCaptureState();
  return Boolean(res?.pipelineReady || (res?.hasLiveStream && res?.videoWidth >= 8));
}

function clearStaleSetupLock() {
  if (
    completeTestSetupInFlight &&
    completeTestSetupStartedAt > 0 &&
    Date.now() - completeTestSetupStartedAt > SETUP_LOCK_STALE_MS
  ) {
    console.warn('Risebird proctoring: clearing stale setup lock');
    completeTestSetupInFlight = false;
    completeTestSetupStartedAt = 0;
  }
}

async function ensureProctoringSessionStarted() {
  const sessionKey = getSessionKey();
  if (state.sessionActive && state.backendStartConfirmedKey === sessionKey) {
    return { ok: true, alreadyActive: true };
  }
  if (!state.sessionToken || !state.joined) {
    return { ok: false, error: 'Join session first (reload dashboard or use extension popup).' };
  }
  if (state.backendJoinConfirmedKey !== sessionKey) {
    const joinRes = await completeBackendJoinAfterShare();
    if (!joinRes.ok) return joinRes;
  }
  try {
    await apiPost(PROCTORING_API.start, { interviewId: state.interviewId }, true);
  } catch (err) {
    return { ok: false, error: err?.message || 'Server could not start the exam session.' };
  }
  state.sessionActive = true;
  state.backendStartConfirmedKey = sessionKey;
  if (!state.sessionActiveSince) state.sessionActiveSince = Date.now();
  await chrome.storage.local.set({ sessionActive: true, proctoringSessionJoined: true });
  await persistSessionToStorage();
  startTabAndWindowFocusMonitoring();
  startHeartbeat();
  startScreenshotFallbackTimer();
  scheduleOffscreenMediaKeepAlive();
  scheduleUploadQueueAlarm();
  void processUploadQueue();
  if (await offscreenCaptureActive()) {
    void armMediaCaptureAfterSessionStart();
  }
  return { ok: true };
}

function isCaptureStaleState(hasLiveStream, pipelineReady) {
  if (!state.sessionActive || !state.joined) return false;
  if (state.contentScreenShareActive) return false;
  if (hasLiveStream && pipelineReady) return false;
  const since = state.sessionActiveSince || 0;
  if (!since || Date.now() - since < CAPTURE_STALE_MS) return false;
  const mediaCount = (state.screenshotUploads || 0) + (state.screenChunkUploads || 0);
  if (mediaCount > 0 && hasLiveStream) return false;
  return !hasLiveStream || mediaCount === 0;
}

async function resetScreenPickerForSession() {
  screenPickerAttemptedKey = '';
  try {
    await chrome.storage.local.remove('proctoringScreenPickerAttemptedKey');
  } catch (_) {
    /* ignore */
  }
}

/** Pause capture after abrupt interview stop; keep join so resume can re-pick entire screen once. */
async function softInterruptProctoringInternal() {
  stopScreenshotTimer();
  stopOffscreenMediaKeepAlive();
  if (state.proctoringTabId != null) {
    try {
      await chrome.tabs.sendMessage(state.proctoringTabId, { type: 'CONTENT_SCREEN_SHARE_CLEANUP' });
    } catch (_) {
      /* tab may be closed */
    }
  }
  state.contentScreenShareActive = false;
  try {
    await messageOffscreen({ type: 'OFFSCREEN_STOP_CAPTURE' });
  } catch (_) {
    /* ignore */
  }
  state.sessionActive = false;
  state.sessionActiveSince = 0;
  await chrome.storage.local.set({ sessionActive: false });
}

/** Re-share entire screen only when content confirms server bootstrap resumeCaptureAfterInterrupt. */
async function resumeCaptureAfterInterruptInternal(tabId, tabUrl, senderWindowId) {
  if (!state.joined || !state.sessionToken) {
    return { ok: false, error: 'Join session first (reload the interview page).' };
  }
  await resetScreenPickerForSession();
  const sessionRes = await ensureProctoringSessionStartedWithRetry();
  if (!sessionRes.ok) return sessionRes;
  try {
    await ensureOffscreenDocument();
    await resolveOffscreenDocumentId();
  } catch (warmErr) {
    return { ok: false, error: warmErr?.message || 'Could not prepare screen capture.' };
  }
  try {
    await messageOffscreen({ type: 'OFFSCREEN_STOP_CAPTURE' });
  } catch (_) {
    /* ignore */
  }
  rememberProctoringTab(tabId, senderWindowId);
  return beginEntireScreenShare(tabId, tabUrl, senderWindowId, { forceNewPicker: true });
}

async function nudgeCaptureRecoveryOnInterviewTab(source) {
  if (state.contentScreenShareActive) return;
  if (!state.proctoringTabId) return;
  const now = Date.now();
  if (now - (state.lastCaptureRecoveryAt || 0) < CAPTURE_REPICK_COOLDOWN_MS) return;
  state.lastCaptureRecoveryAt = now;
  try {
    await chrome.tabs.sendMessage(state.proctoringTabId, {
      type: 'NOTIFY_CAPTURE_RECOVERY',
      source: source || 'worker',
    });
  } catch (_) {
    /* tab may be closed */
  }
}

async function runWorkerCaptureRecoveryIfStale(_source) {
  const hasLiveStream = await offscreenCaptureActive();
  const pipelineReady = await offscreenPipelineReady();
  if (hasLiveStream) {
    if (state.sessionActive) {
      await armMediaCaptureAfterSessionStart();
    }
    if (!pipelineReady) {
      await messageOffscreen({ type: 'OFFSCREEN_ENSURE_RECORDING' }).catch(() => {});
      await messageOffscreen({ type: 'OFFSCREEN_CAPTURE_SCREENSHOT_NOW' }).catch(() => {});
      await nudgeCaptureRecoveryOnInterviewTab('recorder_only');
    }
    return;
  }
  if (state.sessionActive && !hasLiveStream && state.proctoringTabId != null) {
    try {
      await chrome.tabs.sendMessage(state.proctoringTabId, { type: 'NOTIFY_SHARE_RECOVERY_NEEDED' });
    } catch (_) {
      /* ignore */
    }
  }
}

async function ensureProctoringSessionStartedWithRetry(maxAttempts = 10) {
  for (let attempt = 0; attempt < maxAttempts; attempt++) {
    const res = await ensureProctoringSessionStarted();
    if (res.ok) return res;
    const waitMs = Math.min(4000, 250 + attempt * 350);
    await new Promise((r) => setTimeout(r, waitMs));
  }
  return {
    ok: false,
    error: 'POST /start did not succeed after retries. Check network and reload the interview page.',
  };
}

/** Content-script getDisplayMedia monitor share is active — arm session without offscreen picker. */
async function activateContentScreenShare(tabId, senderWindowId) {
  const joinRes = await completeBackendJoinAfterShare();
  if (!joinRes.ok) return joinRes;
  const sessionRes = await ensureProctoringSessionStartedWithRetry();
  if (!sessionRes.ok) return sessionRes;
  state.contentScreenShareActive = true;
  rememberProctoringTab(tabId, senderWindowId);
  startTabAndWindowFocusMonitoring();
  stopHeartbeat();
  startHeartbeat();
  stopScreenshotTimer();
  startScreenshotFallbackTimer();
  scheduleUploadQueueAlarm();
  void processUploadQueue();
  try {
    await chrome.tabs.sendMessage(tabId, {
      type: 'START_CONTENT_MONITORING',
      settings: getContentMonitoringSettings(),
    });
  } catch (_) {
    /* content script may already be monitoring */
  }
  try {
    await chrome.tabs.sendMessage(tabId, { type: 'NOTIFY_PROCTORING_STARTED' });
  } catch (_) {
    /* non-fatal — share is already active in the page */
  }
  await syncFlowPhaseToStorage({
    isSessionJoined: true,
    isSessionStarted: true,
    isScreenShareActive: true,
    hasValidMonitorShare: true,
    isProctoringActive: true,
    shareStopped: false,
  });
  setTimeout(() => void openProctoringPopupWindow(), 500);
  return {
    ok: true,
    contentScreenShare: true,
    joined: true,
    sessionStarted: true,
    backendReady: true,
  };
}

async function finalizeProctoringAfterCapture(tabId, senderWindowId, sessionKey) {
  if (!state.joined || state.backendJoinConfirmedKey !== sessionKey) {
    const joinRes = await completeBackendJoinAfterShare();
    if (!joinRes.ok) {
      return Object.assign({}, joinRes, {
        captureMayBeLive: await offscreenCaptureActive(),
        retry: true,
        doNotRetryPicker: true,
        pickerInvoked: true,
      });
    }
  }

  const sessionRes = await ensureProctoringSessionStartedWithRetry();
  if (!sessionRes.ok) {
    return Object.assign({}, sessionRes, {
      captureMayBeLive: await offscreenCaptureActive(),
      retry: true,
      doNotRetryPicker: true,
      pickerInvoked: true,
    });
  }

  proctoringPrepareKey = sessionKey;
  state.initProctoringCompletedKey = sessionKey;
  await chrome.storage.local.set({ proctoringPrepareKey });

  await armMediaCaptureAfterSessionStart();
  await messageOffscreen({ type: 'OFFSCREEN_CAPTURE_SCREENSHOT_NOW' });

  try {
    const startRes = await chrome.tabs.sendMessage(tabId, {
      type: 'START_CONTENT_MONITORING',
      settings: getContentMonitoringSettings(),
    });
    if (startRes && startRes.ok === false) {
      return {
        ok: false,
        error: startRes?.error || 'Could not start page monitoring.',
        captureMayBeLive: await offscreenCaptureActive(),
        retry: true,
        doNotRetryPicker: true,
        pickerInvoked: true,
      };
    }
    await chrome.tabs.sendMessage(tabId, { type: 'NOTIFY_PROCTORING_STARTED' });
  } catch (err) {
    return {
      ok: false,
      error: 'Could not reach this page. Refresh the interview tab and ensure the extension is enabled.',
      captureMayBeLive: await offscreenCaptureActive(),
      retry: true,
      doNotRetryPicker: true,
      pickerInvoked: true,
    };
  }

  state.contentScreenShareActive = true;
  scheduleOffscreenMediaKeepAlive();
  scheduleUploadQueueAlarm();
  void processUploadQueue();
  await syncFlowPhaseToStorage({
    isSessionJoined: true,
    isSessionStarted: true,
    isScreenShareActive: true,
    hasValidMonitorShare: true,
    isProctoringActive: true,
    isPromptingScreenShare: false,
    shareStopped: false,
  });
  setTimeout(() => void openProctoringPopupWindow(), 500);
  void processUploadQueue();
  return {
    ok: true,
    pickerInvoked: true,
    joined: true,
    sessionStarted: true,
    backendReady: true,
  };
}

async function activateExistingScreenShare(tabId, senderWindowId) {
  rememberProctoringTab(tabId, senderWindowId);
  if (!(await offscreenCaptureActive())) {
    return {
      ok: false,
      error: 'Entire screen share is not active. Share entire screen once from the interview page.',
      doNotRetryPicker: true,
      screenShareLost: true,
    };
  }
  if (!(await offscreenPipelineReady())) {
    await messageOffscreen({ type: 'OFFSCREEN_ENSURE_RECORDING' });
  }
  await armMediaCaptureAfterSessionStart();
  return finalizeProctoringAfterCapture(tabId, senderWindowId, getSessionKey());
}

async function resumeProctoringAfterReloadInternal(tabId, tabUrl, senderWindowId, payload) {
  if (payload) {
    try {
      await applyJoinPayloadFromHandshake(payload, {
        tab: { id: tabId, url: tabUrl, windowId: senderWindowId },
      });
    } catch (err) {
      return { ok: false, error: err?.message || String(err) };
    }
  }
  await resolveApiOrigin({ tab: { id: tabId, url: tabUrl } });
  rememberProctoringTab(tabId, senderWindowId);

  for (let attempt = 0; attempt < 24; attempt++) {
    if (await offscreenCaptureActive()) {
      if (!state.joined || state.backendJoinConfirmedKey !== getSessionKey()) {
        const joinRes = await completeBackendJoinAfterShare();
        if (!joinRes.ok) return joinRes;
      }
      if (!state.sessionActive || state.backendStartConfirmedKey !== getSessionKey()) {
        const startRes = await ensureProctoringSessionStartedWithRetry();
        if (!startRes.ok) return startRes;
      }
      await armMediaCaptureAfterSessionStart();
      await messageOffscreen({ type: 'OFFSCREEN_CAPTURE_SCREENSHOT_NOW' }).catch(() => {});
      await syncFlowPhaseToStorage({
        isSessionJoined: true,
        isSessionStarted: true,
        isScreenShareActive: true,
        hasValidMonitorShare: true,
        isProctoringActive: true,
        isPromptingScreenShare: false,
        shareStopped: false,
      });
      return {
        ok: true,
        resumed: true,
        sessionStarted: true,
        backendReady: true,
      };
    }
    await new Promise((r) => setTimeout(r, 250));
  }

  return {
    ok: false,
    error: 'Screen share needs to be re-established — please share your entire screen again.',
    retry: true,
  };
}

/** Phase 1: join + offscreen warm — no /start and no screen picker until share succeeds. */
async function prepareProctoringBackend(tabId, tabUrl, senderWindowId) {
  if (!isAllowedProctoringPageUrl(tabUrl)) {
    return { ok: false, error: 'Proctoring runs only on the AI Interview page.' };
  }
  if (!validateHost(tabUrl)) {
    return {
      ok: false,
      error: 'This page host is not allowed for proctoring. Open the interview from the Risebird portal.',
    };
  }
  if (!state.joined || !state.sessionToken) {
    return { ok: false, error: 'Join session first (reload the interview page).' };
  }
  if (await maybeEndSessionIfExpired('before_prepare_proctoring')) {
    return { ok: false, error: 'Session already expired. Please restart from AI Interview.' };
  }
  rememberProctoringTab(tabId, senderWindowId);
  try {
    await ensureOffscreenDocument();
    await resolveOffscreenDocumentId();
  } catch (warmErr) {
    return { ok: false, error: warmErr?.message || 'Could not prepare screen capture.' };
  }
  return { ok: true, proctoringReady: true, sessionStarted: false };
}

/** Phase 2: entire-screen picker + capture. promptFirst opens the native picker before API join/prepare. */
async function beginEntireScreenShare(tabId, tabUrl, senderWindowId, options = {}) {
  const forceNewPicker = options.forceNewPicker === true;
  const promptFirst = options.promptFirst === true;
  const bootstrapPayload = options.payload || null;
  clearStaleSetupLock();
  const sessionKey = getSessionKey();

  if (!forceNewPicker && (await offscreenCaptureActive())) {
    return activateExistingScreenShare(tabId, senderWindowId);
  }

  if (completeTestSetupInFlight) {
    if (promptFirst && !(await offscreenCaptureActive())) {
      clearStaleSetupLock();
    }
    if (!forceNewPicker) {
      for (let wait = 0; wait < 30; wait++) {
        await new Promise((r) => setTimeout(r, 400));
        if (await offscreenCaptureActive()) {
          return activateExistingScreenShare(tabId, senderWindowId);
        }
        if (!completeTestSetupInFlight) break;
      }
    }
    if (completeTestSetupInFlight && promptFirst && !(await offscreenCaptureActive())) {
      clearStaleSetupLock();
    }
    if (completeTestSetupInFlight) {
      return { ok: false, error: 'Proctoring setup is already in progress.', doNotRetryPicker: true, retry: true };
    }
  }

  if (!isAllowedProctoringPageUrl(tabUrl)) {
    return { ok: false, error: 'Proctoring runs only on the AI Interview page.' };
  }

  if (bootstrapPayload) {
    try {
      await applyJoinPayloadFromHandshake(bootstrapPayload, {
        tab: { id: tabId, url: tabUrl, windowId: senderWindowId },
      });
    } catch (err) {
      return { ok: false, error: err?.message || String(err), doNotRetryPicker: true };
    }
  }

  if (!validateHost(tabUrl)) {
    return {
      ok: false,
      error: 'This page host is not allowed for proctoring. Open the interview from the Risebird portal.',
      doNotRetryPicker: true,
    };
  }

  if (!state.sessionToken) {
    return { ok: false, error: 'Join session first (reload the interview page).' };
  }
  if (await maybeEndSessionIfExpired('before_start_proctoring')) {
    return { ok: false, error: 'Session already expired. Please restart from AI Interview.' };
  }

  rememberProctoringTab(tabId, senderWindowId);

  // POST /join before the screen picker — join does not require an active capture stream.
  const joinBeforeShare = await completeBackendJoinAfterShare();
  if (!joinBeforeShare.ok) {
    return Object.assign({}, joinBeforeShare, { doNotRetryPicker: true, retry: true });
  }

  if (promptFirst) {
    await syncFlowPhaseToStorage({ isPromptingScreenShare: true });
    void ensureOffscreenDocument().catch((e) => console.warn('offscreen prewarm before picker', e));
  } else {
    if (!state.joined) {
      return { ok: false, error: 'Join session first (reload the interview page).' };
    }
    if (!forceNewPicker && proctoringPrepareKey !== sessionKey) {
      const prep = await prepareProctoringBackend(tabId, tabUrl, senderWindowId);
      if (!prep.ok) return prep;
    }
  }

  if (!forceNewPicker && screenPickerAttemptedKey === sessionKey) {
    for (let wait = 0; wait < 24; wait++) {
      if (await offscreenCaptureActive()) {
        return activateExistingScreenShare(tabId, senderWindowId);
      }
      await new Promise((r) => setTimeout(r, 250));
    }
    const retryFinalize = await finalizeProctoringAfterCapture(tabId, senderWindowId, sessionKey);
    if (retryFinalize.ok) {
      return Object.assign({}, retryFinalize, { reusedExistingScreenShare: true });
    }
    // Stream is confirmed dead — clear the stale picker key so a fresh share can be shown.
    screenPickerAttemptedKey = null;
    await chrome.storage.local.remove('proctoringScreenPickerAttemptedKey');
    // Fall through to the new picker flow below.
  }

  if (screenPickerInFlight && !forceNewPicker) {
    return { ok: false, inProgress: true, doNotRetryPicker: true, retry: true };
  }

  completeTestSetupInFlight = true;
  completeTestSetupStartedAt = Date.now();
  screenPickerInFlight = true;
  try {
    rememberProctoringTab(tabId, senderWindowId);

    let interviewTab;
    try {
      interviewTab = await chrome.tabs.get(tabId);
    } catch (_) {
      return { ok: false, error: 'Could not access the interview tab.' };
    }

    await syncFlowPhaseToStorage({ isPromptingScreenShare: true });

    try {
      await ensureOffscreenDocument();
      await resolveOffscreenDocumentId();
    } catch (warmErr) {
      return { ok: false, error: warmErr?.message || 'Could not prepare screen capture.', retry: true };
    }

    let streamId;
    try {
      streamId = await chooseEntireScreenStreamId(interviewTab);
    } catch (pickErr) {
      const pickMsg = pickErr?.message || 'Entire screen share was not granted.';
      const cancelled = /cancel/i.test(pickMsg);
      await syncFlowPhaseToStorage({ isPromptingScreenShare: false });
      return {
        ok: false,
        error: pickMsg,
        pickerCancelled: cancelled,
        pickerInvoked: true,
        doNotRetryPicker: true,
      };
    }

    screenPickerAttemptedKey = sessionKey;
    await chrome.storage.local.set({ proctoringScreenPickerAttemptedKey: sessionKey });

    await syncFlowPhaseToStorage({ isPromptingScreenShare: false });

    /* Attach desktop stream immediately — streamId expires if join/start APIs run first. */
    const offRes = await messageOffscreen({
      type: 'OFFSCREEN_START_CAPTURE',
      streamId,
      settings: getContentMonitoringSettings(),
    });
    if (!offRes?.ok) {
      const errMsg = offRes?.error || 'Screen capture could not start.';
      const invalidShare = /entire screen|not tab or window/i.test(errMsg);
      if (invalidShare) {
        await resetScreenPickerForSession();
      }
      return {
        ok: false,
        error: errMsg,
        doNotRetryPicker: !invalidShare,
        invalidShare,
        pickerInvoked: true,
      };
    }

    const finalizeRes = await finalizeProctoringAfterCapture(tabId, senderWindowId, sessionKey);
    if (!finalizeRes.ok) {
      if (!finalizeRes.captureMayBeLive) {
        await messageOffscreen({ type: 'OFFSCREEN_STOP_CAPTURE' });
      }
      return finalizeRes;
    }
    return finalizeRes;
  } finally {
    screenPickerInFlight = false;
    completeTestSetupInFlight = false;
    completeTestSetupStartedAt = 0;
  }
}

async function endExamInternal() {
  stopTabAndWindowFocusMonitoring();
  stopHeartbeat();
  stopScreenshotTimer();
  stopOffscreenMediaKeepAlive();
  stopSessionExpiryTimer();
  clearUploadQueueAlarm();
  if (state.proctoringTabId != null) {
    try {
      await chrome.tabs.sendMessage(state.proctoringTabId, { type: 'CONTENT_SCREEN_SHARE_CLEANUP' });
    } catch (_) {
      /* tab may be closed */
    }
  }
  state.contentScreenShareActive = false;
  state.initProctoringCompletedKey = '';
  state.backendJoinConfirmedKey = '';
  state.backendStartConfirmedKey = '';
  await syncFlowPhaseToStorage({ shareStopped: false, isProctoringActive: false });
  state.flowPhase = PROCTORING_FLOW_PHASE.SESSION_ENDED;
  try {
    await messageOffscreen({ type: 'OFFSCREEN_STOP_CAPTURE' });
  } catch (_) {
    /* ignore */
  }
  screenPickerAttemptedKey = null;
  proctoringPrepareKey = null;
  await chrome.storage.local.remove([
    'proctoringScreenPickerAttemptedKey',
    'proctoringPrepareKey',
    'proctoringInterruptResumePendingKey',
    'proctoringShareEstablishedKey',
    'proctoringAutoSharePromptKey',
    'proctoringBackendJoinKey',
    'proctoringBackendStartKey',
  ]);
  await closeOffscreenDocument();
  for (let i = 0; i < 5; i++) {
    await processUploadQueue();
    const remaining = await dbQueueGetAll();
    if (!remaining.length) break;
    await new Promise((r) => setTimeout(r, 500));
  }
  const tabs = await chrome.tabs.query({});
  for (const t of tabs) {
    if (t.id && t.url && /^https?:/.test(t.url)) {
      try {
        await chrome.tabs.sendMessage(t.id, { type: 'STOP_CONTENT_MONITORING' });
      } catch (_) {
        /* tab may not run content script */
      }
    }
  }
  await chrome.storage.local.set({ sessionActive: false, proctoringSessionJoined: false });
  try {
    await apiPost(PROCTORING_API.stop, { interviewId: state.interviewId }, true);
  } catch (_) {
    /* backend optional */
  }
  state.sessionActive = false;
  state.sessionActiveSince = 0;
  state.lastCaptureRecoveryAt = 0;
  state.joined = false;
  state.recentTabHosts = [];
  state.lastKnownTabContext = {
    hostname: '',
    url: '',
    title: '',
    tabId: null,
    windowId: null,
    updatedAt: 0,
  };
  state.autoPopupShownForSessionKey = '';
  state.sessionExpiresAtMs = null;
  state.proctoringTabId = null;
  state.proctoringWindowId = null;
  await resetSessionDiagnostics();
  await persistSessionToStorage();
  await syncDynamicRules([]);
}

async function persistSessionToStorage() {
  try {
    await chrome.storage.local.set({
      proctoringLastJoin: {
        name: state.name || '',
        email: state.email || '',
        sessionToken: state.sessionToken || '',
        interviewId: state.interviewId != null ? state.interviewId : '',
        sessionExpiresAtMs: state.sessionExpiresAtMs != null ? state.sessionExpiresAtMs : '',
        updatedAt: Date.now(),
      },
      proctoringSessionJoined: state.joined === true,
      proctoringSessionSnapshot: {
        apiOrigin: state.apiOrigin || '',
        restrictedDomains: state.policy.restrictedDomains || [],
        allowedExamHosts: state.policy.allowedExamHosts || [],
        clipboardBlocking: state.policy.clipboardBlocking !== false,
        screenshotIntervalMs: state.screenshotIntervalMs || SCREENSHOT_MS,
        screenChunkIntervalMs: state.screenChunkIntervalMs || SCREEN_CHUNK_MS,
        sessionExpiresAtMs: state.sessionExpiresAtMs != null ? state.sessionExpiresAtMs : '',
      },
      proctoringDiagnostics: {
        screenshotUploads: state.screenshotUploads,
        lastScreenshotAt: state.lastScreenshotAt,
        screenChunkUploads: state.screenChunkUploads,
        lastScreenChunkAt: state.lastScreenChunkAt,
      },
      sessionActive: state.sessionActive === true,
      proctoringBackendJoinKey: state.backendJoinConfirmedKey || '',
      proctoringBackendStartKey: state.backendStartConfirmedKey || '',
    });
  } catch (e) {
    console.warn('persistSessionToStorage', e);
  }
}

async function syncDiagnosticsToStorage() {
  try {
    await chrome.storage.local.set({
      proctoringDiagnostics: {
        screenshotUploads: state.screenshotUploads,
        lastScreenshotAt: state.lastScreenshotAt,
        screenChunkUploads: state.screenChunkUploads,
        lastScreenChunkAt: state.lastScreenChunkAt,
        updatedAt: Date.now(),
      },
    });
  } catch (e) {
    console.warn('syncDiagnosticsToStorage', e);
  }
}

async function resolveApiOrigin(senderTab) {
  if (state.apiOrigin) return;
  if (senderTab?.url && /^https?:/.test(senderTab.url)) {
    state.apiOrigin = new URL(senderTab.url).origin;
    return;
  }
  const tabs = await chrome.tabs.query({ active: true, currentWindow: true });
  const t = tabs[0];
  if (t?.url && /^https?:/.test(t.url)) {
    state.apiOrigin = new URL(t.url).origin;
  }
}

/** Open extension popup once per session when screen share + proctoring are active. */
async function openProctoringPopupWindow(force = false) {
  await persistSessionToStorage();
  await syncDiagnosticsToStorage();
  const sessionKey = getSessionKey();
  if (!sessionKey) {
    console.warn('Risebird proctoring: open popup skipped — interviewId not set');
    return;
  }
  try {
    const stored = await chrome.storage.local.get(['proctoringAutoPopupShownFor']);
    if (!force && stored.proctoringAutoPopupShownFor === sessionKey) {
      return;
    }
  } catch (_) {
    /* ignore */
  }
  const popupUrl = chrome.runtime.getURL('popup.html');
  try {
    if (typeof chrome.action?.openPopup === 'function') {
      await chrome.action.openPopup();
      state.autoPopupShownForSessionKey = sessionKey;
      await chrome.storage.local.set({ proctoringAutoPopupShownFor: sessionKey });
      return;
    }
  } catch (e) {
    console.warn('Risebird proctoring: chrome.action.openPopup', e);
  }
  try {
    const extOrigin = chrome.runtime.getURL('');
    const wins = await chrome.windows.getAll({ populate: true });
    const existing = wins.find(
      (w) =>
        w.type === 'popup' &&
        Array.isArray(w.tabs) &&
        w.tabs.some((t) => t.url && t.url.startsWith(extOrigin) && t.url.includes('popup.html'))
    );
    if (existing?.id != null) {
      await chrome.windows.update(existing.id, { focused: true, drawAttention: true });
      state.autoPopupShownForSessionKey = sessionKey;
      await chrome.storage.local.set({ proctoringAutoPopupShownFor: sessionKey });
      return;
    }
    let left;
    let top = 80;
    try {
      const anchor = await chrome.windows.getLastFocused({ populate: false });
      if (anchor?.left != null && anchor?.width != null) {
        left = Math.max(0, anchor.left + anchor.width - 460);
      }
      if (anchor?.top != null) {
        top = Math.max(0, anchor.top + 60);
      }
    } catch (_) {
      /* ignore */
    }
    await chrome.windows.create({
      url: popupUrl,
      type: 'popup',
      width: 440,
      height: 720,
      focused: true,
      ...(left != null ? { left } : {}),
      top,
    });
    state.autoPopupShownForSessionKey = sessionKey;
    await chrome.storage.local.set({ proctoringAutoPopupShownFor: sessionKey });
  } catch (e2) {
    console.warn('Risebird proctoring: windows.create fallback', e2);
  }
}

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  const msgType = message?.type;
  if (message?.target === 'offscreen') {
    return false;
  }
  if (msgType && String(msgType).startsWith('OFFSCREEN_')) {
    return false;
  }
  (async () => {
    if (message?.type === 'INIT_PROCTORING_SESSION' || message?.type === 'JOIN_DASHBOARD_HANDSHAKE') {
      const p = message.payload || {};
      const handshakeTabUrl = sender.tab?.url || '';
      if (!isAllowedProctoringPageUrl(handshakeTabUrl)) {
        sendResponse({
          ok: false,
          error: 'Proctoring runs only on the AI Interview page (AIInterview/Index).',
        });
        return;
      }
      try {
        sendResponse(await initProctoringSessionInternal(p, sender));
      } catch (err) {
        sendResponse({ ok: false, error: err?.message || String(err) });
      }
      return;
    }

    if (message?.type === 'UPDATE_PROCTORING_FLOW_STATE') {
      await syncFlowPhaseToStorage(message.flow || {});
      sendResponse({ ok: true, flowPhase: state.flowPhase });
      return;
    }

    if (message?.type === 'JOIN_SESSION') {
      const p = message.payload || {};
      const tabsJoin = await chrome.tabs.query({ active: true, currentWindow: true });
      const joinPageUrl = tabsJoin[0]?.url || '';
      if (!isAllowedProctoringPageUrl(joinPageUrl)) {
        sendResponse({
          ok: false,
          error: 'Open the AI Interview tab first. Proctoring only runs on that page.',
        });
        return;
      }
      const interviewId = parseInterviewId(p.interviewId);
      if (!interviewId) {
        sendResponse({ ok: false, error: 'Invalid Interview ID.' });
        return;
      }
      if (!p.sessionToken || !String(p.sessionToken).trim()) {
        sendResponse({ ok: false, error: 'Session token is required.' });
        return;
      }
      /* Optional web access is requested from the popup (user gesture), not here. */
      state.sessionToken = p.sessionToken;
      state.interviewId = interviewId;
      state.name = p.name || '';
      state.email = p.email || '';
      state.screenshotIntervalMs = sanitizeMs(p.screenshotIntervalMs, SCREENSHOT_MS, 10000, 300000);
      state.screenChunkIntervalMs = sanitizeMs(p.screenChunkIntervalMs, SCREEN_CHUNK_MS, 10000, 120000);
      setSessionExpiry(p.sessionExpiresAtMs || p.meetingEndIso || p.meetingEndMs);
      await resolveApiOrigin(sender.tab);
      if (!state.apiOrigin) {
        sendResponse({ ok: false, error: 'Open the Risebird portal tab first.' });
        return;
      }
      await persistSessionToStorage();
      try {
        await apiPost(
          PROCTORING_API.join,
          {
            interviewId: state.interviewId,
            sessionToken: state.sessionToken,
            name: state.name,
            email: state.email,
          },
          false
        );
      } catch (err) {
        console.warn('JOIN_SESSION api', err);
        sendResponse({ ok: false, error: err?.message || String(err) });
        return;
      }
      state.joined = true;
      state.backendJoinConfirmedKey = getSessionKey();
      if (!state.sessionActive) {
        await resetSessionDiagnostics();
      }
      await persistSessionToStorage();
      startTabAndWindowFocusMonitoring();
      void ensureOffscreenDocument().catch((e) => console.warn('offscreen prewarm on join', e));
      sendResponse({
        ok: true,
        joined: true,
        sessionStarted: false,
        message: 'Joined. Share entire screen from the interview page to start proctoring.',
      });
      return;
    }

    if (message?.type === 'PROCTORING_ARM_JOIN') {
      const tabId = sender.tab?.id;
      const tabUrl = sender.tab?.url || '';
      if (!tabId) {
        sendResponse({ ok: false, error: 'No tab context.' });
        return;
      }
      const p = message.payload || {};
      try {
        await applyJoinPayloadFromHandshake(p, {
          tab: { id: tabId, url: tabUrl, windowId: sender.tab?.windowId },
        });
        await resolveApiOrigin(sender.tab);
        sendResponse(await completeBackendJoinAfterShare());
      } catch (err) {
        sendResponse({ ok: false, error: err?.message || String(err) });
      }
      return;
    }

    if (message?.type === 'ENSURE_SESSION_STARTED') {
      const sessionKey = getSessionKey();
      if (!state.joined || state.backendJoinConfirmedKey !== sessionKey) {
        const joinRes = await completeBackendJoinAfterShare();
        if (!joinRes.ok) {
          sendResponse(joinRes);
          return;
        }
      }
      sendResponse(await ensureProctoringSessionStartedWithRetry());
      return;
    }

    if (message?.type === 'PREPARE_PROCTORING') {
      const tabId = sender.tab?.id;
      const tabUrl = sender.tab?.url || '';
      if (!tabId) {
        sendResponse({ ok: false, error: 'No tab context.' });
        return;
      }
      await resolveApiOrigin(sender.tab);
      try {
        sendResponse(await prepareProctoringBackend(tabId, tabUrl, sender.tab?.windowId));
      } catch (err) {
        sendResponse({ ok: false, error: err?.message || String(err) });
      }
      return;
    }

    if (message?.type === 'CLEAR_STALE_SCREEN_PICKER_STATE') {
      if (message.force === true && !(await offscreenCaptureActive())) {
        screenPickerAttemptedKey = null;
        screenPickerInFlight = false;
        await chrome.storage.local.remove('proctoringScreenPickerAttemptedKey');
        clearStaleSetupLock();
      }
      sendResponse({ ok: true });
      return;
    }

    if (message?.type === 'RESUME_PROCTORING_AFTER_RELOAD') {
      const tabId = sender.tab?.id;
      const tabUrl = sender.tab?.url || '';
      if (!tabId) {
        sendResponse({ ok: false, error: 'No tab context.' });
        return;
      }
      try {
        sendResponse(
          await resumeProctoringAfterReloadInternal(
            tabId,
            tabUrl,
            sender.tab?.windowId,
            message.payload || null
          )
        );
      } catch (err) {
        sendResponse({ ok: false, error: err?.message || String(err) });
      }
      return;
    }

    if (message?.type === 'BEGIN_SCREEN_SHARE') {
      const tabId = sender.tab?.id;
      const tabUrl = sender.tab?.url || '';
      if (!tabId) {
        sendResponse({ ok: false, error: 'No tab context.' });
        return;
      }
      await resolveApiOrigin(sender.tab);
      try {
        sendResponse(
          await beginEntireScreenShare(tabId, tabUrl, sender.tab?.windowId, {
            forceNewPicker: message.forceNewPicker === true,
            promptFirst: message.promptFirst === true,
            payload: message.payload || null,
          })
        );
      } catch (err) {
        sendResponse({
          ok: false,
          error: 'Could not reach this page. Refresh the interview tab and ensure the extension is enabled.',
          retry: true,
        });
      }
      return;
    }

    if (message?.type === 'GET_PROCTORING_SETUP_STATE') {
      const hasLiveStream = await offscreenCaptureActive();
      const pipelineReady = await offscreenPipelineReady();
      const sessionKey = getSessionKey();
      sendResponse({
        ok: true,
        joined: state.joined,
        sessionActive: state.sessionActive,
        backendJoinConfirmed: state.backendJoinConfirmedKey === sessionKey,
        backendStartConfirmed: state.backendStartConfirmedKey === sessionKey,
        hasLiveStream,
        pipelineReady,
        proctoringReady: proctoringPrepareKey === sessionKey,
        setupInProgress: completeTestSetupInFlight,
        pickerAttempted: screenPickerAttemptedKey === sessionKey,
        captureStale: isCaptureStaleState(hasLiveStream, pipelineReady),
        contentScreenShareActive: state.contentScreenShareActive === true,
        screenshotUploads: state.screenshotUploads || 0,
        screenChunkUploads: state.screenChunkUploads || 0,
        sessionActiveSince: state.sessionActiveSince || 0,
      });
      return;
    }

    if (message?.type === 'CONTENT_SCREEN_SHARE_STARTED') {
      const tabId = sender.tab?.id;
      const tabUrl = sender.tab?.url || '';
      if (!tabId) {
        sendResponse({ ok: false, error: 'No tab context.' });
        return;
      }
      if (!isAllowedProctoringPageUrl(tabUrl)) {
        sendResponse({ ok: false, error: 'Proctoring runs only on the AI Interview page.' });
        return;
      }
      await resolveApiOrigin(sender.tab);
      try {
        sendResponse(await activateContentScreenShare(tabId, sender.tab?.windowId));
      } catch (err) {
        sendResponse({ ok: false, error: err?.message || String(err) });
      }
      return;
    }

    if (message?.type === 'CONTENT_SCREEN_SHARE_STOPPED') {
      state.contentScreenShareActive = false;
      await syncFlowPhaseToStorage(
        Object.assign({}, message.flow || {}, {
          isScreenShareActive: false,
          hasValidMonitorShare: false,
          isProctoringActive: false,
          shareStopped: true,
        })
      );
      if (state.proctoringTabId != null) {
        try {
          await chrome.tabs.sendMessage(state.proctoringTabId, { type: 'NOTIFY_SHARE_RECOVERY_NEEDED' });
        } catch (_) {
          /* tab may be reloading */
        }
      }
      sendResponse({ ok: true });
      return;
    }

    if (message?.type === 'PROCTORING_VIOLATION') {
      const p = message.payload || {};
      const violationType = p.violationType || 'unknown';
      const msg = p.message || violationType;
      const meta = Object.assign({}, p.metadata || {}, {
        violationType,
        occurredAt: p.occurredAt || new Date().toISOString(),
      });
      await recordWorkerEvent('violation', msg, meta);
      sendResponse({ ok: true });
      return;
    }

    if (message?.type === 'RESET_SCREEN_PICKER_FOR_SESSION') {
      await resetScreenPickerForSession();
      sendResponse({ ok: true });
      return;
    }

    if (message?.type === 'SOFT_INTERRUPT_PROCTORING') {
      try {
        await softInterruptProctoringInternal();
        sendResponse({ ok: true });
      } catch (err) {
        sendResponse({ ok: false, error: err?.message || String(err) });
      }
      return;
    }

    if (message?.type === 'RESUME_CAPTURE_AFTER_INTERRUPT') {
      if (message.allowResume !== true) {
        sendResponse({ ok: false, error: 'Resume capture not allowed for this page.' });
        return;
      }
      const tabId = sender.tab?.id;
      const tabUrl = sender.tab?.url || '';
      if (!tabId) {
        sendResponse({ ok: false, error: 'No tab context.' });
        return;
      }
      await resolveApiOrigin(sender.tab);
      try {
        sendResponse(await resumeCaptureAfterInterruptInternal(tabId, tabUrl, sender.tab?.windowId));
      } catch (err) {
        sendResponse({ ok: false, error: err?.message || String(err) });
      }
      return;
    }

    if (message?.type === 'REQUEST_CAPTURE_RECOVERY') {
      await runWorkerCaptureRecoveryIfStale('manual');
      sendResponse({ ok: true });
      return;
    }

    if (
      message?.type === 'START_PROCTORING' ||
      message?.type === 'START_EXAM_MONITORING' ||
      message?.type === 'COMPLETE_TEST_SETUP'
    ) {
      const tabId = sender.tab?.id;
      const tabUrl = sender.tab?.url || '';
      if (!tabId) {
        sendResponse({ ok: false, error: 'No tab context.' });
        return;
      }
      await resolveApiOrigin(sender.tab);
      try {
        const prep = await prepareProctoringBackend(tabId, tabUrl, sender.tab?.windowId);
        if (!prep.ok) {
          sendResponse(prep);
          return;
        }
        const result = await beginEntireScreenShare(tabId, tabUrl, sender.tab?.windowId, {
          forceNewPicker: message.forceNewPicker === true,
        });
        sendResponse(result);
      } catch (err) {
        sendResponse({
          ok: false,
          error: 'Could not reach this page. Refresh the interview tab and ensure the extension can access it.',
        });
      }
      return;
    }

    if (message?.type === 'QUERY_OFFSCREEN_CAPTURE') {
      const hasLiveStream = await offscreenCaptureActive();
      sendResponse({ ok: true, hasLiveStream });
      return;
    }

    if (message?.type === 'START_SESSION') {
      const tabs = await chrome.tabs.query({ active: true, currentWindow: true });
      let tab = tabs[0];
      if (state.proctoringTabId != null) {
        try {
          tab = await chrome.tabs.get(state.proctoringTabId);
        } catch (_) {
          /* use active tab */
        }
      }
      if (!tab?.id) {
        sendResponse({ ok: false, error: 'No active tab.' });
        return;
      }
      if (await maybeEndSessionIfExpired('before_start_session')) {
        sendResponse({ ok: false, error: 'Session already expired. Please restart from AI Interview.' });
        return;
      }
      if (!isAllowedProctoringPageUrl(tab.url || '')) {
        sendResponse({
          ok: false,
          error: 'Switch to the AI Interview tab. Proctoring only runs on that page.',
        });
        return;
      }
      await resolveApiOrigin(tab);
      const saved = await chrome.storage.local.get(['proctoringLastJoin']);
      const j = saved.proctoringLastJoin;
      if (!state.joined && j?.sessionToken) {
        try {
          const initRes = await initProctoringSessionInternal({ ...j, deferSessionStart: true }, { tab });
          if (!initRes.ok) {
            sendResponse(initRes);
            return;
          }
        } catch (err) {
          sendResponse({ ok: false, error: err?.message || String(err) });
          return;
        }
      }
      if (!state.joined || !state.sessionToken) {
        sendResponse({ ok: false, error: 'Join session first (open AI Interview page).' });
        return;
      }
      if (state.contentScreenShareActive && state.sessionActive) {
        sendResponse({
          ok: true,
          alreadyActive: true,
          message: 'Screen sharing is already active.',
        });
        return;
      }
      if (await offscreenCaptureActive()) {
        sendResponse(await activateExistingScreenShare(tab.id, tab.windowId));
        return;
      }
      const resumeRes = await beginEntireScreenShare(tab.id, tab.url || '', tab.windowId, {
        forceNewPicker: false,
        promptFirst: false,
      });
      if (resumeRes.ok) {
        sendResponse(Object.assign({ ok: true, resumed: true }, resumeRes));
        return;
      }
      if (resumeRes.pickerAlreadyAttempted || resumeRes.doNotRetryPicker) {
        sendResponse({
          ok: false,
          error:
            resumeRes.error ||
            'Screen was already shared once. Reload the interview page if proctoring did not start.',
        });
        return;
      }
      try {
        await chrome.tabs.sendMessage(tab.id, { type: 'REQUEST_MANUAL_SCREEN_SHARE' });
        sendResponse({ ok: true, manualShareRequested: true });
      } catch (err) {
        sendResponse({
          ok: false,
          error: 'Could not reach the interview tab. Refresh the page and try again.',
        });
      }
      return;
    }

    if (message?.type === 'END_EXAM') {
      await endExamInternal();
      sendResponse({ ok: true });
      return;
    }

    if (message?.type === 'UPDATE_SETTINGS') {
      state.settings.clipboardBlocking = Boolean(message.settings?.clipboardBlocking);
      sendResponse({ ok: true });
      return;
    }

    if (message?.type === 'GET_STATE') {
      await loadEventsFromStorageIfEmpty();
      const stored = await chrome.storage.local.get(['proctoringContentFlow', 'proctoringFlowPhase']);
      const contentFlow = stored.proctoringContentFlow || {};
      const flowPhase = stored.proctoringFlowPhase || computeFlowPhase(contentFlow);
      const hasLiveStream = await offscreenCaptureActive();
      const pipelineReady = await offscreenPipelineReady();
      sendResponse({
        ok: true,
        state: {
          interviewId: state.interviewId,
          joined: state.joined,
          sessionActive: state.sessionActive,
          sessionStarted: state.sessionActive,
          contentScreenShareActive: state.contentScreenShareActive,
          hasLiveStream,
          pipelineReady,
          flowPhase,
          flow: contentFlow,
          violations: state.violations,
          settings: { clipboardBlocking: state.settings.clipboardBlocking },
          events: state.events,
          hasWebMonitoringAccess: await hasOptionalWebOrigins(),
          screenshotUploads: state.screenshotUploads,
          lastScreenshotAt: state.lastScreenshotAt,
          screenChunkUploads: state.screenChunkUploads,
          lastScreenChunkAt: state.lastScreenChunkAt,
        },
      });
      return;
    }

    if (message?.type === 'OPEN_PROCTORING_POPUP_ONCE') {
      await openProctoringPopupWindow(false);
      sendResponse({ ok: true });
      return;
    }

    if (message?.type === 'GET_REPORT') {
      await loadEventsFromStorageIfEmpty();
      sendResponse({
        ok: true,
        report: {
          interviewId: state.interviewId,
          violations: state.violations,
          events: state.events,
          screenshotUploads: state.screenshotUploads,
          lastScreenshotAt: state.lastScreenshotAt,
          screenChunkUploads: state.screenChunkUploads,
          lastScreenChunkAt: state.lastScreenChunkAt,
        },
      });
      return;
    }

    if (message?.type === 'CONTENT_EVENT') {
      const meta = message.metadata || {};
      if (
        meta.event === 'screen_share_ended' &&
        !state.contentScreenShareActive &&
        state.proctoringTabId != null
      ) {
        try {
          await chrome.tabs.sendMessage(state.proctoringTabId, { type: 'NOTIFY_SCREEN_SHARE_LOST' });
        } catch (_) {
          /* ignore */
        }
      }
      await recordWorkerEvent(message.level || 'info', message.reason || '', meta);
      sendResponse({ ok: true });
      return;
    }

    if (message?.type === 'UPLOAD_SCREENSHOT') {
      const p = message.payload || {};
      const uploadResult = await uploadScreenshotPayload(p);
      sendResponse(uploadResult);
      return;
    }

    if (message?.type === 'UPLOAD_SCREEN_CHUNK') {
      const p = message.payload || {};
      const uploadResult = await uploadScreenChunkPayload(p);
      sendResponse(uploadResult);
      return;
    }

    sendResponse({ ok: false, error: 'Unknown message.' });
  })().catch((err) => sendResponse({ ok: false, error: err?.message || String(err) }));

  return true;
});

/** Dedupe DNR + webNavigation both firing for the same blocked navigation. */
const restrictedBlockDedupe = new Map();

function shouldRecordRestrictedBlock(url) {
  const now = Date.now();
  const prev = restrictedBlockDedupe.get(url);
  if (prev && now - prev < 2000) return false;
  restrictedBlockDedupe.set(url, now);
  if (restrictedBlockDedupe.size > 100) {
    const cutoff = now - 120000;
    for (const [u, t] of restrictedBlockDedupe) {
      if (t < cutoff) restrictedBlockDedupe.delete(u);
    }
  }
  return true;
}

function emitRestrictedNavigationBlocked(requestUrl, extraMeta = {}) {
  if (!state.sessionToken) return;
  if (!state.joined && !state.sessionActive) return;
  if (!requestUrl || !/^https?:/i.test(requestUrl)) return;
  if (!shouldRecordRestrictedBlock(requestUrl)) return;
  let hostname = '';
  try {
    hostname = new URL(requestUrl).hostname;
  } catch (_) {}
  void recordWorkerEvent(
    'violation',
    `Blocked — restricted URL not loaded (${hostname || 'unknown host'})`,
    {
      eventCategory: 'restricted_domain',
      blocked: true,
      blockedHostname: hostname || undefined,
      requestUrl: requestUrl.slice(0, 800),
      ...extraMeta,
    }
  );
}

function initRestrictedDomainEventLogging() {
  try {
    if (chrome.declarativeNetRequest?.onRuleMatched) {
      chrome.declarativeNetRequest.onRuleMatched.addListener((details) => {
        const requestUrl = details.request?.url || '';
        const ruleId = details.rule?.ruleId ?? details.ruleId;
        emitRestrictedNavigationBlocked(requestUrl, {
          ruleId,
          detectionSource: 'dnr_rule_matched',
        });
      });
    }
  } catch (e) {
    console.warn('initRestrictedDomainEventLogging dnr', e);
  }
  try {
    if (chrome.webNavigation?.onErrorOccurred) {
      chrome.webNavigation.onErrorOccurred.addListener((details) => {
        if (details.frameId !== 0) return;
        const err = String(details.error || '').toLowerCase();
        if (!err.includes('blocked')) return;
        const requestUrl = details.url || '';
        emitRestrictedNavigationBlocked(requestUrl, {
          detectionSource: 'web_navigation_error',
          errorText: details.error,
        });
      });
    }
  } catch (e) {
    console.warn('initRestrictedDomainEventLogging webNavigation', e);
  }
}
initRestrictedDomainEventLogging();

/** After SW sleep/restart, restore token, policy, DNR, and tab listeners so events keep recording. */
async function restoreSessionFromStorageOnStartup() {
  try {
    const data = await chrome.storage.local.get([
      'sessionActive',
      'proctoringLastJoin',
      'proctoringSessionSnapshot',
      'proctoringSessionJoined',
      'proctoringDiagnostics',
      'proctoringScreenPickerAttemptedKey',
      'proctoringPrepareKey',
      'proctoringBackendJoinKey',
      'proctoringBackendStartKey',
    ]);
    if (data.proctoringScreenPickerAttemptedKey) {
      screenPickerAttemptedKey = String(data.proctoringScreenPickerAttemptedKey);
    }
    if (data.proctoringPrepareKey) {
      proctoringPrepareKey = String(data.proctoringPrepareKey);
    }
    if (data.proctoringBackendJoinKey) {
      state.backendJoinConfirmedKey = String(data.proctoringBackendJoinKey);
    }
    if (data.proctoringBackendStartKey) {
      state.backendStartConfirmedKey = String(data.proctoringBackendStartKey);
    }
    const j = data.proctoringLastJoin;
    const snap = data.proctoringSessionSnapshot;
    if (snap && typeof snap === 'object') {
      state.apiOrigin = String(snap.apiOrigin || '').replace(/\/$/, '');
      state.policy.restrictedDomains = Array.isArray(snap.restrictedDomains) ? snap.restrictedDomains : [];
      state.policy.allowedExamHosts = Array.isArray(snap.allowedExamHosts) ? snap.allowedExamHosts : [];
      state.policy.clipboardBlocking = snap.clipboardBlocking !== false;
      state.screenshotIntervalMs = sanitizeMs(snap.screenshotIntervalMs, SCREENSHOT_MS, 10000, 300000);
      state.screenChunkIntervalMs = sanitizeMs(snap.screenChunkIntervalMs, SCREEN_CHUNK_MS, 10000, 120000);
      setSessionExpiry(snap.sessionExpiresAtMs);
      state.settings.clipboardBlocking = state.policy.clipboardBlocking;
    }
    const diag = data.proctoringDiagnostics;
    if (diag && typeof diag === 'object') {
      state.screenshotUploads = Number(diag.screenshotUploads) || 0;
      state.lastScreenshotAt = Number(diag.lastScreenshotAt) || null;
      state.screenChunkUploads = Number(diag.screenChunkUploads) || 0;
      state.lastScreenChunkAt = Number(diag.lastScreenChunkAt) || null;
    }
    let joinedFlag = data.proctoringSessionJoined === true;
    if (
      !joinedFlag &&
      data.proctoringSessionJoined === undefined &&
      j?.sessionToken &&
      data.sessionActive === true
    ) {
      joinedFlag = true;
    }
    if (j && j.sessionToken && joinedFlag) {
      const id = parseInterviewId(j.interviewId);
      if (id) {
        state.sessionToken = String(j.sessionToken);
        state.interviewId = id;
        state.name = j.name || '';
        state.email = j.email || '';
        if (!state.sessionExpiresAtMs) {
          setSessionExpiry(j.sessionExpiresAtMs || j.meetingEndIso);
        }
        state.joined = true;
      } else {
        state.joined = false;
      }
    }
    if (data.sessionActive === true) {
      state.sessionActive = true;
      const mediaCount = (state.screenshotUploads || 0) + (state.screenChunkUploads || 0);
      state.sessionActiveSince =
        mediaCount > 0 ? Date.now() : Date.now() - CAPTURE_STALE_MS;
    }
    if (!state.sessionToken) return;
    if (!state.joined && !state.sessionActive) return;
    if (!state.apiOrigin) {
      await resolveApiOrigin(null);
    }
    try {
      await syncDynamicRules(state.policy.restrictedDomains || []);
    } catch (e) {
      console.warn('restoreSessionFromStorageOnStartup DNR', e);
    }
    startTabAndWindowFocusMonitoring();
    await maybeEndSessionIfExpired('restore_startup');
    if (state.joined && !state.sessionActive) {
      void ensureProctoringSessionStartedWithRetry().catch((e) =>
        console.warn('restore startup start', e)
      );
    }
    if (state.sessionActive) {
      startHeartbeat();
      startScreenshotFallbackTimer();
      scheduleUploadQueueAlarm();
      scheduleOffscreenMediaKeepAlive();
      void processUploadQueue();
      void ensureOffscreenDocument().catch(() => {});
      void (async () => {
        if (await offscreenCaptureActive()) {
          await armMediaCaptureAfterSessionStart();
        }
      })();
    }
  } catch (e) {
    console.warn('restoreSessionFromStorageOnStartup', e);
  }
}

try {
  self.addEventListener('online', () => {
    void onProctoringNetworkOnline();
  });
} catch (e) {
  console.warn('proctoring online listener', e);
}

try {
  chrome.alarms.onAlarm.addListener((alarm) => {
    if (alarm?.name === UPLOAD_QUEUE_ALARM) {
      void processUploadQueue();
    }
  });
} catch (e) {
  console.warn('alarms.onAlarm', e);
}

void restoreSessionFromStorageOnStartup();
