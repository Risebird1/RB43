(function initContentMonitor() {

  let sessionActive = false;

  let clipboardBlocking = false;

  let proctoringStartForwarded = false;

  let autoPipelineStarted = false;
  let extensionReadyAnnounced = false;
  let setupRetryTimerId = null;
  const SETUP_RETRY_MS = 5000;
  const SETUP_RETRY_MAX = 30;
  const CAPTURE_RECOVERY_MS = 45000;
  const JOIN_HANDSHAKE_MAX_ATTEMPTS = 10;
  let setupRetryCount = 0;
  let joinCompleted = false;
  let pipelineInFlight = false;
  let screenShareConfirmed = false;



  function safeRuntimeSendMessage(payload) {

    return new Promise((resolve) => {

      try {

        const cloneable = JSON.parse(JSON.stringify(payload));

        chrome.runtime.sendMessage(cloneable, (response) => {

          if (chrome.runtime.lastError) {

            resolve({ ok: false, retry: true, error: chrome.runtime.lastError.message });

            return;

          }

          resolve(response || { ok: false, retry: true });

        });

      } catch (e) {

        resolve({ ok: false, retry: true, error: e?.message || String(e) });

      }

    });

  }



  function send(level, reason, metadata) {

    const base = Object.assign({ pageHostname: window.location.hostname }, metadata || {});

    safeRuntimeSendMessage({ type: 'CONTENT_EVENT', level, reason, metadata: base });

  }



  function postStarted() {

    window.postMessage(

      { source: 'risebird-proctor-extension', type: 'RB_PROCTORING_STARTED' },

      window.location.origin

    );

  }



  /** Console only — not thrown; setup retries silently in the background. */
  function postStartFailed(error, options) {
    if (options?.silent) return;
    const msg = error != null && String(error).trim() ? String(error).trim() : 'unknown';
    if (/cancel/i.test(msg) || options?.pickerCancelled) return;
    if (typeof console !== 'undefined' && console.warn) {
      console.warn('[Risebird Proctoring] setup will retry:', msg);
    }
  }

  function isFatalJoinError(error) {
    const err = String(error || '').toLowerCase();
    return (
      err.includes('missing session token') ||
      err.includes('invalid interview') ||
      err.includes('not allowed') ||
      err.includes('only on the ai interview')
    );
  }

  async function joinHandshakeWithRetry(payload) {
    let lastError = null;
    for (let i = 0; i < JOIN_HANDSHAKE_MAX_ATTEMPTS; i++) {
      const joinRes = await safeRuntimeSendMessage({ type: 'JOIN_DASHBOARD_HANDSHAKE', payload });
      if (joinRes?.ok) return joinRes;
      lastError = joinRes?.error || 'join_failed';
      if (isFatalJoinError(lastError)) break;
      await sleep(Math.min(3500, 300 + i * 280));
    }
    return { ok: false, error: lastError };
  }



  chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {

    (async () => {

      if (message?.type === 'NOTIFY_PROCTORING_STARTED') {

        sessionActive = true;

        proctoringStartForwarded = true;

        await chrome.storage.local.set({ sessionActive: true });

        postStarted();

        sendResponse({ ok: true });

        return;

      }



      if (message?.type === 'NOTIFY_SCREEN_SHARE_LOST') {
        proctoringStartForwarded = false;
        screenShareConfirmed = false;
        window.postMessage(
          { source: 'risebird-proctor-extension', type: 'RB_SCREEN_SHARE_LOST' },
          window.location.origin
        );
        void scheduleShareLostRepickOnce();
        sendResponse({ ok: true });
        return;
      }



      if (message?.type === 'START_CONTENT_MONITORING') {

        const settings = message.settings || {};

        clipboardBlocking = Boolean(settings.clipboardBlocking);

        sessionActive = true;

        proctoringStartForwarded = true;

        await chrome.storage.local.set({ sessionActive: true });

        sendResponse({ ok: true });

        return;

      }



      if (message?.type === 'STOP_CONTENT_MONITORING') {

        sessionActive = false;

        proctoringStartForwarded = false;

        await chrome.storage.local.set({ sessionActive: false });

        sendResponse({ ok: true });

        return;

      }



      sendResponse({ ok: false, error: 'Unknown content action.' });

    })().catch((error) => {

      postStartFailed(error.message || String(error));

      sendResponse({ ok: false, error: error.message || String(error) });

    });



    return true;

  });



  chrome.storage.local.get(['sessionActive'], (data) => {

    sessionActive = Boolean(data.sessionActive);

  });



  chrome.storage.onChanged.addListener((changes, area) => {

    if (area === 'local' && changes.sessionActive) {

      sessionActive = Boolean(changes.sessionActive.newValue);

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

        meetingStartIso: data.meetingStartIso != null ? String(data.meetingStartIso) : '',

        meetingEndIso: data.meetingEndIso != null ? String(data.meetingEndIso) : '',

        resumeCaptureAfterInterrupt: data.resumeCaptureAfterInterrupt === true,

        apiOrigin: origin,

        updatedAt: Date.now(),

        source: 'page_bootstrap_dom',

      },

    });

  }



  function readBootstrapPayload() {

    const el = document.getElementById('risebird-proctoring-bootstrap');

    if (!el || !el.textContent || !el.textContent.trim()) return null;

    try {

      const data = JSON.parse(el.textContent.trim());

      return {

        name: data.name != null ? String(data.name) : '',

        email: data.email != null ? String(data.email) : '',

        sessionToken: data.sessionToken != null ? String(data.sessionToken) : '',

        interviewId: data.interviewId !== undefined && data.interviewId !== null ? data.interviewId : '',

        meetingStartIso: data.meetingStartIso != null ? String(data.meetingStartIso) : '',

        meetingEndIso: data.meetingEndIso != null ? String(data.meetingEndIso) : '',

        resumeCaptureAfterInterrupt: data.resumeCaptureAfterInterrupt === true,

        restrictedDomains: data.restrictedDomains,

        allowedExamHosts: data.allowedExamHosts,

        clipboardBlocking: data.clipboardBlocking,

        apiOrigin: window.location.origin,

      };

    } catch {

      return null;

    }

  }



  async function persistJoinPayload(p) {

    void chrome.storage.local.set({

      proctoringLastJoin: {

        name: p.name != null ? String(p.name) : '',

        email: p.email != null ? String(p.email) : '',

        sessionToken: p.sessionToken != null ? String(p.sessionToken) : '',

        interviewId: p.interviewId !== undefined && p.interviewId !== null ? p.interviewId : '',

        meetingStartIso: p.meetingStartIso != null ? String(p.meetingStartIso) : '',

        meetingEndIso: p.meetingEndIso != null ? String(p.meetingEndIso) : '',

        apiOrigin: p.apiOrigin || window.location.origin,

        updatedAt: Date.now(),

        source: 'dashboard',

      },

    });

  }



  function sleep(ms) {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }

  async function queryLiveCapture() {
    const q = await safeRuntimeSendMessage({ type: 'QUERY_OFFSCREEN_CAPTURE' });
    return Boolean(q?.hasLiveStream);
  }

  async function markStartedIfLive() {
    if (proctoringStartForwarded) return true;
    if (!(await queryLiveCapture())) return false;
    screenShareConfirmed = true;
    proctoringStartForwarded = true;
    postStarted();
    return true;
  }

  let screenPickerAttemptedKey = false;
  let proctoringPrepareDone = false;
  let interruptResumeInFlight = false;
  let interruptResumeDone = false;

  async function runProctoringPreparePhase() {
    const prep = await safeRuntimeSendMessage({ type: 'PREPARE_PROCTORING' });
    if (prep?.ok && prep?.proctoringReady) {
      proctoringPrepareDone = true;
      window.postMessage(
        { source: 'risebird-proctor-extension', type: 'RB_PROCTORING_PREPARE_READY' },
        window.location.origin
      );
    }
    return prep;
  }

  /** Phase 2 — show entire-screen picker once prepare is done. */
  async function requestEntireScreenShareOnce(forceNewPicker) {
    if (forceNewPicker) {
      screenPickerAttemptedKey = false;
      screenShareConfirmed = false;
      proctoringStartForwarded = false;
    }

    if (await markStartedIfLive()) {
      setupRetryCount = 0;
      return { ok: true };
    }

    if (!forceNewPicker && screenPickerAttemptedKey) {
      await sleep(600);
      if (await markStartedIfLive()) {
        setupRetryCount = 0;
        return { ok: true };
      }
      return { ok: false, doNotRetryPicker: true };
    }

    const shareRes = await safeRuntimeSendMessage({
      type: 'BEGIN_SCREEN_SHARE',
      forceNewPicker: forceNewPicker === true,
    });

    if (!forceNewPicker && shareRes) {
      const e = String(shareRes.error || '').toLowerCase();
      if (
        shareRes.ok ||
        shareRes.pickerCancelled ||
        shareRes.doNotRetryPicker ||
        e.includes('screen') ||
        e.includes('cancel') ||
        e.includes('not granted') ||
        e.includes('capture')
      ) {
        screenPickerAttemptedKey = true;
      }
    }

    if (shareRes?.ok) {
      screenShareConfirmed = true;
      proctoringStartForwarded = true;
      postStarted();
      setupRetryCount = 0;
      return { ok: true };
    }

    if (await markStartedIfLive()) {
      setupRetryCount = 0;
      return { ok: true };
    }

    postStartFailed(shareRes?.error, {
      pickerCancelled: Boolean(shareRes?.pickerCancelled),
      silent: Boolean(shareRes?.doNotRetryPicker && !shareRes?.error),
    });
    return shareRes || { ok: false };
  }

  function scheduleSetupRetry() {
    if (setupRetryTimerId != null || proctoringStartForwarded) return;
    if (setupRetryCount >= SETUP_RETRY_MAX) return;
    setupRetryCount += 1;
    setupRetryTimerId = window.setTimeout(() => {
      setupRetryTimerId = null;
      void runSetupRecovery();
    }, SETUP_RETRY_MS);
  }

  let shareLostRepickTimerId = null;
  const SHARE_LOST_REPICK_DELAY_MS = 5000;

  async function runCaptureRecovery(st) {
    if (!st?.sessionActive || !st?.joined) return false;
    if (st.pipelineReady) {
      await markStartedIfLive();
      return true;
    }
    if (st.hasLiveStream) {
      await safeRuntimeSendMessage({ type: 'REQUEST_CAPTURE_RECOVERY' });
      await markStartedIfLive();
      return true;
    }
    return false;
  }

  function bootstrapRequestsInterruptResume() {
    const payload = readBootstrapPayload();
    return Boolean(payload?.resumeCaptureAfterInterrupt);
  }

  /** Only after abrupt interview stop + same-session resume (server bootstrap flag). */
  async function runResumeCaptureAfterInterrupt() {
    if (!bootstrapRequestsInterruptResume()) return false;
    if (interruptResumeDone || interruptResumeInFlight) return interruptResumeDone;
    if (pipelineInFlight) return false;
    interruptResumeInFlight = true;
    pipelineInFlight = true;
    try {
      const payload = readBootstrapPayload();
      if (!payload?.sessionToken) return false;
      await persistJoinPayload(payload);
      if (!joinCompleted) {
        const joinRes = await joinHandshakeWithRetry(payload);
        if (!joinRes?.ok) {
          scheduleSetupRetry();
          return false;
        }
        joinCompleted = true;
        window.postMessage(
          { source: 'risebird-proctor-extension', type: 'RB_PROCTORING_JOIN_OK' },
          window.location.origin
        );
      }
      await safeRuntimeSendMessage({
        type: 'RESUME_CAPTURE_AFTER_INTERRUPT',
        allowResume: true,
        interviewId: payload.interviewId,
      });
      if (!proctoringPrepareDone) {
        const prep = await runProctoringPreparePhase();
        if (!prep?.ok) {
          scheduleSetupRetry();
          return false;
        }
      }
      screenPickerAttemptedKey = false;
      screenShareConfirmed = false;
      proctoringStartForwarded = false;
      const shareRes = await requestEntireScreenShareOnce(true);
      const ok = Boolean(shareRes?.ok || proctoringStartForwarded);
      if (ok) interruptResumeDone = true;
      if (!ok) scheduleSetupRetry();
      return ok;
    } finally {
      interruptResumeInFlight = false;
      pipelineInFlight = false;
    }
  }

  async function scheduleShareLostRepickOnce() {
    if (shareLostRepickTimerId != null) return;
    shareLostRepickTimerId = window.setTimeout(() => {
      shareLostRepickTimerId = null;
      void (async () => {
        const st = await safeRuntimeSendMessage({ type: 'GET_PROCTORING_SETUP_STATE' });
        if (!st?.sessionActive || !st?.joined) return;
        if (st.hasLiveStream || st.pipelineReady) return;
        await safeRuntimeSendMessage({ type: 'RESET_SCREEN_PICKER_FOR_SESSION' });
        screenPickerAttemptedKey = false;
        await requestEntireScreenShareOnce(true);
      })();
    }, SHARE_LOST_REPICK_DELAY_MS);
  }

  async function runSetupRecovery() {
    if (pipelineInFlight) return;
    if (bootstrapRequestsInterruptResume()) {
      const stResume = await safeRuntimeSendMessage({ type: 'GET_PROCTORING_SETUP_STATE' });
      if (stResume?.joined && !stResume?.hasLiveStream && !stResume?.pipelineReady) {
        await runResumeCaptureAfterInterrupt();
        return;
      }
    }
    if (await markStartedIfLive()) return;
    const st = await safeRuntimeSendMessage({ type: 'GET_PROCTORING_SETUP_STATE' });
    if (!st?.joined) {
      if (!joinCompleted) void tryStartAutoPipeline();
      return;
    }
    if (st.pipelineReady || (st.hasLiveStream && (st.screenshotUploads > 0 || st.screenChunkUploads > 0))) {
      await markStartedIfLive();
      return;
    }
    if (st.setupInProgress) return;
    if (st.captureStale) {
      await runCaptureRecovery(st);
      return;
    }
    if (!st.proctoringReady && !proctoringPrepareDone) {
      const prep = await runProctoringPreparePhase();
      if (!prep?.ok) {
        scheduleSetupRetry();
        return;
      }
    }
    if ((st.pickerAttempted || screenPickerAttemptedKey) && !st.hasLiveStream && !st.pipelineReady) {
      if (bootstrapRequestsInterruptResume()) {
        await runResumeCaptureAfterInterrupt();
      }
      return;
    }
    await requestEntireScreenShareOnce(false);
    if (!proctoringStartForwarded) scheduleSetupRetry();
  }



  /** Join once, then entire-screen picker only if capture is not already live. */

  async function runJoinAndScreenShareFromBootstrap() {
    if (pipelineInFlight) return;
    pipelineInFlight = true;
    try {
      announceExtensionReady();

      const payload = readBootstrapPayload();

      if (!payload?.sessionToken) {
        console.warn('[Risebird Proctoring] missing session token on page');
        scheduleSetupRetry();
        return;
      }

      await persistJoinPayload(payload);

      if (!joinCompleted) {
        const joinRes = await joinHandshakeWithRetry(payload);
        if (!joinRes?.ok) {
          console.warn('[Risebird Proctoring] join handshake:', joinRes?.error || 'failed');
          scheduleSetupRetry();
          return;
        }
        joinCompleted = true;
        window.postMessage(
          { source: 'risebird-proctor-extension', type: 'RB_PROCTORING_JOIN_OK' },
          window.location.origin
        );
        if (joinRes.proctoringReady || joinRes.sessionStarted) {
          proctoringPrepareDone = true;
          window.postMessage(
            { source: 'risebird-proctor-extension', type: 'RB_PROCTORING_PREPARE_READY' },
            window.location.origin
          );
        } else if (joinRes.sessionStarted !== true) {
          await safeRuntimeSendMessage({ type: 'ENSURE_SESSION_STARTED' });
        }
      }

      if (await markStartedIfLive()) return;

      if (!proctoringPrepareDone) {
        const prep = await runProctoringPreparePhase();
        if (!prep?.ok) {
          scheduleSetupRetry();
          return;
        }
      }

      if (bootstrapRequestsInterruptResume()) {
        const stAfterJoin = await safeRuntimeSendMessage({ type: 'GET_PROCTORING_SETUP_STATE' });
        if (!stAfterJoin?.hasLiveStream && !stAfterJoin?.pipelineReady) {
          await runResumeCaptureAfterInterrupt();
          return;
        }
      }

      await requestEntireScreenShareOnce(false);
      if (!proctoringStartForwarded) {
        scheduleSetupRetry();
      }
    } finally {
      pipelineInFlight = false;
    }
  }



  function scheduleBootstrapDom() {

    const run = () => persistFromBootstrapDom();

    run();

    if (document.readyState === 'loading') {

      document.addEventListener('DOMContentLoaded', run);

    }

    [80, 400, 1500].forEach((ms) => setTimeout(persistFromBootstrapDom, ms));

  }

  scheduleBootstrapDom();



  function announceExtensionReady() {
    if (extensionReadyAnnounced) return;
    extensionReadyAnnounced = true;
    window.postMessage(
      { source: 'risebird-proctor-extension', type: 'RB_PROCTORING_EXTENSION_READY' },
      window.location.origin
    );
  }

  function waitForBootstrapElement(maxMs = 60000) {
    return new Promise((resolve) => {
      const found = () => document.getElementById('risebird-proctoring-bootstrap');
      if (found()) {
        resolve(true);
        return;
      }
      const deadline = Date.now() + maxMs;
      const observer = new MutationObserver(() => {
        if (found()) {
          observer.disconnect();
          resolve(true);
        }
      });
      observer.observe(document.documentElement, { childList: true, subtree: true });
      const poll = () => {
        if (found()) {
          observer.disconnect();
          resolve(true);
          return;
        }
        if (Date.now() >= deadline) {
          observer.disconnect();
          resolve(false);
          return;
        }
        setTimeout(poll, 200);
      };
      setTimeout(poll, 200);
    });
  }

  async function tryStartAutoPipeline() {
    const el = document.getElementById('risebird-proctoring-bootstrap');
    if (!el || !el.textContent || !el.textContent.trim()) return;
    if (proctoringStartForwarded) return;
    if (autoPipelineStarted) {
      void runSetupRecovery();
      return;
    }
    autoPipelineStarted = true;
    void runJoinAndScreenShareFromBootstrap();
  }

  let proctoringReadyKickoffDone = false;

  async function emitProctoringReady() {
    announceExtensionReady();
    const hasBootstrap = await waitForBootstrapElement(60000);
    if (!hasBootstrap) {
      console.warn('[Risebird Proctoring] bootstrap not found yet');
      return;
    }
    if (proctoringReadyKickoffDone) {
      if (!proctoringStartForwarded) void runSetupRecovery();
      return;
    }
    proctoringReadyKickoffDone = true;
    await tryStartAutoPipeline();
  }

  function scheduleProctoringReadyPing() {
    const run = () => void emitProctoringReady();
    run();
    [0, 50, 150, 400].forEach((ms) => setTimeout(run, ms));
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', run);
    }
  }

  function watchBootstrapForEarlyPipeline() {
    const tryEarly = () => {
      announceExtensionReady();
      void tryStartAutoPipeline();
    };
    if (document.getElementById('risebird-proctoring-bootstrap')) {
      tryEarly();
      return;
    }
    const observer = new MutationObserver(() => {
      if (document.getElementById('risebird-proctoring-bootstrap')) {
        observer.disconnect();
        tryEarly();
      }
    });
    observer.observe(document.documentElement, { childList: true, subtree: true });
    setTimeout(() => observer.disconnect(), 60000);
  }

  scheduleProctoringReadyPing();
  watchBootstrapForEarlyPipeline();

  window.setInterval(() => {
    if (proctoringStartForwarded || pipelineInFlight || setupRetryTimerId != null) return;
    void runSetupRecovery();
  }, SETUP_RETRY_MS);

  window.addEventListener('online', () => {
    void runSetupRecovery();
    void safeRuntimeSendMessage({ type: 'ENSURE_SESSION_STARTED' });
  });

  chrome.runtime.onMessage.addListener((message) => {
      if (message?.type === 'NOTIFY_NETWORK_ONLINE') {
      void runSetupRecovery();
    }
    if (message?.type === 'NOTIFY_CAPTURE_RECOVERY') {
      void (async () => {
        const st = await safeRuntimeSendMessage({ type: 'GET_PROCTORING_SETUP_STATE' });
        await runCaptureRecovery(st);
      })();
    }
  });

  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') {
      void runSetupRecovery();
    }
  });

  window.addEventListener('message', (event) => {

    if (event.source !== window || event.origin !== window.location.origin) return;



    if (event.data?.type === 'RB_PROCTORING_HELLO') {
      void emitProctoringReady();
      return;
    }



    if (event.data?.type === 'RB_PROCTORING_MEETING_END') {

      safeRuntimeSendMessage({ type: 'END_EXAM' });

      return;

    }

    if (event.data?.type === 'RB_PROCTORING_SOFT_INTERRUPT') {
      const payload = readBootstrapPayload();
      const pendingKey =
        payload?.interviewId !== undefined && payload?.interviewId !== null
          ? String(payload.interviewId)
          : '';
      if (pendingKey) {
        void chrome.storage.local.set({ proctoringInterruptResumePendingKey: pendingKey });
      }
      void safeRuntimeSendMessage({ type: 'SOFT_INTERRUPT_PROCTORING' });
      return;
    }

    if (event.data?.type === 'RB_START_PROCTORING') {
      void (async () => {
        await runProctoringPreparePhase();
        await requestEntireScreenShareOnce(event.data.forceNewPicker === true);
      })();
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

