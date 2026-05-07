/**
 * Risebird Proctoring — MV3 service worker.
 * Coordinates session state, DNR rules, backend posts, heartbeat, and screenshots.
 */
const DYNAMIC_RULE_ID_START = 10000;
const HEARTBEAT_MS = 30000;
const SCREENSHOT_MS = 60000;
const SCREEN_CHUNK_FAILURE_COOLDOWN_MS = 60000;
const SESSION_EXPIRY_CHECK_MS = 15000;
const EVENT_LOG_STORAGE_KEY = 'proctoringEvents';
const MAX_STORED_EVENTS = 400;

const state = {
  joined: false,
  sessionActive: false,
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
  dynamicRuleIds: [],
  screenshotUploads: 0,
  lastScreenshotAt: null,
  screenChunkUploads: 0,
  lastScreenChunkAt: null,
  lastScreenChunkFailureAt: 0,
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
};

let tabActivatedListener = null;
let windowFocusListener = null;

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
      '/event',
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

async function apiPost(path, body, useBearer) {
  const base = (state.apiOrigin || '').replace(/\/$/, '');
  if (!base) {
    throw new Error('API origin is missing. Open the Risebird portal in a tab and try again.');
  }
  const headers = { 'Content-Type': 'application/json' };
  if (useBearer && state.sessionToken) {
    headers.Authorization = 'Bearer ' + state.sessionToken;
  }
  const url = base + '/api/proctoring/session' + path;
  const res = await fetch(url, {
    method: 'POST',
    headers,
    body: JSON.stringify(body ?? {}),
  });
  if (!res.ok) {
    const t = await res.text();
    throw new Error(t || res.statusText);
  }
  try {
    return await res.json();
  } catch {
    return {};
  }
}

function sanitizeMs(value, fallback, min, max) {
  const n = Number(value);
  if (!Number.isFinite(n)) return fallback;
  return Math.min(Math.max(Math.round(n), min), max);
}

function getSessionKey() {
  if (!state.interviewId) return '';
  return `interview:${state.interviewId}`;
}

async function resetSessionDiagnostics() {
  state.screenshotUploads = 0;
  state.lastScreenshotAt = null;
  state.screenChunkUploads = 0;
  state.lastScreenChunkAt = null;
  state.lastScreenChunkFailureAt = 0;
  try {
    await chrome.storage.local.set({
      proctoringDiagnostics: {
        screenshotUploads: state.screenshotUploads,
        lastScreenshotAt: state.lastScreenshotAt,
      },
    });
  } catch (_) {
    /* ignore */
  }
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

function validateHost(tabUrl) {
  try {
    const host = new URL(tabUrl).hostname;
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

function startHeartbeat() {
  stopHeartbeat();
  state.heartbeatTimer = setInterval(() => {
    void maybeEndSessionIfExpired('heartbeat_tick');
    if (!state.sessionActive) return;
    if (!state.apiOrigin) return;
    apiPost(
      '/heartbeat',
      {
        interviewId: state.interviewId,
        utcTicks: Date.now(),
        timestamp: Date.now(),
        status: state.sessionActive ? 'active' : state.joined ? 'joined' : 'idle',
        violations: state.violations,
      },
      true
    ).catch((e) =>
      console.warn('proctoring heartbeat', e)
    );
  }, HEARTBEAT_MS);
}

function stopScreenshotTimer() {
  if (state.screenshotTimer) {
    clearInterval(state.screenshotTimer);
    state.screenshotTimer = null;
  }
}

async function captureAndPostScreenshot() {
  if (!state.apiOrigin || !state.sessionActive) return;
  try {
    const tabs = await chrome.tabs.query({ active: true, currentWindow: true });
    const activeTab = tabs[0];
    if (!activeTab?.url || !isAllowedProctoringPageUrl(activeTab.url)) return;
    const dataUrl = await chrome.tabs.captureVisibleTab(activeTab.windowId || null, { format: 'jpeg', quality: 60 });
    const base64 = dataUrl.split(',')[1];
    await apiPost('/screenshot', { interviewId: state.interviewId, imageBase64: base64 }, true);
    state.screenshotUploads += 1;
    state.lastScreenshotAt = Date.now();
    await chrome.storage.local.set({
      proctoringDiagnostics: {
        screenshotUploads: state.screenshotUploads,
        lastScreenshotAt: state.lastScreenshotAt,
      },
    });
  } catch (e) {
    console.warn('proctoring screenshot', e);
    await recordWorkerEvent('violation', 'Screenshot upload failed', {
      eventCategory: 'screenshot',
      event: 'screenshot_upload_failed',
      error: e?.message || String(e),
      interviewId: state.interviewId,
    });
  }
}

function startScreenshotTimer() {
  stopScreenshotTimer();
  state.screenshotTimer = setInterval(() => captureAndPostScreenshot(), state.screenshotIntervalMs || SCREENSHOT_MS);
}

async function endExamInternal() {
  stopTabAndWindowFocusMonitoring();
  stopHeartbeat();
  stopScreenshotTimer();
  stopSessionExpiryTimer();
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
    await apiPost('/stop', { interviewId: state.interviewId }, true);
  } catch (_) {
    /* backend optional */
  }
  state.sessionActive = false;
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
        sessionExpiresAtMs: state.sessionExpiresAtMs != null ? state.sessionExpiresAtMs : '',
      },
      proctoringDiagnostics: {
        screenshotUploads: state.screenshotUploads,
        lastScreenshotAt: state.lastScreenshotAt,
      },
    });
  } catch (e) {
    console.warn('persistSessionToStorage', e);
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

/** Open extension popup when AI Interview page completes join (toolbar popup or fallback window). */
async function openProctoringPopupWindow() {
  const sessionKey = getSessionKey();
  if (!sessionKey) return;
  try {
    const data = await chrome.storage.local.get(['proctoringAutoPopupShownFor']);
    if (data.proctoringAutoPopupShownFor === sessionKey || state.autoPopupShownForSessionKey === sessionKey) {
      return;
    }
  } catch (_) {
    /* ignore storage read errors */
  }
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
    const hasProctorPopup = wins.some(
      (w) =>
        w.type === 'popup' &&
        Array.isArray(w.tabs) &&
        w.tabs.some((t) => t.url && t.url.startsWith(extOrigin) && t.url.includes('popup.html'))
    );
    if (hasProctorPopup) return;
    await chrome.windows.create({
      url: chrome.runtime.getURL('popup.html'),
      type: 'popup',
      width: 440,
      height: 720,
      focused: true,
    });
    state.autoPopupShownForSessionKey = sessionKey;
    await chrome.storage.local.set({ proctoringAutoPopupShownFor: sessionKey });
  } catch (e2) {
    console.warn('Risebird proctoring: windows.create fallback', e2);
  }
}

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  (async () => {
    if (message?.type === 'JOIN_DASHBOARD_HANDSHAKE') {
      const p = message.payload || {};
      const handshakeTabUrl = sender.tab?.url || '';
      if (!isAllowedProctoringPageUrl(handshakeTabUrl)) {
        sendResponse({
          ok: false,
          error: 'Proctoring runs only on the AI Interview page (AIInterview/Index).',
        });
        return;
      }
      const interviewId = parseInterviewId(p.interviewId);
      if (!interviewId) {
        sendResponse({ ok: false, error: 'Invalid Interview ID from dashboard.' });
        return;
      }
      if (!p.sessionToken || !String(p.sessionToken).trim()) {
        sendResponse({ ok: false, error: 'Missing session token from dashboard.' });
        return;
      }
      /* Do not call chrome.permissions.request here — it requires a user gesture and
         will fail from the service worker, blocking join before the popup opens. */
      state.sessionToken = p.sessionToken || null;
      state.interviewId = interviewId;
      state.name = p.name || '';
      state.email = p.email || '';
      state.policy.restrictedDomains = p.restrictedDomains || [];
      state.policy.allowedExamHosts = p.allowedExamHosts || [];
      state.policy.clipboardBlocking = p.clipboardBlocking !== false;
      state.screenshotIntervalMs = sanitizeMs(p.screenshotIntervalMs, SCREENSHOT_MS, 5000, 300000);
      setSessionExpiry(p.sessionExpiresAtMs || p.meetingEndIso || p.meetingEndMs);
      state.settings.clipboardBlocking = state.policy.clipboardBlocking;
      state.apiOrigin = (p.apiOrigin || '').replace(/\/$/, '');
      await resolveApiOrigin(sender.tab);
      if (!state.apiOrigin) {
        sendResponse({ ok: false, error: 'Could not resolve API origin.' });
        return;
      }
      await persistSessionToStorage();
      try {
        await syncDynamicRules(state.policy.restrictedDomains);
        await apiPost(
          '/join',
          {
            interviewId: state.interviewId,
            sessionToken: state.sessionToken,
            name: state.name,
            email: state.email,
          },
          false
        );
      } catch (err) {
        console.warn('JOIN_DASHBOARD_HANDSHAKE api', err);
        sendResponse({ ok: false, error: err?.message || String(err) });
        return;
      }
      state.joined = true;
      if (!state.sessionActive) {
        await resetSessionDiagnostics();
      }
      await persistSessionToStorage();
      startTabAndWindowFocusMonitoring();
      await openProctoringPopupWindow();
      sendResponse({ ok: true });
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
      state.screenshotIntervalMs = sanitizeMs(p.screenshotIntervalMs, SCREENSHOT_MS, 5000, 300000);
      setSessionExpiry(p.sessionExpiresAtMs || p.meetingEndIso || p.meetingEndMs);
      await resolveApiOrigin(sender.tab);
      if (!state.apiOrigin) {
        sendResponse({ ok: false, error: 'Open the Risebird portal tab first.' });
        return;
      }
      await persistSessionToStorage();
      try {
        await apiPost(
          '/join',
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
      if (!state.sessionActive) {
        await resetSessionDiagnostics();
      }
      await persistSessionToStorage();
      startTabAndWindowFocusMonitoring();
      sendResponse({ ok: true });
      return;
    }

    if (message?.type === 'COMPLETE_TEST_SETUP') {
      const tabId = sender.tab?.id;
      const tabUrl = sender.tab?.url || '';
      if (!tabId) {
        sendResponse({ ok: false, error: 'No tab context.' });
        return;
      }
      if (!isAllowedProctoringPageUrl(tabUrl)) {
        sendResponse({
          ok: false,
          error: 'Proctoring runs only on the AI Interview page.',
        });
        return;
      }
      if (!validateHost(tabUrl)) {
        sendResponse({
          ok: false,
          error: 'This page host is not allowed for proctoring. Open the interview from the Risebird portal.',
        });
        return;
      }
      /* No permissions.request here — page-driven; optional access via popup if needed. */
      if (!state.joined || !state.sessionToken) {
        sendResponse({ ok: false, error: 'Join session first (reload dashboard or use extension popup).' });
        return;
      }
      if (await maybeEndSessionIfExpired('before_complete_test_setup')) {
        sendResponse({ ok: false, error: 'Session already expired. Please restart from AI Interview.' });
        return;
      }
      await resolveApiOrigin(sender.tab);
      let startRes;
      try {
        startRes = await chrome.tabs.sendMessage(tabId, {
          type: 'START_CONTENT_MONITORING',
          settings: { clipboardBlocking: state.policy.clipboardBlocking },
        });
      } catch (err) {
        sendResponse({
          ok: false,
          error: 'Could not reach this page. Refresh the interview tab and ensure the extension can access it.',
        });
        return;
      }
      if (!startRes?.ok) {
        sendResponse({ ok: false, error: startRes?.error || 'Screen share was not started.' });
        return;
      }
      try {
        await apiPost('/start', { interviewId: state.interviewId }, true);
      } catch (err) {
        try {
          await chrome.tabs.sendMessage(tabId, { type: 'STOP_CONTENT_MONITORING' });
        } catch (_) {
          /* ignore */
        }
        await chrome.storage.local.set({ sessionActive: false });
        state.sessionActive = false;
        sendResponse({ ok: false, error: err?.message || 'Server could not start the exam session.' });
        return;
      }
      state.sessionActive = true;
      await chrome.storage.local.set({ sessionActive: true });
      startTabAndWindowFocusMonitoring();
      startHeartbeat();
      startScreenshotTimer();
      sendResponse({ ok: true });
      return;
    }

    if (message?.type === 'START_SESSION') {
      /* Popup already requests optional hosts on button click before this message. */
      await resolveApiOrigin(sender.tab);
      const tabs = await chrome.tabs.query({ active: true, currentWindow: true });
      const tab = tabs[0];
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
      let startRes;
      try {
        startRes = await chrome.tabs.sendMessage(tab.id, {
          type: 'START_CONTENT_MONITORING',
          settings: { clipboardBlocking: state.settings.clipboardBlocking },
        });
      } catch (err) {
        sendResponse({
          ok: false,
          error: 'Could not reach the active tab. Open a Risebird page and try again.',
        });
        return;
      }
      if (!startRes?.ok) {
        sendResponse({ ok: false, error: startRes?.error || 'Screen share was not started.' });
        return;
      }
      try {
        await apiPost('/start', { interviewId: state.interviewId }, true);
      } catch (err) {
        try {
          await chrome.tabs.sendMessage(tab.id, { type: 'STOP_CONTENT_MONITORING' });
        } catch (_) {
          /* ignore */
        }
        await chrome.storage.local.set({ sessionActive: false });
        state.sessionActive = false;
        sendResponse({ ok: false, error: err?.message || 'Server could not start the exam session.' });
        return;
      }
      state.sessionActive = true;
      await chrome.storage.local.set({ sessionActive: true });
      startTabAndWindowFocusMonitoring();
      startHeartbeat();
      startScreenshotTimer();
      sendResponse({ ok: true });
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
      sendResponse({
        ok: true,
        state: {
          joined: state.joined,
          sessionActive: state.sessionActive,
          violations: state.violations,
          settings: { clipboardBlocking: state.settings.clipboardBlocking },
          events: state.events,
          hasWebMonitoringAccess: await hasOptionalWebOrigins(),
          screenshotUploads: state.screenshotUploads,
          lastScreenshotAt: state.lastScreenshotAt,
        },
      });
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
        },
      });
      return;
    }

    if (message?.type === 'CONTENT_EVENT') {
      await recordWorkerEvent(message.level || 'info', message.reason || '', message.metadata || {});
      sendResponse({ ok: true });
      return;
    }

    if (message?.type === 'UPLOAD_SCREEN_CHUNK') {
      const p = message.payload || {};
      if (!state.sessionActive || !state.sessionToken) {
        sendResponse({ ok: true });
        return;
      }
      try {
        await apiPost(
          '/screen-chunk',
          {
            interviewId: state.interviewId,
            chunkSeq: p.chunkSeq,
            mimeType: p.mimeType,
            capturedAt: p.capturedAt,
            data: p.data,
          },
          true
        );
        state.screenChunkUploads += 1;
        state.lastScreenChunkAt = Date.now();
      } catch (err) {
        const now = Date.now();
        if (now - state.lastScreenChunkFailureAt >= SCREEN_CHUNK_FAILURE_COOLDOWN_MS) {
          state.lastScreenChunkFailureAt = now;
          await recordWorkerEvent('violation', 'Screen chunk upload failed', {
            eventCategory: 'screen_chunk',
            event: 'screen_chunk_upload_failed',
            error: err?.message || String(err),
            chunkSeq: p.chunkSeq,
            mimeType: p.mimeType,
            capturedAt: p.capturedAt,
          });
        }
      }
      sendResponse({ ok: true });
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
    ]);
    const j = data.proctoringLastJoin;
    const snap = data.proctoringSessionSnapshot;
    if (snap && typeof snap === 'object') {
      state.apiOrigin = String(snap.apiOrigin || '').replace(/\/$/, '');
      state.policy.restrictedDomains = Array.isArray(snap.restrictedDomains) ? snap.restrictedDomains : [];
      state.policy.allowedExamHosts = Array.isArray(snap.allowedExamHosts) ? snap.allowedExamHosts : [];
      state.policy.clipboardBlocking = snap.clipboardBlocking !== false;
      state.screenshotIntervalMs = sanitizeMs(snap.screenshotIntervalMs, SCREENSHOT_MS, 5000, 300000);
      setSessionExpiry(snap.sessionExpiresAtMs);
      state.settings.clipboardBlocking = state.policy.clipboardBlocking;
    }
    const diag = data.proctoringDiagnostics;
    if (diag && typeof diag === 'object') {
      state.screenshotUploads = Number(diag.screenshotUploads) || 0;
      state.lastScreenshotAt = Number(diag.lastScreenshotAt) || null;
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
    if (state.sessionActive) {
      startHeartbeat();
      startScreenshotTimer();
    }
  } catch (e) {
    console.warn('restoreSessionFromStorageOnStartup', e);
  }
}
void restoreSessionFromStorageOnStartup();
