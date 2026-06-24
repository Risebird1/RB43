(function initContentMonitor() {
  if (!/\/AIInterview(\/|$)/i.test(window.location.pathname || '')) return;

  let sessionActive = false;

  let clipboardBlocking = false;

  let proctoringStartForwarded = false;

  let autoPipelineInFlight = false;
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

  const SCREEN_CHUNK_TIMESLICE_MS = 5000;
  const MIN_PROMPT_INTERVAL_MS =
    typeof MIN_SCREEN_SHARE_PROMPT_INTERVAL_MS === 'number' ? MIN_SCREEN_SHARE_PROMPT_INTERVAL_MS : 8000;

  const proctoringFlow = {
    isSessionJoined: false,
    isSessionStarted: false,
    isPromptingScreenShare: false,
    isScreenShareActive: false,
    isProctoringActive: false,
    hasValidMonitorShare: false,
    lastPromptAt: 0,
    promptRetryCount: 0,
    shareStopped: false,
  };

  let isPromptingScreenShare = false;
  let isScreenShareActive = false;
  let hasValidMonitorShare = false;
  let screenShareStream = null;
  let screenVideoTrack = null;
  let screenMediaRecorder = null;
  let chunkSeq = 0;
  let screenSharePromptedThisSession = false;
  let screenSharePromptSessionKey = '';
  let initProctoringInFlight = false;
  let autoSharePromptDone = false;
  let autoSharePickerInvoked = false;
  let autoStartPipelineScheduled = false;
  let shareRecoveryInFlight = false;
  let lastShareRecoveryAt = 0;

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

  function recordViolation(violationType, message, metadata) {
    safeRuntimeSendMessage({
      type: 'PROCTORING_VIOLATION',
      payload: {
        violationType,
        message,
        metadata: Object.assign({ pageHostname: window.location.hostname }, metadata || {}),
        occurredAt: new Date().toISOString(),
      },
    });
  }

  function getScreenShareSessionKey() {
    const payload = readBootstrapPayload();
    const id = payload?.interviewId;
    return id !== undefined && id !== null && String(id) !== '' ? String(id) : 'default';
  }

  function hasPromptedScreenShareThisSession() {
    return screenSharePromptedThisSession && screenSharePromptSessionKey === getScreenShareSessionKey();
  }

  function markScreenSharePromptedThisSession() {
    screenSharePromptedThisSession = true;
    screenSharePromptSessionKey = getScreenShareSessionKey();
  }

  function resetScreenSharePromptForNewSession() {
    screenSharePromptedThisSession = false;
    screenSharePromptSessionKey = '';
  }

  function syncFlowFlags() {
    proctoringFlow.isPromptingScreenShare = isPromptingScreenShare;
    proctoringFlow.isScreenShareActive = isScreenShareActive;
    proctoringFlow.hasValidMonitorShare = hasValidMonitorShare;
    proctoringFlow.isProctoringActive = Boolean(sessionActive && hasValidMonitorShare && isScreenShareActive);
    void safeRuntimeSendMessage({ type: 'UPDATE_PROCTORING_FLOW_STATE', flow: proctoringFlow });
  }

  /** No custom overlay — only the native Chrome "Share your entire screen" picker is used. */
  const SCREEN_SHARE_OVERLAY_ID = 'risebird-proctor-screen-share-overlay';

  function removeScreenShareOverlay() {
    document.getElementById(SCREEN_SHARE_OVERLAY_ID)?.remove();
    document.querySelectorAll(`#${SCREEN_SHARE_OVERLAY_ID}`).forEach((el) => el.remove());
  }

  function hideScreenShareOverlay() {
    removeScreenShareOverlay();
  }

  function watchScreenShareOverlayRemoval() {
    removeScreenShareOverlay();
    const observer = new MutationObserver(() => removeScreenShareOverlay());
    const root = document.documentElement || document.body;
    if (root) observer.observe(root, { childList: true, subtree: true });
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', removeScreenShareOverlay, { once: true });
    }
  }

  function canRequestScreenShare(forceManual) {
    if (isPromptingScreenShare) return false;
    if (isScreenShareStillLive() || hasValidMonitorShare) return false;
    if (!forceManual) {
      if (autoSharePromptDone || hasPromptedScreenShareThisSession()) return false;
      const elapsed = Date.now() - (proctoringFlow.lastPromptAt || 0);
      if (proctoringFlow.lastPromptAt && elapsed < MIN_PROMPT_INTERVAL_MS) return false;
    }
    return proctoringFlow.isSessionJoined || joinCompleted;
  }

  function validateMonitorScreenShare(videoTrack) {
    const settings = videoTrack.getSettings ? videoTrack.getSettings() : {};
    const surface = settings.displaySurface;
    if (surface === 'browser' || surface === 'window' || surface === 'tab') {
      return { ok: false, displaySurface: surface };
    }
    if (surface === 'monitor') return { ok: true, displaySurface: 'monitor' };
    if (videoTrack.readyState === 'live') {
      return { ok: true, displaySurface: surface || 'monitor_assumed' };
    }
    return { ok: false, displaySurface: surface || 'unknown' };
  }

  async function isOffscreenCaptureLive() {
    const st = await safeRuntimeSendMessage({ type: 'GET_PROCTORING_SETUP_STATE' });
    return Boolean(st?.pipelineReady || st?.hasLiveStream || st?.contentScreenShareActive);
  }

  function isScreenShareStillLive() {
    return Boolean(
      (isScreenShareActive && screenVideoTrack && screenVideoTrack.readyState === 'live') ||
        (proctoringStartForwarded && hasValidMonitorShare)
    );
  }

  async function persistShareFlags(established) {
    const key = getScreenShareSessionKey();
    await chrome.storage.local.set({
      proctoringShareEstablishedKey: established ? key : '',
      proctoringAutoSharePromptKey: key,
    });
    if (established) {
      autoSharePromptDone = true;
      hasValidMonitorShare = true;
      isScreenShareActive = true;
    }
    markScreenSharePromptedThisSession();
  }

  async function loadPersistedShareFlags() {
    const key = getScreenShareSessionKey();
    if (!key || key === 'default') return;
    const data = await chrome.storage.local.get([
      'proctoringShareEstablishedKey',
      'proctoringAutoSharePromptKey',
      'proctoringSessionJoined',
      'sessionActive',
    ]);
    if (data.proctoringShareEstablishedKey === key) {
      autoSharePromptDone = true;
      autoSharePickerInvoked = true;
      hasValidMonitorShare = true;
      isScreenShareActive = true;
      screenSharePromptedThisSession = true;
      screenSharePromptSessionKey = key;
      if (data.sessionActive === true) {
        sessionActive = true;
        proctoringStartForwarded = true;
        proctoringFlow.isSessionStarted = true;
        proctoringFlow.isProctoringActive = true;
      }
    } else if (data.proctoringAutoSharePromptKey === key) {
      autoSharePromptDone = true;
      autoSharePickerInvoked = true;
      screenSharePromptedThisSession = true;
      screenSharePromptSessionKey = key;
    }
    if (data.proctoringSessionJoined === true) {
      joinCompleted = true;
      proctoringFlow.isSessionJoined = true;
    }
  }

  async function resumeProctoringAfterPageReload() {
    await loadPersistedShareFlags();
    const key = getScreenShareSessionKey();
    const data = await chrome.storage.local.get([
      'proctoringShareEstablishedKey',
      'proctoringAutoSharePromptKey',
      'sessionActive',
    ]);
    const wasEstablished =
      data.proctoringShareEstablishedKey === key ||
      data.proctoringAutoSharePromptKey === key ||
      proctoringStartForwarded ||
      hasAutoSharePickerBeenInvoked();
    if (!wasEstablished) return false;

    const st = await safeRuntimeSendMessage({ type: 'GET_PROCTORING_SETUP_STATE' });
    if (
      st?.hasLiveStream &&
      st?.sessionActive &&
      st?.backendStartConfirmed &&
      st?.backendJoinConfirmed
    ) {
      joinCompleted = true;
      await finalizeProctoringStarted({
        ok: true,
        backendReady: true,
        sessionStarted: true,
        resumed: true,
      });
      return true;
    }

    const payload = readBootstrapPayload();
    if (!payload?.sessionToken) return false;
    const resumeRes = await safeRuntimeSendMessage({
      type: 'RESUME_PROCTORING_AFTER_RELOAD',
      payload,
    });
    if (resumeRes?.ok) {
      joinCompleted = true;
      await finalizeProctoringStarted(resumeRes);
      return true;
    }
    return false;
  }

  /** Skip auto picker only when entire-screen capture is actually live for this interview. */
  async function shouldSkipAutoSharePicker() {
    const key = getScreenShareSessionKey();
    const captureLive = await isOffscreenCaptureLive();
    if (!captureLive) return false;
    if (proctoringStartForwarded && hasValidMonitorShare) return true;
    const data = await chrome.storage.local.get(['proctoringShareEstablishedKey']);
    return data.proctoringShareEstablishedKey === key;
  }

  function hasAutoSharePickerBeenInvoked() {
    return Boolean(
      autoSharePickerInvoked || autoSharePromptDone || hasPromptedScreenShareThisSession()
    );
  }

  async function markAutoSharePickerInvoked() {
    autoSharePickerInvoked = true;
    autoSharePromptDone = true;
    markScreenSharePromptedThisSession();
    await persistShareFlags(false);
  }

  async function clearPersistedShareFlags() {
    await chrome.storage.local.remove(['proctoringShareEstablishedKey', 'proctoringAutoSharePromptKey']);
    resetScreenSharePromptForNewSession();
    autoSharePromptDone = false;
    autoSharePickerInvoked = false;
  }

  function stopScreenShareTracks() {
    if (screenVideoTrack) {
      try {
        screenVideoTrack.onended = null;
      } catch (_) {
        /* ignore */
      }
    }
    if (screenShareStream) {
      screenShareStream.getTracks().forEach((t) => {
        try {
          t.stop();
        } catch (_) {
          /* ignore */
        }
      });
    }
    screenShareStream = null;
    screenVideoTrack = null;
  }

  function stopScreenRecorder() {
    if (!screenMediaRecorder) return;
    try {
      if (screenMediaRecorder.state !== 'inactive') screenMediaRecorder.stop();
    } catch (_) {
      /* ignore */
    }
    screenMediaRecorder = null;
  }

  function cleanupScreenShareState(options = {}) {
    stopScreenRecorder();
    stopScreenShareTracks();
    isScreenShareActive = false;
    hasValidMonitorShare = false;
    isPromptingScreenShare = false;
    screenShareConfirmed = false;
    chunkSeq = 0;
    if (!options.keepOverlay) hideScreenShareOverlay();
    if (options.resetPromptFlags) resetScreenSharePromptForNewSession();
    proctoringFlow.shareStopped = options.shareStopped === true;
    proctoringFlow.isProctoringActive = false;
    syncFlowFlags();
  }

  function blobToDataUrl(blob) {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onloadend = () => resolve(reader.result);
      reader.onerror = () => reject(reader.error || new Error('blob_read_failed'));
      reader.readAsDataURL(blob);
    });
  }

  async function uploadScreenChunk(blob, mimeType) {
    if (!blob || blob.size < 1024) return;
    chunkSeq += 1;
    const seq = chunkSeq;
    const capturedAt = Date.now();
    let data;
    try {
      data = await blobToDataUrl(blob);
    } catch (e) {
      recordViolation('screen_chunk_upload_failed', 'Screen chunk upload failed', {
        eventCategory: 'screen_chunk',
        error: e?.message || String(e),
        chunkSeq: seq,
        capturedAt,
      });
      return;
    }
    if (!data || !String(data).startsWith('data:')) return;
    const res = await safeRuntimeSendMessage({
      type: 'UPLOAD_SCREEN_CHUNK',
      payload: {
        chunkSeq: seq,
        mimeType: mimeType || blob.type || 'video/webm',
        capturedAt,
        data,
        captureSource: 'content_display_media',
      },
    });
    if (res && res.ok === false) {
      recordViolation('screen_chunk_upload_failed', 'Screen chunk upload failed', {
        eventCategory: 'screen_chunk',
        error: res.error || 'upload_failed',
        chunkSeq: seq,
        capturedAt,
      });
    }
  }

  function startScreenRecorder(stream) {
    if (screenMediaRecorder && screenMediaRecorder.state !== 'inactive') return;
    const preferred = ['video/webm;codecs=vp9', 'video/webm;codecs=vp8', 'video/webm'];
    let mimeType = '';
    for (const c of preferred) {
      if (MediaRecorder.isTypeSupported(c)) {
        mimeType = c;
        break;
      }
    }
    const options = mimeType ? { mimeType } : {};
    screenMediaRecorder = new MediaRecorder(stream, options);
    screenMediaRecorder.ondataavailable = (event) => {
      if (event.data && event.data.size > 0) {
        void uploadScreenChunk(event.data, screenMediaRecorder.mimeType || event.data.type);
      }
    };
    screenMediaRecorder.onerror = () => {
      recordViolation('screen_chunk_upload_failed', 'Screen recorder error', {
        eventCategory: 'screen_chunk',
      });
    };
    screenMediaRecorder.start(SCREEN_CHUNK_TIMESLICE_MS);
  }

  function onScreenTrackEnded() {
    if (!isScreenShareActive && !screenShareConfirmed) return;
    if (screenVideoTrack && screenVideoTrack.readyState === 'live') return;
    cleanupScreenShareState({ shareStopped: true });
    proctoringStartForwarded = false;
    recordViolation('screen_share_stopped', 'Screen sharing stopped', { eventCategory: 'screen_share' });
    void safeRuntimeSendMessage({ type: 'CONTENT_SCREEN_SHARE_STOPPED', flow: proctoringFlow });
    window.postMessage(
      { source: 'risebird-proctor-extension', type: 'RB_SCREEN_SHARE_LOST' },
      window.location.origin
    );
  }

  async function promptMonitorScreenShare(forceManual) {
    if (!canRequestScreenShare(forceManual)) {
      return { ok: false, skipped: true, doNotRetryPicker: true };
    }

    if (forceManual) {
      cleanupScreenShareState({ resetPromptFlags: true });
    }

    if (!navigator.mediaDevices?.getDisplayMedia) {
      return { ok: false, error: 'Screen capture is not available in this browser.', doNotRetryPicker: true };
    }

    isPromptingScreenShare = true;
    proctoringFlow.lastPromptAt = Date.now();
    proctoringFlow.isPromptingScreenShare = true;
    syncFlowFlags();

    try {
      const stream = await navigator.mediaDevices.getDisplayMedia({
        video: { displaySurface: 'monitor' },
        audio: false,
      });
      const videoTrack = stream.getVideoTracks()[0];
      if (!videoTrack) {
        stream.getTracks().forEach((t) => t.stop());
        return { ok: false, error: 'No video track from screen share.', doNotRetryPicker: true };
      }

      const validation = validateMonitorScreenShare(videoTrack);
      if (!validation.ok) {
        stream.getTracks().forEach((t) => t.stop());
        recordViolation('invalid_screen_share', 'Invalid screen share type — entire monitor required', {
          eventCategory: 'screen_share',
          displaySurface: validation.displaySurface || 'unknown',
        });
        return { ok: false, invalidShare: true, doNotRetryPicker: true };
      }

      if (!forceManual) {
        autoSharePromptDone = true;
        markScreenSharePromptedThisSession();
      }

      screenShareStream = stream;
      screenVideoTrack = videoTrack;
      isScreenShareActive = true;
      hasValidMonitorShare = true;
      screenShareConfirmed = true;
      proctoringFlow.shareStopped = false;
      chunkSeq = 0;
      videoTrack.onended = onScreenTrackEnded;

      startScreenRecorder(stream);

      let swRes = await safeRuntimeSendMessage({ type: 'CONTENT_SCREEN_SHARE_STARTED' });
      if (!swRes?.ok) {
        await sleep(500);
        swRes = await safeRuntimeSendMessage({ type: 'CONTENT_SCREEN_SHARE_STARTED' });
      }
      if (!swRes?.ok) {
        console.warn('[Risebird Proctoring] session arm failed; screen share kept:', swRes?.error);
      }

      hideScreenShareOverlay();
      sessionActive = true;
      proctoringStartForwarded = true;
      proctoringFlow.isProctoringActive = true;
      setupRetryCount = 0;
      syncFlowFlags();
      postStarted();
      return { ok: true };
    } catch (err) {
      const cancelled = err?.name === 'NotAllowedError' || /cancel/i.test(String(err?.message || ''));
      if (!forceManual) {
        autoSharePromptDone = true;
        markScreenSharePromptedThisSession();
      }
      return {
        ok: false,
        error: err?.message || String(err),
        pickerCancelled: cancelled,
        doNotRetryPicker: true,
      };
    } finally {
      isPromptingScreenShare = false;
      proctoringFlow.isPromptingScreenShare = false;
      syncFlowFlags();
    }
  }



  function postStarted() {

    window.postMessage(

      { source: 'risebird-proctor-extension', type: 'RB_PROCTORING_STARTED' },

      window.location.origin

    );

    void postProctoringStats();

  }

  let proctoringStatsTimer = null;

  async function postProctoringStats() {
    try {
      const res = await safeRuntimeSendMessage({ type: 'GET_STATE' });
      const st = res?.state || {};
      window.postMessage(
        {
          source: 'risebird-proctor-extension',
          type: 'RB_PROCTORING_STATS',
          flowPhase: st.flowPhase || '',
          sessionActive: st.sessionActive === true,
          screenshotUploads: st.screenshotUploads || 0,
          screenChunkUploads: st.screenChunkUploads || 0,
          lastScreenshotAt: st.lastScreenshotAt || null,
          lastScreenChunkAt: st.lastScreenChunkAt || null,
        },
        window.location.origin
      );
    } catch (_) {
      /* ignore */
    }
  }

  function startProctoringStatsPolling() {
    if (proctoringStatsTimer) return;
    void postProctoringStats();
    proctoringStatsTimer = window.setInterval(() => void postProctoringStats(), 3000);
  }

  function stopProctoringStatsPolling() {
    if (!proctoringStatsTimer) return;
    window.clearInterval(proctoringStatsTimer);
    proctoringStatsTimer = null;
  }



  /** Console only — not thrown; setup retries silently in the background. */
  function postStartFailed(error, options) {
    if (options?.silent) return;
    const msg = error != null && String(error).trim() ? String(error).trim() : 'unknown';
    if (/cancel/i.test(msg) || options?.pickerCancelled) return;
    if (typeof console !== 'undefined' && console.warn) {
      if (options?.silent) {
        console.warn('[Risebird Proctoring]', msg);
      } else {
        console.warn('[Risebird Proctoring] setup will retry:', msg);
      }
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

  function isFatalSetupError(error) {
    return (
      isFatalJoinError(error) ||
      String(error || '')
        .toLowerCase()
        .includes('not allowed for proctoring')
    );
  }

  async function initProctoringSessionWithRetry(payload) {
    let lastError = null;
    const initPayload = Object.assign({}, payload, { deferSessionStart: true });
    for (let i = 0; i < JOIN_HANDSHAKE_MAX_ATTEMPTS; i++) {
      const initRes = await safeRuntimeSendMessage({ type: 'INIT_PROCTORING_SESSION', payload: initPayload });
      if (initRes?.ok) {
        proctoringFlow.isSessionJoined = true;
        proctoringFlow.isSessionStarted = Boolean(initRes.sessionStarted || initRes.proctoringReady);
        syncFlowFlags();
        return initRes;
      }
      if (initRes?.inProgress) return initRes;
      lastError = initRes?.error || 'init_failed';
      if (isFatalJoinError(lastError)) break;
      await sleep(Math.min(3500, 300 + i * 280));
    }
    return { ok: false, error: lastError };
  }



  chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {

    (async () => {

      if (message?.type === 'REQUEST_MANUAL_SCREEN_SHARE') {
        if (await isOffscreenCaptureLive()) {
          const resumeRes = await safeRuntimeSendMessage({
            type: 'BEGIN_SCREEN_SHARE',
            forceNewPicker: false,
            promptFirst: false,
            payload: readBootstrapPayload(),
          });
          if (resumeRes?.ok) await finalizeProctoringStarted(resumeRes);
          sendResponse({ ok: Boolean(resumeRes?.ok), resumed: Boolean(resumeRes?.ok) });
          return;
        }
        await requestEntireScreenShareOnce(false);
        sendResponse({ ok: true });
        return;
      }

      if (message?.type === 'NOTIFY_SHARE_RECOVERY_NEEDED') {
        void tryShareRecoveryOnce();
        sendResponse({ ok: true });
        return;
      }

      if (message?.type === 'NOTIFY_PROCTORING_STARTED') {
        await finalizeProctoringStarted({ ok: true, sessionStarted: true, backendReady: true });
        await chrome.storage.local.set({ sessionActive: true });
        sendResponse({ ok: true });
        return;
      }



      if (message?.type === 'NOTIFY_SCREEN_SHARE_LOST') {
        if (await isOffscreenCaptureLive()) {
          hideScreenShareOverlay();
          sendResponse({ ok: true, ignored: true });
          return;
        }
        cleanupScreenShareState({ shareStopped: true });
        proctoringStartForwarded = false;
        await clearPersistedShareFlags();
        await safeRuntimeSendMessage({ type: 'CLEAR_STALE_SCREEN_PICKER_STATE', force: true });
        sendResponse({ ok: true });
        return;
      }

      if (message?.type === 'CONTENT_SCREEN_SHARE_CLEANUP') {
        cleanupScreenShareState({ resetPromptFlags: true });
        await clearPersistedShareFlags();
        sessionActive = false;
        proctoringStartForwarded = false;
        await chrome.storage.local.set({ sessionActive: false });
        sendResponse({ ok: true });
        return;
      }



      if (message?.type === 'START_CONTENT_MONITORING') {

        const settings = message.settings || {};

        clipboardBlocking = Boolean(settings.clipboardBlocking);

        sessionActive = true;

        proctoringStartForwarded = true;

        hideScreenShareOverlay();

        await chrome.storage.local.set({ sessionActive: true });

        sendResponse({ ok: true });

        return;

      }



      if (message?.type === 'STOP_CONTENT_MONITORING') {

        cleanupScreenShareState({ resetPromptFlags: true });
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

  async function finalizeProctoringStarted(shareRes) {
    hideScreenShareOverlay();

    let st = await safeRuntimeSendMessage({ type: 'GET_PROCTORING_SETUP_STATE' });
    const backendReadyFromShare =
      shareRes?.ok === true && shareRes?.backendReady === true && shareRes?.sessionStarted === true;
    if (!st?.backendJoinConfirmed || !st?.sessionActive || !st?.backendStartConfirmed) {
      if (!backendReadyFromShare) {
        const ensured = await safeRuntimeSendMessage({ type: 'ENSURE_SESSION_STARTED' });
        if (!ensured?.ok) {
          postStartFailed(ensured?.error || 'Join/start did not complete on server.');
          return { ok: false, error: ensured?.error || 'Join/start did not complete.' };
        }
      }
      st = await safeRuntimeSendMessage({ type: 'GET_PROCTORING_SETUP_STATE' });
      if (!st?.joined || !st?.sessionActive || !st?.backendStartConfirmed) {
        postStartFailed('Backend join/start did not complete.');
        return { ok: false, error: 'Backend join/start did not complete.' };
      }
    }

    sessionActive = true;
    proctoringStartForwarded = true;
    screenShareConfirmed = true;
    isScreenShareActive = true;
    hasValidMonitorShare = true;
    joinCompleted = true;
    proctoringFlow.isSessionJoined = true;
    proctoringFlow.isSessionStarted = true;
    proctoringFlow.isProctoringActive = true;
    proctoringFlow.shareStopped = false;
    autoSharePromptDone = true;
    autoSharePickerInvoked = true;
    await persistShareFlags(true);
    proctoringPrepareDone = true;
    syncFlowFlags();
    postStarted();
    startProctoringStatsPolling();
    void safeRuntimeSendMessage({ type: 'REQUEST_CAPTURE_RECOVERY' });
    window.postMessage(
      { source: 'risebird-proctor-extension', type: 'RB_PROCTORING_PREPARE_READY' },
      window.location.origin
    );
    return { ok: true };
  }

  async function markStartedIfLive() {
    const st = await safeRuntimeSendMessage({ type: 'GET_PROCTORING_SETUP_STATE' });
    if (!st?.joined || !st?.backendJoinConfirmed) return false;
    joinCompleted = true;
    proctoringFlow.isSessionJoined = true;
    proctoringFlow.isSessionStarted = Boolean(st.sessionActive && st.backendStartConfirmed);
    if (st.pipelineReady && st.backendStartConfirmed) return true;
    if (st.contentScreenShareActive && st.hasLiveStream && st.sessionActive && st.backendStartConfirmed) {
      return true;
    }
    return false;
  }

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

  /**
   * Offscreen capture (BEGIN_SCREEN_SHARE) persists across form.submit() page reloads.
   * Content getDisplayMedia does not — use service worker picker once per interview.
   */
  async function requestEntireScreenShareOnce(forceManual) {
    if (!forceManual && (await resumeProctoringAfterPageReload())) {
      return { ok: true, resumed: true };
    }
    if (await markStartedIfLive()) {
      setupRetryCount = 0;
      const resumeRes = await safeRuntimeSendMessage({
        type: 'BEGIN_SCREEN_SHARE',
        forceNewPicker: false,
        promptFirst: false,
        payload: readBootstrapPayload(),
      });
      if (resumeRes?.ok) return await finalizeProctoringStarted(resumeRes);
    }

    if (!forceManual && (await shouldSkipAutoSharePicker())) {
      const resumeRes = await safeRuntimeSendMessage({
        type: 'BEGIN_SCREEN_SHARE',
        forceNewPicker: false,
        promptFirst: false,
        payload: readBootstrapPayload(),
      });
      if (resumeRes?.ok) return await finalizeProctoringStarted(resumeRes);
      return { ok: false, doNotRetryPicker: true, alreadyPrompted: true };
    }

    if (!forceManual && hasAutoSharePickerBeenInvoked()) {
      await resumeBackendWithoutPicker();
      return { ok: false, doNotRetryPicker: true, alreadyPrompted: true };
    }

    if (isPromptingScreenShare) {
      return { ok: false, inProgress: true, doNotRetryPicker: true };
    }

    if (forceManual) {
      if (await isOffscreenCaptureLive()) {
        const resumeRes = await safeRuntimeSendMessage({
          type: 'BEGIN_SCREEN_SHARE',
          forceNewPicker: false,
          promptFirst: false,
          payload: readBootstrapPayload(),
        });
        if (resumeRes?.ok) return await finalizeProctoringStarted(resumeRes);
        return resumeRes || { ok: false };
      }
      await clearPersistedShareFlags();
      await safeRuntimeSendMessage({ type: 'CLEAR_STALE_SCREEN_PICKER_STATE', force: true });
      screenShareConfirmed = false;
      proctoringStartForwarded = false;
      isScreenShareActive = false;
      hasValidMonitorShare = false;
    }

    isPromptingScreenShare = true;
    proctoringFlow.isPromptingScreenShare = true;
    proctoringFlow.lastPromptAt = Date.now();
    syncFlowFlags();
    try {
      const bootstrapPayload = readBootstrapPayload();
      let shareRes = null;
      for (let attempt = 0; attempt < 4; attempt++) {
        shareRes = await safeRuntimeSendMessage({
          type: 'BEGIN_SCREEN_SHARE',
          forceNewPicker: forceManual === true,
          promptFirst: !forceManual,
          payload: bootstrapPayload,
        });
        if (
          shareRes?.pickerInvoked ||
          shareRes?.pickerCancelled ||
          shareRes?.pickerAlreadyAttempted ||
          shareRes?.ok
        ) {
          break;
        }
        if (!shareRes?.retry || attempt >= 3) break;
        await sleep(250 + attempt * 350);
      }
      if (shareRes?.ok) {
        setupRetryCount = 0;
        joinCompleted = true;
        proctoringFlow.isSessionJoined = true;
        proctoringFlow.isSessionStarted = true;
        window.postMessage(
          { source: 'risebird-proctor-extension', type: 'RB_PROCTORING_JOIN_OK' },
          window.location.origin
        );
        return await finalizeProctoringStarted(shareRes);
      }
      if (shareRes?.retry && shareRes?.doNotRetryPicker) {
        await resumeBackendWithoutPicker();
      }
      if (
        !forceManual &&
        !shareRes?.pickerCancelled &&
        !shareRes?.invalidShare &&
        (shareRes?.pickerInvoked || shareRes?.pickerAlreadyAttempted)
      ) {
        await markAutoSharePickerInvoked();
      }
      if (shareRes?.invalidShare && !forceManual) {
        await clearPersistedShareFlags();
        await safeRuntimeSendMessage({ type: 'RESET_SCREEN_PICKER_FOR_SESSION' });
        window.setTimeout(() => void requestEntireScreenShareOnce(false), 800);
      }
      if (isFatalSetupError(shareRes?.error)) {
        shareRes = Object.assign({}, shareRes, { doNotRetryPicker: true });
      }
      postStartFailed(shareRes?.error, {
        pickerCancelled: Boolean(shareRes?.pickerCancelled),
        silent: Boolean(shareRes?.doNotRetryPicker),
      });
      return shareRes || { ok: false };
    } finally {
      isPromptingScreenShare = false;
      proctoringFlow.isPromptingScreenShare = false;
      syncFlowFlags();
    }
  }

  async function resumeBackendWithoutPicker() {
    const payload = readBootstrapPayload();
    if (!payload?.sessionToken) return;
    if (!(await isOffscreenCaptureLive())) {
      if (!joinCompleted) {
        const initRes = await initProctoringSessionWithRetry(payload);
        if (initRes?.ok) joinCompleted = true;
      }
      return;
    }
    const resumeRes = await safeRuntimeSendMessage({
      type: 'BEGIN_SCREEN_SHARE',
      forceNewPicker: false,
      promptFirst: false,
      payload,
    });
    if (resumeRes?.ok) await finalizeProctoringStarted(resumeRes);
  }

  function scheduleSetupRetry() {
    if (setupRetryTimerId != null) return;
    if (setupRetryCount >= SETUP_RETRY_MAX) return;
    setupRetryCount += 1;
    setupRetryTimerId = window.setTimeout(() => {
      setupRetryTimerId = null;
      void (async () => {
        if (proctoringStartForwarded && (await isOffscreenCaptureLive())) return;
        if (hasAutoSharePickerBeenInvoked() || (await shouldSkipAutoSharePicker())) {
          await resumeBackendWithoutPicker();
          return;
        }
        if (!joinCompleted) void tryStartAutoPipeline();
        else void runSetupRecovery();
      })();
    }, SETUP_RETRY_MS);
  }

  async function runCaptureRecovery(st) {
    if (!st?.sessionActive || !st?.joined) return false;
    if (st.contentScreenShareActive || isScreenShareStillLive()) {
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
        const joinRes = await initProctoringSessionWithRetry(payload);
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
      resetScreenSharePromptForNewSession();
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

  async function runSetupRecovery() {
    if (pipelineInFlight || initProctoringInFlight) return;
    if (isPromptingScreenShare) return;
    if (await markStartedIfLive()) {
      const resumeRes = await safeRuntimeSendMessage({ type: 'BEGIN_SCREEN_SHARE', forceNewPicker: false });
      if (resumeRes?.ok) await finalizeProctoringStarted(resumeRes);
      return;
    }
    if (!joinCompleted) {
      void tryStartAutoPipeline();
    }
  }



  /** Native share picker first on landing; join/start/capture run after the candidate shares. */

  async function runJoinAndScreenShareFromBootstrap() {
    if (autoPipelineInFlight) return;
    autoPipelineInFlight = true;
    try {
      announceExtensionReady();
      await loadPersistedShareFlags();
      if (await resumeProctoringAfterPageReload()) {
        return;
      }
      const payload = readBootstrapPayload();
      if (!payload?.sessionToken) {
        console.warn('[Risebird Proctoring] missing session token on page');
        scheduleSetupRetry();
        return;
      }
      await persistJoinPayload(payload);

      const armRes = await safeRuntimeSendMessage({ type: 'PROCTORING_ARM_JOIN', payload });
      if (!armRes?.ok) {
        console.warn('[Risebird Proctoring] POST join before share failed:', armRes?.error);
        scheduleSetupRetry();
        return;
      }

      removeScreenShareOverlay();

      if (await markStartedIfLive()) {
        const resumeRes = await safeRuntimeSendMessage({
          type: 'BEGIN_SCREEN_SHARE',
          forceNewPicker: false,
        });
        if (resumeRes?.ok) {
          joinCompleted = true;
          await finalizeProctoringStarted(resumeRes);
          return;
        }
      }

      const shareRes = await requestEntireScreenShareOnce(false);
      if (
        !shareRes?.ok &&
        !shareRes?.inProgress &&
        !shareRes?.pickerCancelled &&
        !shareRes?.pickerAlreadyAttempted &&
        !shareRes?.doNotRetryPicker
      ) {
        scheduleSetupRetry();
      } else if (shareRes?.ok) {
        setupRetryCount = 0;
      } else if (shareRes?.retry && shareRes?.doNotRetryPicker) {
        await resumeBackendWithoutPicker();
        scheduleSetupRetry();
      }
    } finally {
      autoPipelineInFlight = false;
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

  async function tryShareRecoveryOnce() {
    if (shareRecoveryInFlight || isPromptingScreenShare || autoPipelineInFlight) return;
    const elapsed = Date.now() - lastShareRecoveryAt;
    if (lastShareRecoveryAt && elapsed < MIN_PROMPT_INTERVAL_MS) return;
    shareRecoveryInFlight = true;
    lastShareRecoveryAt = Date.now();
    try {
      if (await resumeProctoringAfterPageReload()) return;
      const resumeRes = await safeRuntimeSendMessage({
        type: 'BEGIN_SCREEN_SHARE',
        forceNewPicker: false,
        promptFirst: false,
        payload: readBootstrapPayload(),
      });
      if (resumeRes?.ok) await finalizeProctoringStarted(resumeRes);
      else if (resumeRes?.retry) scheduleSetupRetry();
    } finally {
      shareRecoveryInFlight = false;
    }
  }

  async function tryStartAutoPipeline() {
    const el = document.getElementById('risebird-proctoring-bootstrap');
    if (!el || !el.textContent || !el.textContent.trim()) return;
    if (autoPipelineInFlight || autoStartPipelineScheduled) return;
    autoStartPipelineScheduled = true;
    if (await resumeProctoringAfterPageReload()) return;
    if (await shouldSkipAutoSharePicker()) {
      // Stream is live and session established — just re-arm without a new picker.
      await resumeBackendWithoutPicker();
      return;
    }
    // Picker may have run before but stream is dead — fall through to re-establish.
    void runJoinAndScreenShareFromBootstrap();
  }

  async function emitProctoringReady() {
    announceExtensionReady();
    if (await resumeProctoringAfterPageReload()) return;
    // Only skip when the stream is verifiably live — never block on a stale picker flag alone.
    if (await shouldSkipAutoSharePicker()) return;
    const hasBootstrap = document.getElementById('risebird-proctoring-bootstrap');
    if (!hasBootstrap) {
      const found = await waitForBootstrapElement(60000);
      if (!found) {
        console.warn('[Risebird Proctoring] bootstrap not found yet');
        return;
      }
    }
    await tryStartAutoPipeline();
  }

  function scheduleProctoringReadyPing() {
    void emitProctoringReady();
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', () => void emitProctoringReady(), { once: true });
    }
  }

  function watchBootstrapForEarlyPipeline() {
    if (document.getElementById('risebird-proctoring-bootstrap')) {
      announceExtensionReady();
      void tryStartAutoPipeline();
      return;
    }
    const observer = new MutationObserver(() => {
      if (document.getElementById('risebird-proctoring-bootstrap')) {
        observer.disconnect();
        announceExtensionReady();
        void tryStartAutoPipeline();
      }
    });
    observer.observe(document.documentElement, { childList: true, subtree: true });
    setTimeout(() => observer.disconnect(), 60000);
  }

  scheduleProctoringReadyPing();
  watchBootstrapForEarlyPipeline();
  watchScreenShareOverlayRemoval();

  window.addEventListener('online', () => {
    if (!joinCompleted) void tryStartAutoPipeline();
    else void safeRuntimeSendMessage({ type: 'ENSURE_SESSION_STARTED' });
  });

  chrome.runtime.onMessage.addListener((message) => {
    if (message?.type === 'NOTIFY_NETWORK_ONLINE' && !joinCompleted) {
      void tryStartAutoPipeline();
    }
    if (message?.type === 'NOTIFY_CAPTURE_RECOVERY' && isScreenShareStillLive()) {
      hideScreenShareOverlay();
    }
  });

  window.setInterval(async () => {
    if (!joinCompleted && !proctoringStartForwarded) return;
    const st = await safeRuntimeSendMessage({ type: 'GET_PROCTORING_SETUP_STATE' });
    if (!st?.sessionActive) return;
    if (st.pipelineReady && st.hasLiveStream) return;
    if (st.captureStale || (!st.hasLiveStream && st.sessionActive)) {
      void tryShareRecoveryOnce();
    } else if (st.hasLiveStream && !st.pipelineReady) {
      void safeRuntimeSendMessage({ type: 'REQUEST_CAPTURE_RECOVERY' });
    }
  }, 20000);

  window.addEventListener('message', (event) => {

    if (event.source !== window || event.origin !== window.location.origin) return;



    if (event.data?.type === 'RB_PROCTORING_HELLO') {
      announceExtensionReady();
      void resumeProctoringAfterPageReload().then((resumed) => {
        if (!resumed && !hasAutoSharePickerBeenInvoked() && !proctoringStartForwarded) {
          void emitProctoringReady();
        }
      });
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
      void requestEntireScreenShareOnce(true);
    }

  });



  document.addEventListener('visibilitychange', () => {

    if (sessionActive && document.visibilityState !== 'visible') {

      recordViolation('page_hidden', `Page hidden or backgrounded | page: ${window.location.hostname}`, {

        eventCategory: 'page_visibility',

      });

    }

  });



  document.addEventListener('fullscreenchange', () => {

    if (sessionActive && !document.fullscreenElement) {

      recordViolation('fullscreen_exit', `Fullscreen exited | page: ${window.location.hostname}`, { eventCategory: 'fullscreen' });

    }

  });



  window.addEventListener('blur', () => {

    if (sessionActive) {

      recordViolation('window_blur', `Window blur — focus left this page | page: ${window.location.hostname}`, {

        eventCategory: 'window_blur',

      });

    }

  });



  document.addEventListener('copy', (event) => {

    if (!sessionActive) return;

    if (clipboardBlocking) event.preventDefault();

    recordViolation('copy', `Copy | page: ${window.location.hostname} | blocked: ${clipboardBlocking}`, {

      eventCategory: 'clipboard',

      blocked: clipboardBlocking,

    });

  });



  document.addEventListener('cut', (event) => {

    if (!sessionActive) return;

    if (clipboardBlocking) event.preventDefault();

    recordViolation('cut', `Cut | page: ${window.location.hostname} | blocked: ${clipboardBlocking}`, {

      eventCategory: 'clipboard',

      blocked: clipboardBlocking,

    });

  });



  document.addEventListener('paste', (event) => {

    if (!sessionActive) return;

    if (clipboardBlocking) event.preventDefault();

    recordViolation('paste', `Paste | page: ${window.location.hostname} | blocked: ${clipboardBlocking}`, {

      eventCategory: 'clipboard',

      blocked: clipboardBlocking,

    });

  });



  document.addEventListener('contextmenu', (event) => {

    if (!sessionActive) return;

    event.preventDefault();

    recordViolation('contextmenu', `Context menu | page: ${window.location.hostname}`, { eventCategory: 'contextmenu' });

  });

  /* Do not stop offscreen capture on beforeunload — form.submit() reloads the page between questions. */

})();

