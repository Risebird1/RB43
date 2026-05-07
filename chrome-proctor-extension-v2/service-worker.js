const API_PATHS = {
  JOIN: "/api/proctoring/session/join",
  START: "/api/proctoring/session/start",
  EVENT: "/api/proctoring/session/event",
  SCREENSHOT: "/api/proctoring/session/screenshot",
  SCREEN_CHUNK: "/api/proctoring/session/screen-chunk",
  HEARTBEAT: "/api/proctoring/session/heartbeat",
  STOP: "/api/proctoring/session/stop"
};

const ALARM_NAMES = {
  HEARTBEAT: "risebird-heartbeat",
  SCREENSHOT: "risebird-screenshot"
};

const STORAGE_KEY = "risebird.proctor.session.state";
const DNR_RULE_ID_BASE = 20000;
/** Chrome MV3 dynamic rule cap — leave headroom below 5000. */
const DNR_MAX_RULES = 4500;

/** Serialize DNR updates so concurrent startSession / restore never double-add the same IDs. */
let dnrSyncChain = Promise.resolve();
const RESTRICTED_BLOCK_DEDUPE_MS = 3000;
const TAB_FOCUS_LOSS_DEDUPE_MS = 2000;

/** Baseline blocklist merged with server policy (dashboard / join handshake). */
const DEFAULT_RESTRICTED_DOMAINS = [
  "google.com",
  "google.co.in",
  "google.co.uk",
  "bing.com",
  "duckduckgo.com",
  "chatgpt.com",
  "openai.com",
  "chat.openai.com",
  "stackoverflow.com",
  "stackexchange.com",
  "facebook.com",
  "instagram.com",
  "youtube.com",
  "youtu.be",
  "reddit.com",
  "linkedin.com",
  "web.whatsapp.com",
  "whatsapp.com",
  "telegram.org",
  "t.me",
  "discord.com",
  "discordapp.com",
  "quora.com",
  "copilot.microsoft.com",
  "gemini.google.com",
  "claude.ai",
  "anthropic.com"
];

const DEFAULT_POLICY = {
  screenshotIntervalSeconds: 15,
  heartbeatIntervalSeconds: 10,
  chunkIntervalMs: 5000,
  restrictedDomains: [],
  allowedHosts: []
};

const state = {
  sessionActive: false,
  backendBaseUrl: "",
  sessionToken: "",
  sessionId: "",
  tabId: null,
  candidateId: "",
  screenshotIntervalSeconds: DEFAULT_POLICY.screenshotIntervalSeconds,
  heartbeatIntervalSeconds: DEFAULT_POLICY.heartbeatIntervalSeconds,
  chunkIntervalMs: DEFAULT_POLICY.chunkIntervalMs,
  restrictedDomains: [],
  allowedHosts: [],
  dnrRuleIds: [],
  screenshotUploads: 0,
  lastScreenshotAt: null,
  lastHeartbeatAt: null,
  violationCount: 0,
  violations: [],
  eventLog: [],
  startedAt: null,
  lastError: null
};

const runtimeTelemetry = {
  blockedHostLastSeenAt: new Map(),
  lastTabFocusLossAt: 0,
  lastEvidenceCaptureAt: 0
};

/** Avoid flooding screenshot + recorder flush when violations fire in bursts (e.g. rapid clipboard attempts). */
const EVIDENCE_CAPTURE_DEBOUNCE_MS = 1200;

const NO_EVIDENCE_CAPTURE_VIOLATIONS = new Set([
  "SCREEN_CHUNK_UPLOAD_FAILED",
  "SCREENSHOT_UPLOAD_FAILED",
  "MEDIA_RECORDER_ERROR",
  "SCREEN_CHUNK_PIPELINE_ERROR",
  "SESSION_BOOTSTRAP_INVALID",
  "MONITOR_SETUP_FAILED"
]);

async function saveState() {
  await chrome.storage.local.set({ [STORAGE_KEY]: state });
}

async function restoreState() {
  const persisted = await chrome.storage.local.get(STORAGE_KEY);
  if (!persisted[STORAGE_KEY]) {
    return;
  }
  Object.assign(state, persisted[STORAGE_KEY]);
  /**
   * Older persisted snapshots could omit restrictedDomains — then DNR installs no rules and
   * isRestrictedHost() stays false, so sites open and no violations log. Re-merge defaults when active.
   */
  if (
    state.sessionActive &&
    (!Array.isArray(state.restrictedDomains) || state.restrictedDomains.length === 0)
  ) {
    state.restrictedDomains = uniqueDomains([...DEFAULT_RESTRICTED_DOMAINS]);
    try {
      await saveState();
    } catch (_e) {
      // Ignore.
    }
  }
}

function sanitizeBaseUrl(url) {
  return (url || "").trim().replace(/\/+$/, "");
}

function normalizeDomain(value) {
  return (value || "")
    .trim()
    .toLowerCase()
    .replace(/^https?:\/\//, "")
    .replace(/\/.*$/, "");
}

function uniqueDomains(values) {
  return [...new Set((values || []).map(normalizeDomain).filter(Boolean))];
}

function nowIso() {
  return new Date().toISOString();
}

function extractHostname(url) {
  try {
    return new URL(url).hostname.toLowerCase();
  } catch (_error) {
    return "";
  }
}

function isRestrictedHost(hostname) {
  if (!hostname || !Array.isArray(state.restrictedDomains) || state.restrictedDomains.length === 0) {
    return false;
  }
  return state.restrictedDomains.some((domain) => hostname === domain || hostname.endsWith(`.${domain}`));
}

/** Policy entry that matched (e.g. www.google.com → google.com). */
function matchingRestrictedRoot(hostname) {
  if (!hostname || !Array.isArray(state.restrictedDomains)) {
    return "";
  }
  const h = String(hostname).toLowerCase();
  for (const domain of state.restrictedDomains) {
    if (!domain) {
      continue;
    }
    if (h === domain || h.endsWith(`.${domain}`)) {
      return domain;
    }
  }
  return "";
}

function shouldThrottleBlockedHost(hostname) {
  const now = Date.now();
  const previous = runtimeTelemetry.blockedHostLastSeenAt.get(hostname) || 0;
  if (now - previous < RESTRICTED_BLOCK_DEDUPE_MS) {
    return true;
  }
  runtimeTelemetry.blockedHostLastSeenAt.set(hostname, now);
  return false;
}

function requireSessionContext() {
  if (!state.sessionActive || !state.backendBaseUrl || !state.sessionToken) {
    throw new Error("Proctoring session is not active.");
  }
}

async function apiPost(path, payload, options = {}) {
  if (!options.allowInactive) {
    requireSessionContext();
  }

  const baseUrl = sanitizeBaseUrl(options.baseUrl || state.backendBaseUrl);
  const token = options.sessionToken || state.sessionToken;
  const url = `${baseUrl}${path}`;
  const headers = {
    "Content-Type": "application/json"
  };

  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const response = await fetch(url, {
    method: "POST",
    headers,
    body: JSON.stringify(payload || {})
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(`HTTP ${response.status}: ${text || response.statusText}`);
  }

  const raw = await response.text();
  if (!raw) {
    return {};
  }

  try {
    return JSON.parse(raw);
  } catch (_error) {
    return { raw };
  }
}

async function recordViolation(type, message, metadata = {}) {
  const entry = {
    type,
    message,
    metadata,
    capturedAt: nowIso()
  };

  state.violationCount += 1;
  state.violations = [...state.violations.slice(-199), entry];
  appendLocalEvent("violation", type, message, metadata, entry.capturedAt);
  await saveState();

  if (state.sessionActive) {
    await postEvent("violation", message, {
      type,
      ...metadata
    }, { skipLocalLog: true }).catch(() => {
      // Keep local evidence even if event reporting fails.
    });
  }

  if (state.sessionActive && !NO_EVIDENCE_CAPTURE_VIOLATIONS.has(type)) {
    captureIncidentEvidence(type, metadata).catch(() => {
      // Best-effort still image + screen-recording flush only.
    });
  }
}

async function captureScreenshotDataUrlForExamTab() {
  const tab = await chrome.tabs.get(state.tabId);
  if (!tab || !tab.windowId) {
    throw new Error("Exam tab is unavailable.");
  }
  if (chrome.tabs.captureTab) {
    try {
      return await chrome.tabs.captureTab(state.tabId, { format: "jpeg", quality: 70 });
    } catch (_e) {
      /* Older Chrome or permission — fall back to visible tab in window. */
    }
  }
  return chrome.tabs.captureVisibleTab(tab.windowId, {
    format: "jpeg",
    quality: 70
  });
}

async function flushExamTabScreenRecording(triggerType) {
  if (!state.sessionActive || !Number.isInteger(state.tabId)) {
    return;
  }
  try {
    await chrome.tabs.sendMessage(state.tabId, {
      type: "FLUSH_SCREEN_RECORDING",
      reason: String(triggerType || "incident").slice(0, 120)
    });
  } catch (_e) {
    /* Exam tab may have no content script — ignore. */
  }
}

async function captureIncidentEvidence(triggerType, metadata = {}) {
  if (!state.sessionActive || NO_EVIDENCE_CAPTURE_VIOLATIONS.has(triggerType)) {
    return;
  }
  const now = Date.now();
  if (now - runtimeTelemetry.lastEvidenceCaptureAt < EVIDENCE_CAPTURE_DEBOUNCE_MS) {
    return;
  }
  runtimeTelemetry.lastEvidenceCaptureAt = now;

  await flushExamTabScreenRecording(triggerType);
  try {
    await uploadScreenshot({ evidenceTrigger: triggerType });
  } catch (_e) {
    /* Tab hidden / capture blocked — monitor-share flush above still helps. */
  }
}

function appendLocalEvent(level, eventType, message, metadata = {}, timestamp = nowIso()) {
  const eventEntry = {
    level,
    eventType,
    message,
    metadata,
    capturedAt: timestamp
  };
  state.eventLog = [...state.eventLog.slice(-199), eventEntry];
}

async function postEvent(eventType, message, metadata = {}, options = {}) {
  if (!state.sessionActive) {
    return;
  }

  if (!options.skipLocalLog) {
    const normalizedLevel = String(eventType || "").toLowerCase() === "violation" ? "violation" : "info";
    appendLocalEvent(normalizedLevel, eventType, message, metadata);
    await saveState();
  }

  const payload = {
    sessionId: state.sessionId,
    candidateId: state.candidateId || null,
    eventType,
    message,
    metadata,
    timestamp: nowIso()
  };

  await apiPost(API_PATHS.EVENT, payload);
}

function extractPolicy(joinResponse, startResponse, inputPolicy) {
  const candidate =
    joinResponse?.policy ||
    startResponse?.policy ||
    joinResponse?.data?.policy ||
    startResponse?.data?.policy ||
    inputPolicy ||
    {};

  return {
    screenshotIntervalSeconds:
      Number(candidate.screenshotIntervalSeconds) || DEFAULT_POLICY.screenshotIntervalSeconds,
    heartbeatIntervalSeconds:
      Number(candidate.heartbeatIntervalSeconds) || DEFAULT_POLICY.heartbeatIntervalSeconds,
    chunkIntervalMs: Number(candidate.chunkIntervalMs) || DEFAULT_POLICY.chunkIntervalMs,
    restrictedDomains: uniqueDomains([
      ...DEFAULT_RESTRICTED_DOMAINS,
      ...(Array.isArray(candidate.restrictedDomains) ? candidate.restrictedDomains : [])
    ]),
    allowedHosts: uniqueDomains(candidate.allowedHosts || [])
  };
}

async function removeAllDynamicDeclarativeRules() {
  try {
    const existing = await chrome.declarativeNetRequest.getDynamicRules();
    const removeRuleIds = existing.map((r) => r.id);
    if (removeRuleIds.length > 0) {
      await chrome.declarativeNetRequest.updateDynamicRules({ removeRuleIds, addRules: [] });
    }
  } catch (_e) {
    // DNR may be unavailable in restricted contexts — session should still proceed.
  }
}

async function syncDynamicRestrictedDomainRules() {
  const run = async () => {
    /** Always clear dynamic rules from Chrome first — persisted state.dnrRuleIds can be stale vs actual IDs in the browser. */
    await removeAllDynamicDeclarativeRules();

    const domains = uniqueDomains(state.restrictedDomains).slice(0, DNR_MAX_RULES);
    const addRules = [];
    const nextIds = [];

    for (let i = 0; i < domains.length; i += 1) {
      const domain = domains[i];
      if (!domain) {
        continue;
      }
      const id = DNR_RULE_ID_BASE + i;
      /**
       * urlFilter ||domain^ matches that registrable host and subdomains across schemes (main navigation + subresources).
       * excludedRequestDomains on every rule was redundant for per-host blocks and could mis-interact with policy; omit.
       */
      const rule = {
        id,
        priority: 2,
        action: { type: "block" },
        condition: {
          urlFilter: `||${domain}^`
        }
      };

      addRules.push(rule);
      nextIds.push(id);
    }

    if (addRules.length > 0) {
      await chrome.declarativeNetRequest.updateDynamicRules({ removeRuleIds: [], addRules });
    }
    state.dnrRuleIds = nextIds;
  };

  const previous = dnrSyncChain;
  const next = previous.then(run, run);
  dnrSyncChain = next.catch(async (err) => {
    state.lastError = `DNR: ${String(err?.message || err)}`;
    try {
      await saveState();
    } catch (_e) {
      // Ignore.
    }
  });
  return next;
}

async function setupSessionTimers() {
  await chrome.alarms.clear(ALARM_NAMES.HEARTBEAT);
  await chrome.alarms.clear(ALARM_NAMES.SCREENSHOT);

  async function createAlarmWithFallback(name, seconds) {
    const requestedMinutes = Math.max(1 / 60, Number(seconds || 0) / 60);
    try {
      await chrome.alarms.create(name, { periodInMinutes: requestedMinutes });
    } catch (_error) {
      // Some Chrome versions enforce higher minimums (for example 0.5 or 1 minute).
      // Fallback keeps session telemetry alive instead of failing timer setup.
      await chrome.alarms.create(name, { periodInMinutes: 1 });
    }
  }

  await createAlarmWithFallback(ALARM_NAMES.HEARTBEAT, state.heartbeatIntervalSeconds);
  await createAlarmWithFallback(ALARM_NAMES.SCREENSHOT, state.screenshotIntervalSeconds);
}

async function clearSessionTimers() {
  await chrome.alarms.clear(ALARM_NAMES.HEARTBEAT);
  await chrome.alarms.clear(ALARM_NAMES.SCREENSHOT);
}

function getSessionSnapshot() {
  return {
    sessionActive: state.sessionActive,
    sessionId: state.sessionId,
    tabId: state.tabId,
    screenshotUploads: state.screenshotUploads,
    lastScreenshotAt: state.lastScreenshotAt,
    lastHeartbeatAt: state.lastHeartbeatAt,
    violationCount: state.violationCount,
    restrictedDomains: state.restrictedDomains,
    allowedHosts: state.allowedHosts,
    screenshotIntervalSeconds: state.screenshotIntervalSeconds,
    heartbeatIntervalSeconds: state.heartbeatIntervalSeconds,
    chunkIntervalMs: state.chunkIntervalMs,
    recentEvents: state.eventLog.slice(-60).reverse(),
    lastError: state.lastError,
    startedAt: state.startedAt
  };
}

async function startSession(input) {
  if (state.sessionActive) {
    return getSessionSnapshot();
  }

  const backendBaseUrl = sanitizeBaseUrl(input.backendBaseUrl);
  const sessionToken = (input.sessionToken || "").trim();
  const sessionId = (input.sessionId || "").trim();

  if (!backendBaseUrl || !sessionToken || !sessionId) {
    throw new Error("backendBaseUrl, sessionToken and sessionId are required.");
  }

  const tabId = Number(input.tabId);
  state.backendBaseUrl = backendBaseUrl;
  state.sessionToken = sessionToken;
  state.sessionId = sessionId;
  state.tabId = Number.isFinite(tabId) ? tabId : null;
  state.candidateId = (input.candidateId || "").trim();
  state.screenshotUploads = 0;
  state.lastScreenshotAt = null;
  state.lastHeartbeatAt = null;
  state.violationCount = 0;
  state.violations = [];
  state.eventLog = [];
  state.startedAt = nowIso();
  state.lastError = null;
  await saveState();

  const joinPayload = {
    sessionId: state.sessionId,
    candidateId: state.candidateId || null,
    extensionVersion: chrome.runtime.getManifest().version,
    tabId: state.tabId,
    timestamp: nowIso()
  };

  const joinResponse = await apiPost(API_PATHS.JOIN, joinPayload, {
    allowInactive: true,
    baseUrl: state.backendBaseUrl,
    sessionToken: state.sessionToken
  });

  const startResponse = await apiPost(
    API_PATHS.START,
    {
      sessionId: state.sessionId,
      candidateId: state.candidateId || null,
      timestamp: nowIso()
    },
    {
      allowInactive: true,
      baseUrl: state.backendBaseUrl,
      sessionToken: state.sessionToken
    }
  );

  const policy = extractPolicy(joinResponse, startResponse, input.policy);
  state.screenshotIntervalSeconds = policy.screenshotIntervalSeconds;
  state.heartbeatIntervalSeconds = policy.heartbeatIntervalSeconds;
  state.chunkIntervalMs = policy.chunkIntervalMs;
  state.restrictedDomains = policy.restrictedDomains;
  state.allowedHosts = policy.allowedHosts;
  state.sessionActive = true;

  await syncDynamicRestrictedDomainRules();
  await setupSessionTimers();
  await postEvent("session", "Proctoring session started.", {
    restrictedDomains: state.restrictedDomains,
    allowedHosts: state.allowedHosts
  }).catch(() => {
    // Start should not fail due to a secondary event failure.
  });
  await saveState();

  return getSessionSnapshot();
}

async function stopSession(reason = "Stopped by user.") {
  if (!state.sessionActive) {
    try {
      await chrome.storage.local.remove("risebird.proctor.session.manualStopLocked");
    } catch (_e) {
      // Ignore.
    }
    return getSessionSnapshot();
  }

  await clearSessionTimers();

  try {
    await apiPost(API_PATHS.STOP, {
      sessionId: state.sessionId,
      candidateId: state.candidateId || null,
      reason,
      timestamp: nowIso(),
      violationCount: state.violationCount
    });
  } catch (_error) {
    // Always continue local cleanup.
  }

  await removeAllDynamicDeclarativeRules();

  state.sessionActive = false;
  state.dnrRuleIds = [];
  state.restrictedDomains = [];
  state.allowedHosts = [];
  state.lastError = null;
  await saveState();

  try {
    await chrome.storage.local.remove("risebird.proctor.session.manualStopLocked");
  } catch (_e) {
    // Ignore.
  }

  return getSessionSnapshot();
}

async function uploadScreenChunk(message) {
  requireSessionContext();
  const payload = {
    sessionId: state.sessionId,
    candidateId: state.candidateId || null,
    chunkSeq: message.chunkSeq,
    mimeType: message.mimeType,
    capturedAt: message.capturedAt || nowIso(),
    data: message.data,
    evidenceTrigger: message.evidenceTrigger || undefined
  };

  try {
    await apiPost(API_PATHS.SCREEN_CHUNK, payload);
    appendLocalEvent(
      "info",
      "chunk_upload",
      message.evidenceTrigger
        ? `Screen chunk #${message.chunkSeq} (${message.evidenceTrigger})`
        : `Screen chunk #${message.chunkSeq} uploaded`,
      {
        chunkSeq: message.chunkSeq,
        mimeType: message.mimeType,
        evidenceTrigger: message.evidenceTrigger || null
      }
    );
    const seq = Number(message.chunkSeq);
    if (!Number.isFinite(seq) || seq % 5 === 0) {
      await saveState();
    }
  } catch (error) {
    await recordViolation("SCREEN_CHUNK_UPLOAD_FAILED", "Screen chunk upload failed", {
      error: String(error.message || error),
      chunkSeq: message.chunkSeq
    });
    throw error;
  }
}

async function uploadScreenshot(options = {}) {
  requireSessionContext();

  if (!Number.isInteger(state.tabId)) {
    throw new Error("No active exam tab is associated with session.");
  }

  const screenshotDataUrl = await captureScreenshotDataUrlForExamTab();

  const capturedAt = nowIso();
  const payload = {
    sessionId: state.sessionId,
    candidateId: state.candidateId || null,
    tabId: state.tabId,
    capturedAt,
    data: screenshotDataUrl,
    evidenceTrigger: options.evidenceTrigger || undefined
  };

  await apiPost(API_PATHS.SCREENSHOT, payload);
  state.screenshotUploads += 1;
  state.lastScreenshotAt = capturedAt;
  const trigger = options.evidenceTrigger;
  appendLocalEvent(
    "info",
    "screenshot_tab",
    trigger ? `Exam tab screenshot (${trigger})` : "Exam tab screenshot uploaded",
    { capturedAt, evidenceTrigger: trigger || null }
  );
  await saveState();
}

async function sendHeartbeat() {
  requireSessionContext();
  const timestamp = nowIso();
  const payload = {
    sessionId: state.sessionId,
    candidateId: state.candidateId || null,
    status: "active",
    violationCount: state.violationCount,
    screenshotUploads: state.screenshotUploads,
    lastScreenshotAt: state.lastScreenshotAt,
    timestamp
  };

  await apiPost(API_PATHS.HEARTBEAT, payload);
  state.lastHeartbeatAt = timestamp;
  await saveState();
}

chrome.runtime.onInstalled.addListener(async () => {
  await restoreState();
  if (!state.sessionActive) {
    return;
  }
  try {
    await syncDynamicRestrictedDomainRules();
  } catch (_e) {
    // Rules may fail if session domains empty — continue timers.
  }
  await setupSessionTimers();
});

chrome.runtime.onStartup.addListener(async () => {
  await restoreState();
  if (!state.sessionActive) {
    return;
  }
  try {
    await syncDynamicRestrictedDomainRules();
  } catch (_e) {
    // Same as onInstalled.
  }
  await setupSessionTimers();
});

chrome.alarms.onAlarm.addListener(async (alarm) => {
  if (!state.sessionActive) {
    return;
  }

  if (alarm.name === ALARM_NAMES.HEARTBEAT) {
    try {
      await sendHeartbeat();
    } catch (error) {
      state.lastError = String(error.message || error);
      await saveState();
    }
    return;
  }

  if (alarm.name === ALARM_NAMES.SCREENSHOT) {
    try {
      await uploadScreenshot();
    } catch (error) {
      await recordViolation("SCREENSHOT_UPLOAD_FAILED", "Screenshot upload failed", {
        error: String(error.message || error)
      });
      state.lastError = String(error.message || error);
      await saveState();
    }
  }
});

chrome.webRequest.onErrorOccurred.addListener(
  (details) => {
    (async () => {
      if (!state.sessionActive) {
        return;
      }
      const errStr = String(details.error || "");
      if (!errStr.includes("ERR_BLOCKED_BY_CLIENT")) {
        return;
      }

      const hostname = extractHostname(details.url || "");
      if (!isRestrictedHost(hostname)) {
        return;
      }
      if (shouldThrottleBlockedHost(hostname)) {
        return;
      }

      const restrictedDomain = matchingRestrictedRoot(hostname);
      await recordViolation(
        "RESTRICTED_DOMAIN_BLOCKED",
        `Restricted domain blocked: ${hostname}${restrictedDomain && restrictedDomain !== hostname ? ` (policy domain: ${restrictedDomain})` : ""}`,
        {
          url: details.url,
          hostname,
          restrictedDomain: restrictedDomain || hostname,
          tabId: details.tabId,
          frameId: details.frameId,
          requestId: details.requestId,
          error: details.error
        }
      );
    })().catch(() => {
      // Best-effort telemetry only.
    });
  },
  { urls: ["<all_urls>"] }
);

/** If DNR misses a navigation, detect restricted URL in the exam tab, log, and go back. */
chrome.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
  (async () => {
    if (!state.sessionActive || !Number.isInteger(state.tabId) || tabId !== state.tabId) {
      return;
    }
    const url = changeInfo.url || tab?.url || "";
    if (!url || !/^https?:\/\//i.test(url)) {
      return;
    }
    const hostname = extractHostname(url);
    if (!hostname || !isRestrictedHost(hostname)) {
      return;
    }
    if (shouldThrottleBlockedHost(hostname)) {
      return;
    }
    const restrictedDomain = matchingRestrictedRoot(hostname);
    await recordViolation(
      "RESTRICTED_DOMAIN_NAVIGATION",
      `Exam tab opened restricted site: ${hostname}${restrictedDomain && restrictedDomain !== hostname ? ` (policy domain: ${restrictedDomain})` : ""}`,
      {
        hostname,
        restrictedDomain: restrictedDomain || hostname,
        url,
        tabId
      }
    );
    try {
      await chrome.tabs.goBack(tabId);
    } catch (_e) {
      // No history or tab closed — block already attempted via DNR.
    }
  })().catch(() => {
    // Best-effort.
  });
});

chrome.tabs.onActivated.addListener((activeInfo) => {
  (async () => {
    if (!state.sessionActive || !Number.isInteger(state.tabId)) {
      return;
    }
    if (activeInfo.tabId === state.tabId) {
      return;
    }

    const now = Date.now();
    if (now - runtimeTelemetry.lastTabFocusLossAt < TAB_FOCUS_LOSS_DEDUPE_MS) {
      return;
    }
    runtimeTelemetry.lastTabFocusLossAt = now;

    await recordViolation("EXAM_TAB_FOCUS_LOST", "Exam tab lost focus due to tab switch.", {
      examTabId: state.tabId,
      activeTabId: activeInfo.tabId,
      capturedAt: nowIso()
    });
  })().catch(() => {
    // Best-effort telemetry only.
  });
});

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  (async () => {
    switch (message?.type) {
      case "START_SESSION": {
        const payload = message.payload || {};
        if (!payload.tabId && typeof sender?.tab?.id === "number") {
          payload.tabId = sender.tab.id;
        }
        const snapshot = await startSession(payload);
        sendResponse({ ok: true, state: snapshot });
        return;
      }
      case "STOP_SESSION": {
        const force = message.force === true;
        const lockData = await chrome.storage.local.get("risebird.proctor.session.manualStopLocked");
        const locked = lockData["risebird.proctor.session.manualStopLocked"] === true;
        if (locked && !force) {
          sendResponse({
            ok: false,
            error: "Stop is disabled until the interview ends or you leave the interview tab."
          });
          return;
        }
        const snapshot = await stopSession(message.reason || "Stopped from extension popup.");
        sendResponse({ ok: true, state: snapshot });
        return;
      }
      case "UPLOAD_SCREEN_CHUNK": {
        await uploadScreenChunk(message.payload || {});
        sendResponse({ ok: true });
        return;
      }
      case "REPORT_VIOLATION": {
        const payload = message.payload || {};
        await recordViolation(payload.type || "UNKNOWN", payload.message || "Violation captured", payload.metadata || {});
        sendResponse({ ok: true, violationCount: state.violationCount });
        return;
      }
      case "SESSION_EVENT": {
        const payload = message.payload || {};
        await postEvent(payload.eventType || "event", payload.message || "Session event", payload.metadata || {});
        sendResponse({ ok: true });
        return;
      }
      case "GET_STATE": {
        await restoreState();
        const snapshot = getSessionSnapshot();
        const lockData = await chrome.storage.local.get("risebird.proctor.session.manualStopLocked");
        snapshot.manualStopLocked = lockData["risebird.proctor.session.manualStopLocked"] === true;
        sendResponse({ ok: true, state: snapshot });
        return;
      }
      default:
        sendResponse({ ok: false, error: "Unknown message type." });
    }
  })().catch(async (error) => {
    state.lastError = String(error.message || error);
    await saveState();
    sendResponse({ ok: false, error: state.lastError });
  });

  return true;
});
