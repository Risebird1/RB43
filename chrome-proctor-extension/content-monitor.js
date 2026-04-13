(function initContentMonitor() {
  let sessionActive = false;
  let handshakeSent = false;

  function postToPage(type, payload = {}) {
    window.postMessage(
      { source: 'risebird-proctor-extension', type, payload },
      window.location.origin
    );
  }

  function send(level, reason, metadata = {}) {
    chrome.runtime.sendMessage({
      type: 'CONTENT_EVENT',
      level,
      reason,
      metadata: {
        url: window.location.href,
        hostname: window.location.hostname,
        title: document.title,
        ...metadata
      }
    });
  }

  function sendHandshake(payload, source = 'page') {
    chrome.runtime.sendMessage({
      type: 'JOIN_DASHBOARD_HANDSHAKE',
      payload: {
        ...payload,
        source,
        pageUrl: window.location.href,
        pageTitle: document.title
      }
    });
  }

  function shouldMonitor() {
    return sessionActive;
  }

  chrome.storage.local.get(['sessionActive'], (data) => {
    sessionActive = Boolean(data.sessionActive);
  });

  chrome.storage.onChanged.addListener((changes, area) => {
    if (area !== 'local') return;
    if (changes.sessionActive) {
      sessionActive = Boolean(changes.sessionActive.newValue);
    }
  });

  window.addEventListener('message', (event) => {
    if (event.source !== window) return;
    if (event.origin !== window.location.origin) return;

    const data = event.data || {};
    if (data?.type === 'RB_PROCTORING_HELLO') {
      postToPage('RB_PROCTORING_EXTENSION_READY', { installed: true, version: '1.2.0' });
      return;
    }

    if (data?.type === 'RB_PROCTORING_JOIN' && data?.payload) {
      sendHandshake(data.payload, 'postMessage');
    }
  });

  (function handshakeFromUrlIfPresent() {
    if (handshakeSent) return;

    const url = new URL(window.location.href);
    const interviewId = url.searchParams.get('interviewId');
    const candidateId = url.searchParams.get('candidateId');
    const proctorSessionToken = url.searchParams.get('proctorSessionToken');

    if (interviewId && proctorSessionToken) {
      handshakeSent = true;
      sendHandshake({ interviewId, candidateId, proctorSessionToken }, 'query-string');
    }
  })();

  document.addEventListener('visibilitychange', () => {
    if (!shouldMonitor()) return;
    if (document.visibilityState !== 'visible') {
      send('violation', 'Page hidden or backgrounded.');
    }
  });

  document.addEventListener('fullscreenchange', () => {
    if (!shouldMonitor()) return;
    if (!document.fullscreenElement) {
      send('violation', 'Candidate exited fullscreen mode.');
    }
  });

  window.addEventListener('blur', () => {
    if (!shouldMonitor()) return;
    send('violation', 'Window lost focus.');
  });

  document.addEventListener('copy', () => {
    if (!shouldMonitor()) return;
    send('violation', 'Copy action detected.');
  });

  document.addEventListener('paste', () => {
    if (!shouldMonitor()) return;
    send('violation', 'Paste action detected.');
  });

  document.addEventListener('contextmenu', (event) => {
    if (!shouldMonitor()) return;
    event.preventDefault();
    send('violation', 'Right-click/context menu blocked.');
  });

  document.addEventListener('keydown', (event) => {
    if (!shouldMonitor()) return;

    const blockedCombos = [
      event.key === 'F12',
      event.ctrlKey && event.shiftKey && ['I', 'J', 'C'].includes(event.key.toUpperCase()),
      event.ctrlKey && ['U', 'S', 'P', 'C', 'V', 'X'].includes(event.key.toUpperCase()),
      event.altKey && event.key === 'Tab'
    ];

    if (blockedCombos.some(Boolean)) {
      event.preventDefault();
      send('violation', 'Blocked keyboard shortcut attempted.', {
        key: event.key,
        ctrl: event.ctrlKey,
        shift: event.shiftKey,
        alt: event.altKey
      });
    }
  });
})();
