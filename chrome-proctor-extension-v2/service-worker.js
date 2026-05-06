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
const RESTRICTED_BLOCK_DEDUPE_MS = 3000;
const TAB_FOCUS_LOSS_DEDUPE_MS = 2000;

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
  popupOpenBySession: new Map()
};

async function saveState() {
  await chrome.storage.local.set({ [STORAGE_KEY]: state });
}

async function restoreState() {
  const persisted = await chrome.storage.local.get(STORAGE_KEY);
  if (!persisted[STORAGE_KEY]) {
    return;
  }
  Object.assign(state, persisted[STORAGE_KEY]);
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
    restrictedDomains: uniqueDomains(candidate.restrictedDomains || []),
    allowedHosts: uniqueDomains(candidate.allowedHosts || [])
  };
}

async function syncDynamicRestrictedDomainRules() {
  const removeRuleIds = [...state.dnrRuleIds];
  const addRules = [];
  const nextIds = [];

  for (let index = 0; index < state.restrictedDomains.length; index += 1) {
    const domain = state.restrictedDomains[index];
    const id = DNR_RULE_ID_BASE + index;
    const rule = {
      id,
      priority: 1,
      action: { type: "block" },
      condition: {
        urlFilter: `||${domain}^`,
        resourceTypes: ["main_frame", "sub_frame"]
      }
    };

    if (state.allowedHosts.length > 0) {
      rule.condition.excludedRequestDomains = state.allowedHosts;
    }

    addRules.push(rule);
    nextIds.push(id);
  }

  await chrome.declarativeNetRequest.updateDynamicRules({ removeRuleIds, addRules });
  state.dnrRuleIds = nextIds;
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
    recentEvents: state.eventLog.slice(-30).reverse(),
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

  const removeRuleIds = [...state.dnrRuleIds];
  if (removeRuleIds.length > 0) {
    await chrome.declarativeNetRequest.updateDynamicRules({ removeRuleIds, addRules: [] });
  }

  state.sessionActive = false;
  state.dnrRuleIds = [];
  state.restrictedDomains = [];
  state.allowedHosts = [];
  state.lastError = null;
  await saveState();

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
    data: message.data
  };

  try {
    await apiPost(API_PATHS.SCREEN_CHUNK, payload);
  } catch (error) {
    await recordViolation("SCREEN_CHUNK_UPLOAD_FAILED", "Screen chunk upload failed", {
      error: String(error.message || error),
      chunkSeq: message.chunkSeq
    });
    throw error;
  }
}

async function uploadScreenshot() {
  requireSessionContext();

  if (!Number.isInteger(state.tabId)) {
    throw new Error("No active exam tab is associated with session.");
  }

  const tab = await chrome.tabs.get(state.tabId);
  if (!tab || !tab.windowId) {
    throw new Error("Exam tab is unavailable.");
  }

  const screenshotDataUrl = await chrome.tabs.captureVisibleTab(tab.windowId, {
    format: "jpeg",
    quality: 70
  });

  const capturedAt = nowIso();
  const payload = {
    sessionId: state.sessionId,
    candidateId: state.candidateId || null,
    tabId: state.tabId,
    capturedAt,
    data: screenshotDataUrl
  };

  await apiPost(API_PATHS.SCREENSHOT, payload);
  state.screenshotUploads += 1;
  state.lastScreenshotAt = capturedAt;
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

async function openProctoringPopup(payload = {}) {
  const sessionId = String(payload.sessionId || "").trim();
  if (!sessionId) {
    throw new Error("sessionId is required to open popup.");
  }

  if (runtimeTelemetry.popupOpenBySession.has(sessionId)) {
    return { opened: false, reused: true };
  }

  const popupUrl = chrome.runtime.getURL("popup.html");
  const windows = await chrome.windows.getAll({ populate: true, windowTypes: ["popup"] });
  const existing = windows.find((win) =>
    Array.isArray(win.tabs) && win.tabs.some((tab) => tab.url === popupUrl)
  );

  if (existing) {
    await chrome.windows.update(existing.id, { focused: true });
    runtimeTelemetry.popupOpenBySession.set(sessionId, Date.now());
    return { opened: false, reused: true };
  }

  await chrome.windows.create({
    url: popupUrl,
    type: "popup",
    focused: true,
    width: 460,
    height: 760
  });

  runtimeTelemetry.popupOpenBySession.set(sessionId, Date.now());
  return { opened: true, reused: false };
}

chrome.runtime.onInstalled.addListener(async () => {
  await restoreState();
  if (!state.sessionActive) {
    return;
  }
  await setupSessionTimers();
});

chrome.runtime.onStartup.addListener(async () => {
  await restoreState();
  if (!state.sessionActive) {
    return;
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
      if (details.error !== "net::ERR_BLOCKED_BY_CLIENT") {
        return;
      }

      const hostname = extractHostname(details.url || "");
      if (!isRestrictedHost(hostname)) {
        return;
      }
      if (shouldThrottleBlockedHost(hostname)) {
        return;
      }

      await recordViolation("RESTRICTED_DOMAIN_BLOCKED", "Restricted domain access blocked.", {
        url: details.url,
        hostname,
        tabId: details.tabId,
        frameId: details.frameId,
        requestId: details.requestId,
        error: details.error
      });
    })().catch(() => {
      // Best-effort telemetry only.
    });
  },
  { urls: ["<all_urls>"] }
);

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
        sendResponse({ ok: true, state: getSessionSnapshot() });
        return;
      }
      case "OPEN_PROCTORING_POPUP": {
        const result = await openProctoringPopup(message.payload || {});
        sendResponse({ ok: true, ...result });
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
