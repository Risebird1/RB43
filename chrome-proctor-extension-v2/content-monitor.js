(function initContentMonitor() {
  let sessionActive = false;
  let clipboardBlocking = false;
  let screenStream = null;
  let surfacePollTimer = null;
  let lastReportedSurface = '';

  /**
   * Service worker may be asleep or reloading; without a callback Chrome logs
   * "Unchecked runtime.lastError: Receiving end does not exist."
   */
  function safeRuntimeSendMessage(payload) {
    try {
      const cloneable = JSON.parse(JSON.stringify(payload));
      chrome.runtime.sendMessage(cloneable, () => {
        void chrome.runtime.lastError;
      });
    } catch (e) {
      console.warn('Risebird proctoring: sendMessage skipped', e);
    }
  }

  function surfaceLabel(surface) {
    if (surface === 'monitor') return 'Entire screen';
    if (surface === 'window') return 'Window';
    if (surface === 'browser') return 'Browser tab';
    return surface || 'unknown';
  }

  function send(level, reason, metadata) {
    const base = Object.assign({ pageHostname: window.location.hostname }, metadata || {});
    safeRuntimeSendMessage({ type: 'CONTENT_EVENT', level, reason, metadata: base });
  }

  function stopSurfacePoll() {
    if (surfacePollTimer) {
      clearInterval(surfacePollTimer);
      surfacePollTimer = null;
    }
    lastReportedSurface = '';
  }

  function startSurfacePoll(videoTrack) {
    stopSurfacePoll();
    lastReportedSurface = videoTrack?.getSettings?.().displaySurface || '';
    surfacePollTimer = setInterval(() => {
      if (!sessionActive || !screenStream) return;
      const vt = screenStream.getVideoTracks()[0];
      if (!vt) return;
      const surface = vt.getSettings?.().displaySurface || 'unknown';
      if (surface === lastReportedSurface) return;
      lastReportedSurface = surface;
      const label = surfaceLabel(surface);
      const level = surface === 'monitor' ? 'info' : 'violation';
      const reason =
        surface === 'monitor'
          ? `Screen capture — ${label} (entire display is shared).`
          : `Screen capture changed — now: ${label} (expected: Entire screen only).`;
      send(level, reason, {
        eventCategory: 'screen_capture',
        event: 'screen_capture_surface',
        displaySurface: surface,
        captureModeLabel: label,
        isEntireScreen: surface === 'monitor',
      });
    }, 3000);
  }

  async function startEnforcedScreenShare() {
    if (screenStream) return { ok: true };

    const stream = await navigator.mediaDevices.getDisplayMedia({
      video: { displaySurface: 'monitor' },
      audio: false,
      preferCurrentTab: false,
    });

    const [videoTrack] = stream.getVideoTracks();
    const surface = videoTrack?.getSettings?.().displaySurface || 'unknown';
    if (surface !== 'monitor') {
      stream.getTracks().forEach((t) => t.stop());
      throw new Error('Please select Entire Screen (not tab/window).');
    }

    videoTrack.onended = () => {
      send('violation', 'Screen share ended — entire screen capture was stopped.', {
        eventCategory: 'screen_capture',
        event: 'screen_share_ended',
        displaySurface: 'ended',
      });
    };

    screenStream = stream;
    const label = surfaceLabel(surface);
    send('info', `Screen capture started — ${label} (shared) | this page: ${window.location.hostname}`, {
      eventCategory: 'screen_capture',
      event: 'screen_share_started',
      displaySurface: surface,
      captureModeLabel: label,
      isEntireScreen: true,
    });
    startSurfacePoll(videoTrack);
    return { ok: true };
  }

  function stopScreenShare() {
    stopSurfacePoll();
    if (!screenStream) return;
    screenStream.getTracks().forEach((t) => t.stop());
    screenStream = null;
  }

  chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
    (async () => {
      if (message?.type === 'START_CONTENT_MONITORING') {
        clipboardBlocking = Boolean(message.settings?.clipboardBlocking);
        await startEnforcedScreenShare();
        sessionActive = true;
        await chrome.storage.local.set({ sessionActive: true });
        sendResponse({ ok: true });
        return;
      }

      if (message?.type === 'STOP_CONTENT_MONITORING') {
        sessionActive = false;
        await chrome.storage.local.set({ sessionActive: false });
        stopScreenShare();
        sendResponse({ ok: true });
        return;
      }

      sendResponse({ ok: false, error: 'Unknown content action.' });
    })().catch((error) => sendResponse({ ok: false, error: error.message || String(error) }));

    return true;
  });

  chrome.storage.local.get(['sessionActive'], (data) => {
    sessionActive = Boolean(data.sessionActive);
  });

  chrome.storage.onChanged.addListener((changes, area) => {
    if (area === 'local' && changes.sessionActive) {
      sessionActive = Boolean(changes.sessionActive.newValue);
      if (!sessionActive) stopScreenShare();
    }
  });

  function persistFromBootstrapDom() {
    const el = document.getElementById('risebird-proctoring-bootstrap');
    if (!el || !el.textContent || !el.textContent.trim()) return;
    let data;
    try {
      data = JSON.parse(el.textContent.trim());
    } catch (e) {
      console.warn('Risebird proctoring: bootstrap JSON parse failed', e);
      return;
    }
    const origin = window.location.origin;
    void chrome.storage.local.set({
      proctoringLastJoin: {
        name: data.name != null ? String(data.name) : '',
        email: data.email != null ? String(data.email) : '',
        sessionToken: data.sessionToken != null ? String(data.sessionToken) : '',
        interviewId: data.interviewId !== undefined && data.interviewId !== null ? data.interviewId : '',
        apiOrigin: origin,
        updatedAt: Date.now(),
        source: 'page_bootstrap_dom',
      },
    });
  }

  function scheduleBootstrapDom() {
    const run = () => persistFromBootstrapDom();
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', run);
    } else {
      run();
    }
    [400, 1500].forEach((ms) => setTimeout(persistFromBootstrapDom, ms));
  }
  scheduleBootstrapDom();

  let proctoringReadyEmitted = false;
  function emitProctoringReadyOnce() {
    if (proctoringReadyEmitted) return;
    if (!document.getElementById('risebird-proctoring-bootstrap')) return;
    proctoringReadyEmitted = true;
    window.postMessage(
      { source: 'risebird-proctor-extension', type: 'RB_PROCTORING_EXTENSION_READY' },
      window.location.origin
    );
  }
  function scheduleProctoringReadyPing() {
    const run = () => emitProctoringReadyOnce();
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', () => {
        setTimeout(run, 50);
        setTimeout(run, 400);
      });
    } else {
      setTimeout(run, 50);
      setTimeout(run, 400);
    }
    setTimeout(run, 900);
  }
  scheduleProctoringReadyPing();

  window.addEventListener('message', (event) => {
    if (event.source !== window || event.origin !== window.location.origin) return;

    if (event.data?.type === 'RB_PROCTORING_HELLO') {
      window.postMessage(
        { source: 'risebird-proctor-extension', type: 'RB_PROCTORING_EXTENSION_READY' },
        window.location.origin
      );
      return;
    }

    if (event.data?.type === 'RB_PROCTORING_MEETING_END') {
      safeRuntimeSendMessage({ type: 'END_EXAM' });
      return;
    }

    if (event.data?.type === 'RB_PROCTORING_JOIN') {
      const payload = Object.assign({}, event.data.payload || {}, {
        apiOrigin: window.location.origin,
      });
      const p = payload;
      void chrome.storage.local.set({
        proctoringLastJoin: {
          name: p.name != null ? String(p.name) : '',
          email: p.email != null ? String(p.email) : '',
          sessionToken: p.sessionToken != null ? String(p.sessionToken) : '',
          interviewId: p.interviewId !== undefined && p.interviewId !== null ? p.interviewId : '',
          apiOrigin: p.apiOrigin || window.location.origin,
          updatedAt: Date.now(),
          source: 'dashboard',
        },
      });
      safeRuntimeSendMessage({ type: 'JOIN_DASHBOARD_HANDSHAKE', payload });
      return;
    }

    if (event.data?.type === 'RB_COMPLETE_TEST_SETUP') {
      safeRuntimeSendMessage({ type: 'COMPLETE_TEST_SETUP' });
    }
  });

  document.addEventListener('visibilitychange', () => {
    if (sessionActive && document.visibilityState !== 'visible') {
      send('violation', `Page hidden or backgrounded | page: ${window.location.hostname}`, {
        eventCategory: 'page_visibility',
      });
    }
  });

  document.addEventListener('fullscreenchange', () => {
    if (sessionActive && !document.fullscreenElement) {
      send('violation', `Fullscreen exited | page: ${window.location.hostname}`, { eventCategory: 'fullscreen' });
    }
  });

  window.addEventListener('blur', () => {
    if (sessionActive) {
      send('violation', `Window blur — focus left this page | page: ${window.location.hostname}`, {
        eventCategory: 'window_blur',
      });
    }
  });

  document.addEventListener('copy', (event) => {
    if (!sessionActive) return;
    if (clipboardBlocking) event.preventDefault();
    send('violation', `Copy | page: ${window.location.hostname} | blocked: ${clipboardBlocking}`, {
      eventCategory: 'clipboard',
      blocked: clipboardBlocking,
    });
  });

  document.addEventListener('cut', (event) => {
    if (!sessionActive) return;
    if (clipboardBlocking) event.preventDefault();
    send('violation', `Cut | page: ${window.location.hostname} | blocked: ${clipboardBlocking}`, {
      eventCategory: 'clipboard',
      blocked: clipboardBlocking,
    });
  });

  document.addEventListener('paste', (event) => {
    if (!sessionActive) return;
    if (clipboardBlocking) event.preventDefault();
    send('violation', `Paste | page: ${window.location.hostname} | blocked: ${clipboardBlocking}`, {
      eventCategory: 'clipboard',
      blocked: clipboardBlocking,
    });
  });

  document.addEventListener('contextmenu', (event) => {
    if (!sessionActive) return;
    event.preventDefault();
    send('violation', `Context menu | page: ${window.location.hostname}`, { eventCategory: 'contextmenu' });
  });
})();
