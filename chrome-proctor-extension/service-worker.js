const DEFAULT_SETTINGS = {
  restrictedDomains: ['google.com', 'stackoverflow.com', 'chat.openai.com'],
  autoClearHours: 1,
  notificationsEnabled: true
};

const DEFAULT_STATE = {
  sessionActive: false,
  violations: 0,
  monitoredTabId: null,
  monitoredWindowId: null,
  examUrl: null,
  candidateContext: null,
  events: [],
  settings: DEFAULT_SETTINGS
};

async function getState() {
  const state = await chrome.storage.local.get(DEFAULT_STATE);
  return {
    ...DEFAULT_STATE,
    ...state,
    settings: { ...DEFAULT_SETTINGS, ...(state.settings || {}) },
    events: Array.isArray(state.events) ? state.events : []
  };
}

async function setState(patch) {
  const current = await getState();
  await chrome.storage.local.set({ ...current, ...patch });
}

function nowIso() {
  return new Date().toISOString();
}

/** Fill hostname from url when missing so stored events always expose a domain for UIs/reports. */
function enrichEventMetadata(metadata = {}) {
  const m = { ...metadata };
  if (m.url && typeof m.url === 'string' && (m.hostname === undefined || m.hostname === '')) {
    try {
      m.hostname = new URL(m.url).hostname;
    } catch {
      /* ignore */
    }
  }
  return m;
}

/**
 * Concurrent tab/webRequest listeners used to interleave reads/writes and drop or corrupt `events`.
 * Serialize all appends on a single chain.
 */
let appendEventTail = Promise.resolve();

function appendEvent(type, reason, metadata = {}) {
  const meta = enrichEventMetadata(metadata);
  const p = appendEventTail.then(async () => {
    const state = await getState();
    const event = {
      id: crypto.randomUUID(),
      timestamp: nowIso(),
      type,
      reason,
      metadata: meta
    };

    const events = [...state.events, event].slice(-300);
    const violations = type === 'violation' ? state.violations + 1 : state.violations;

    await setState({ events, violations });

    if (type === 'violation' && state.settings.notificationsEnabled) {
      await chrome.notifications.create({
        type: 'basic',
        iconUrl: 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO7Z7YQAAAAASUVORK5CYII=',
        title: 'Proctoring Alert',
        message: `${reason} (#${violations})`
      });
    }

    return event;
  });
  appendEventTail = p.catch((err) => {
    console.error('[Risebird Proctoring] appendEvent failed', err);
  });
  return p;
}

function formatRestrictedDomainReason(kind, domainInfo) {
  const rule = domainInfo.matchedRestrictedDomain || 'unknown';
  const host = domainInfo.hostname || 'unknown';
  if (kind === 'navigation') {
    return `Restricted domain (tab URL) — rule: ${rule} — host: ${host}`;
  }
  return `Restricted domain (network) — rule: ${rule} — host: ${host}`;
}

/**
 * Parses a URL and detects which restricted-domain rule (if any) matches.
 * Events use `hostname` (actual host) and `matchedRestrictedDomain` (configured rule).
 */
function getUrlDomainInfo(url, restrictedDomains) {
  try {
    const parsed = new URL(url);
    const hostname = parsed.hostname;
    const hostLower = hostname.toLowerCase();
    let matchedRestrictedDomain = null;
    for (const raw of restrictedDomains) {
      const domain = String(raw || '')
        .trim()
        .toLowerCase();
      if (!domain) continue;
      if (hostLower === domain || hostLower.endsWith(`.${domain}`)) {
        matchedRestrictedDomain = domain;
        break;
      }
    }
    return {
      hostname,
      matchedRestrictedDomain,
      isRestricted: Boolean(matchedRestrictedDomain)
    };
  } catch {
    return {
      hostname: null,
      matchedRestrictedDomain: null,
      isRestricted: false
    };
  }
}

function hostnameFromUrl(url) {
  try {
    return new URL(url).hostname;
  } catch {
    return null;
  }
}

async function syncDynamicBlockRules(restrictedDomains) {
  const existing = await chrome.declarativeNetRequest.getDynamicRules();
  const removeRuleIds = existing.map((rule) => rule.id);

  const addRules = restrictedDomains.slice(0, 50).map((domain, index) => ({
    id: 1000 + index,
    priority: 2,
    action: { type: 'block' },
    condition: {
      regexFilter: `^https?:\\/\\/(?:[^\\/]+\\.)?${domain.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}(?:\\/|$)`,
      resourceTypes: ['main_frame', 'sub_frame']
    }
  }));

  await chrome.declarativeNetRequest.updateDynamicRules({
    removeRuleIds,
    addRules
  });
}

chrome.runtime.onInstalled.addListener(async () => {
  const data = await chrome.storage.local.get(null);
  if (!data || Object.keys(data).length === 0) {
    await chrome.storage.local.set(DEFAULT_STATE);
  }
  const state = await getState();
  await syncDynamicBlockRules(state.settings.restrictedDomains || DEFAULT_SETTINGS.restrictedDomains);
});

chrome.tabs.onActivated.addListener(async ({ tabId }) => {
  const state = await getState();
  if (!state.sessionActive) return;

  if (state.monitoredTabId !== tabId) {
    await appendEvent('violation', 'Candidate switched browser tabs.', { tabId });
  }
});

chrome.tabs.onUpdated.addListener(async (tabId, changeInfo) => {
  const state = await getState();
  if (!state.sessionActive || !changeInfo.url) return;

  const domainInfo = getUrlDomainInfo(changeInfo.url, state.settings.restrictedDomains);
  if (domainInfo.isRestricted) {
    await appendEvent('violation', formatRestrictedDomainReason('navigation', domainInfo), {
      tabId,
      url: changeInfo.url,
      hostname: domainInfo.hostname,
      matchedRestrictedDomain: domainInfo.matchedRestrictedDomain,
      source: 'tabs.onUpdated'
    });
  }

  if (tabId === state.monitoredTabId && changeInfo.url) {
    const host = hostnameFromUrl(changeInfo.url);
    await appendEvent('info', 'Monitored tab URL changed.', {
      url: changeInfo.url,
      hostname: host
    });
  }
});

chrome.webNavigation.onCommitted.addListener(async (details) => {
  const state = await getState();
  if (!state.sessionActive || details.frameId !== 0) return;

  await appendEvent('info', 'Navigation committed.', {
    tabId: details.tabId,
    url: details.url,
    hostname: hostnameFromUrl(details.url),
    transitionType: details.transitionType
  });
});

chrome.windows.onFocusChanged.addListener(async (windowId) => {
  const state = await getState();
  if (!state.sessionActive) return;

  if (windowId === chrome.windows.WINDOW_ID_NONE) {
    await appendEvent('violation', 'Candidate moved focus away from Chrome.');
    return;
  }

  if (state.monitoredWindowId !== windowId) {
    await appendEvent('violation', 'Candidate switched browser windows.', { windowId });
  }
});

chrome.webRequest.onBeforeRequest.addListener(
  async (details) => {
    const state = await getState();
    if (!state.sessionActive) return;

    const domainInfo = getUrlDomainInfo(details.url, state.settings.restrictedDomains);
    if (domainInfo.isRestricted) {
      await appendEvent('violation', formatRestrictedDomainReason('network', domainInfo), {
        url: details.url,
        hostname: domainInfo.hostname,
        matchedRestrictedDomain: domainInfo.matchedRestrictedDomain,
        method: details.method,
        resourceType: details.type,
        source: 'webRequest.onBeforeRequest'
      });
    }
  },
  { urls: ['<all_urls>'] }
);

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  (async () => {
    if (message?.type === 'START_SESSION') {
      const [activeTab] = await chrome.tabs.query({ active: true, currentWindow: true });
      if (!activeTab) {
        sendResponse({ ok: false, error: 'No active tab found.' });
        return;
      }

      await setState({
        sessionActive: true,
        violations: 0,
        monitoredTabId: activeTab.id,
        monitoredWindowId: activeTab.windowId,
        examUrl: activeTab.url,
        events: []
      });

      await appendEvent('info', 'Proctoring session started.', { examUrl: activeTab.url });
      sendResponse({ ok: true });
      return;
    }

    if (message?.type === 'STOP_SESSION') {
      await appendEvent('info', 'Proctoring session stopped.');
      await setState({ sessionActive: false, monitoredTabId: null, monitoredWindowId: null });
      sendResponse({ ok: true });
      return;
    }

    if (message?.type === 'CONTENT_EVENT') {
      const level = message.level === 'violation' ? 'violation' : 'info';
      await appendEvent(level, message.reason || 'Content event', message.metadata || {});
      sendResponse({ ok: true });
      return;
    }

    if (message?.type === 'JOIN_DASHBOARD_HANDSHAKE') {
      const payload = message.payload || {};
      if (!payload.interviewId || !payload.proctorSessionToken) {
        sendResponse({ ok: false, error: 'Missing interviewId or proctorSessionToken.' });
        return;
      }

      const activeSenderTabId = sender?.tab?.id ?? null;
      const activeSenderWindowId = sender?.tab?.windowId ?? null;
      const state = await getState();

      await setState({
        sessionActive: true,
        violations: state.sessionActive ? state.violations : 0,
        monitoredTabId: activeSenderTabId,
        monitoredWindowId: activeSenderWindowId,
        examUrl: payload.pageUrl || sender?.tab?.url || state.examUrl,
        candidateContext: {
          interviewId: payload.interviewId,
          candidateId: payload.candidateId || null,
          proctorSessionToken: payload.proctorSessionToken,
          source: payload.source || 'unknown'
        }
      });

      await appendEvent('info', 'Candidate join dashboard handshake received.', {
        interviewId: payload.interviewId,
        candidateId: payload.candidateId || null,
        source: payload.source || 'unknown',
        pageUrl: payload.pageUrl || sender?.tab?.url || null
      });

      sendResponse({ ok: true });
      return;
    }

    if (message?.type === 'CAPTURE_STATUS') {
      await appendEvent('info', message.reason || 'Capture status update.', message.metadata || {});
      sendResponse({ ok: true });
      return;
    }

    if (message?.type === 'GET_STATE') {
      sendResponse({ ok: true, state: await getState() });
      return;
    }

    if (message?.type === 'UPDATE_SETTINGS') {
      const state = await getState();
      const settings = { ...state.settings, ...(message.settings || {}) };
      await setState({ settings });
      await syncDynamicBlockRules(settings.restrictedDomains || []);
      await appendEvent('info', 'Monitoring rules updated.', {
        restrictedDomains: settings.restrictedDomains || []
      });
      sendResponse({ ok: true, settings });
      return;
    }

    if (message?.type === 'CLEAR_EVENTS') {
      await setState({ events: [], violations: 0 });
      sendResponse({ ok: true });
      return;
    }

    if (message?.type === 'GET_REPORT') {
      const state = await getState();
      sendResponse({
        ok: true,
        report: {
          generatedAt: nowIso(),
          sessionActive: state.sessionActive,
          examUrl: state.examUrl,
          candidateContext: state.candidateContext,
          violations: state.violations,
          eventCount: state.events.length,
          events: state.events,
          settings: state.settings
        }
      });
      return;
    }

    if (message?.type === 'CLEAR_BROWSING_DATA') {
      const hours = Number(message.hours ?? 1);
      const since = Date.now() - hours * 60 * 60 * 1000;

      await chrome.browsingData.remove(
        { since },
        {
          cache: true,
          cookies: true,
          downloads: false,
          history: true,
          localStorage: true,
          passwords: false,
          serviceWorkers: true
        }
      );

      await appendEvent('info', 'Browsing data cleared by proctor.', { hours });
      sendResponse({ ok: true });
      return;
    }

    sendResponse({ ok: false, error: 'Unknown action.' });
  })().catch((error) => {
    sendResponse({ ok: false, error: error.message || String(error) });
  });

  return true;
});
