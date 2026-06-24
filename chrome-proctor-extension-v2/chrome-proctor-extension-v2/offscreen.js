/**
 * Offscreen — desktop capture; uploads when screen share is live and frames have size.
 */
(function initOffscreenCapture() {
  const DEFAULT_SCREENSHOT_MS = CAPTURE_BASE_MS;
  const DEFAULT_SCREEN_CHUNK_MS = CAPTURE_BASE_MS;
  const MIN_CHUNK_BYTES = 1024;
  const MIN_CHUNK_UPLOAD_BYTES = 16384;
  const SCREEN_VIDEO_BITS_PER_SECOND = 2200000;
  const CHUNK_UPLOAD_WORKER_RETRIES = 16;
  let pendingChunkParts = [];
  const MAX_SCREENSHOT_WIDTH = 1280;
  const DIMENSIONS_WAIT_MS = 8000;
  const CONTENT_HINT_WAIT_MS = 4000;

  let screenStream = null;
  let screenRecorder = null;
  let screenChunkSeq = 0;
  let screenshotScheduleTimeout = null;
  let chunkScheduleTimeout = null;
  let chunkPeriodicFlushTimer = null;
  let frameWatchTimer = null;
  let captureVideoEl = null;
  let captureActive = false;
  let screenSharePromise = null;
  let screenshotIntervalMs = DEFAULT_SCREENSHOT_MS;
  let screenChunkIntervalMs = DEFAULT_SCREEN_CHUNK_MS;
  let usedDesktopCaptureStreamId = false;
  let framesLookValid = false;

  function sendToWorker(payload) {
    return new Promise((resolve) => {
      chrome.runtime.sendMessage(payload, (response) => {
        if (chrome.runtime.lastError) {
          resolve({ ok: false, retry: true, error: chrome.runtime.lastError.message });
          return;
        }
        resolve(response || { ok: false, retry: true });
      });
    });
  }

  function blobToDataUrl(blob) {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onloadend = () => resolve(String(reader.result || ''));
      reader.onerror = () => reject(new Error('Failed to encode blob.'));
      reader.readAsDataURL(blob);
    });
  }

  function stopCaptureVideoElement() {
    if (!captureVideoEl) return;
    try {
      captureVideoEl.srcObject = null;
      captureVideoEl.remove();
    } catch (_) {
      /* ignore */
    }
    captureVideoEl = null;
  }

  function stopFrameWatch() {
    if (frameWatchTimer) {
      clearInterval(frameWatchTimer);
      frameWatchTimer = null;
    }
  }

  function hasVideoDimensions() {
    const w = captureVideoEl?.videoWidth || 0;
    const h = captureVideoEl?.videoHeight || 0;
    return w >= 8 && h >= 8;
  }

  /** Ready to record/upload: live share + decoded video size (no hard blank gate). */
  function isCapturePipelineReady() {
    return captureActive && hasLiveStream() && hasVideoDimensions();
  }

  function startFrameWatch(video) {
    stopFrameWatch();
    frameWatchTimer = setInterval(() => {
      if (!captureActive || !video) return;
      framesLookValid = sampleFrameHasContent(video);
    }, 2000);
  }

  function sampleFrameHasContent(video) {
    if (!hasVideoDimensions()) return false;
    try {
      const canvas = document.createElement('canvas');
      canvas.width = 48;
      canvas.height = 27;
      const ctx = canvas.getContext('2d', { willReadFrequently: true });
      if (!ctx) return true;
      ctx.drawImage(video, 0, 0, 48, 27);
      const data = ctx.getImageData(0, 0, 48, 27).data;
      let bright = 0;
      let n = 0;
      for (let i = 0; i < data.length; i += 16) {
        const luma = (data[i] * 299 + data[i + 1] * 587 + data[i + 2] * 114) / 1000;
        if (luma > 16) bright++;
        n++;
      }
      return n === 0 || bright / n >= 0.04;
    } catch (_) {
      return true;
    }
  }

  function stopScreenRecorder() {
    stopChunkSchedule();
    if (!screenRecorder) {
      resetPendingChunkParts();
      return;
    }
    const recorder = screenRecorder;
    recorder.onstop = () => {
      void flushPendingChunkParts(recorder, true);
      resetPendingChunkParts();
    };
    try {
      if (recorder.state === 'recording') recorder.requestData();
      if (recorder.state !== 'inactive') recorder.stop();
    } catch (_) {
      /* ignore */
    }
    screenRecorder.ondataavailable = null;
    screenRecorder.onerror = null;
    screenRecorder = null;
  }

  function stopScreenshotSchedule() {
    if (screenshotScheduleTimeout) {
      clearTimeout(screenshotScheduleTimeout);
      screenshotScheduleTimeout = null;
    }
  }

  function stopChunkSchedule() {
    if (chunkScheduleTimeout) {
      clearTimeout(chunkScheduleTimeout);
      chunkScheduleTimeout = null;
    }
    stopChunkPeriodicFlush();
  }

  function stopAllCapture() {
    captureActive = false;
    usedDesktopCaptureStreamId = false;
    framesLookValid = false;
    stopScreenRecorder();
    stopScreenshotSchedule();
    stopChunkSchedule();
    stopFrameWatch();
    stopCaptureVideoElement();
    if (screenStream) {
      screenStream.getTracks().forEach((t) => t.stop());
      screenStream = null;
    }
    void chrome.storage.local.set({ proctoringOffscreenCaptureActive: false });
  }

  function hasLiveStream() {
    return Boolean(
      screenStream && screenStream.getVideoTracks().some((t) => t.readyState === 'live')
    );
  }

  function isEntireScreenShare(videoTrack, fromDesktopCapturePicker) {
    const surface = (videoTrack?.getSettings?.().displaySurface || '').toLowerCase();
    if (surface === 'monitor') return true;
    if (surface === 'browser' || surface === 'window' || surface === 'tab') return false;
    if (fromDesktopCapturePicker && !surface) return true;
    return false;
  }

  async function waitForVideoDimensions(video, timeoutMs) {
    if (hasVideoDimensions()) return true;
    return new Promise((resolve) => {
      let done = false;
      const finish = (ok) => {
        if (done) return;
        done = true;
        clearTimeout(timer);
        video.removeEventListener('loadedmetadata', onReady);
        video.removeEventListener('resize', onReady);
        video.removeEventListener('playing', onReady);
        resolve(ok);
      };
      const onReady = () => {
        if ((video.videoWidth || 0) >= 8 && (video.videoHeight || 0) >= 8) finish(true);
      };
      video.addEventListener('loadedmetadata', onReady);
      video.addEventListener('resize', onReady);
      video.addEventListener('playing', onReady);
      const timer = setTimeout(() => finish(hasVideoDimensions()), timeoutMs);
      onReady();
    });
  }

  async function hintContentCheck(video) {
    const deadline = Date.now() + CONTENT_HINT_WAIT_MS;
    while (Date.now() < deadline) {
      if (sampleFrameHasContent(video)) {
        framesLookValid = true;
        return;
      }
      await new Promise((r) => setTimeout(r, 400));
    }
    framesLookValid = hasVideoDimensions();
  }

  async function uploadWithSessionRetry(type, payload) {
    const maxAttempts = type === 'UPLOAD_SCREEN_CHUNK' ? CHUNK_UPLOAD_WORKER_RETRIES : 12;
    for (let i = 0; i < maxAttempts; i++) {
      const res = await sendToWorker({ type, payload });
      if (res?.ok) return { ok: true };
      if (res?.retry !== true) return { ok: false, error: res?.error || 'upload_rejected' };
      await new Promise((r) => setTimeout(r, Math.min(10000, 600 * (i + 1))));
    }
    return { ok: false, error: 'upload_retries_exhausted' };
  }

  function chunkMimeType(recorder, blob) {
    return blob?.type || recorder?.mimeType || 'video/webm';
  }

  function resetPendingChunkParts() {
    pendingChunkParts = [];
  }

  async function uploadChunkBlob(blob, recorder) {
    if (!blob || blob.size < MIN_CHUNK_BYTES) return;
    if (!isCapturePipelineReady()) return;
    const video = captureVideoEl;
    const largeEnough = blob.size >= MIN_CHUNK_UPLOAD_BYTES;
    if (video && !largeEnough && !framesLookValid && !sampleFrameHasContent(video)) return;
    try {
      const dataUrl = await blobToDataUrl(blob);
      const res = await uploadWithSessionRetry('UPLOAD_SCREEN_CHUNK', {
        chunkSeq: screenChunkSeq++,
        mimeType: chunkMimeType(recorder, blob),
        capturedAt: Date.now(),
        data: dataUrl,
        captureSource: 'offscreen_display_media',
        captureContext: { captureHost: 'offscreen', bytes: blob.size },
      });
      if (!res.ok) {
        void sendToWorker({
          type: 'PROCTORING_VIOLATION',
          payload: {
            violationType: 'screen_chunk_upload_failed',
            message: 'Screen chunk upload failed',
            metadata: { eventCategory: 'screen_chunk', error: res.error || 'upload_failed' },
          },
        });
      }
    } catch (e) {
      void sendToWorker({
        type: 'PROCTORING_VIOLATION',
        payload: {
          violationType: 'screen_chunk_upload_failed',
          message: 'Screen chunk upload failed',
          metadata: { eventCategory: 'screen_chunk', error: e?.message || String(e) },
        },
      });
    }
  }

  async function flushPendingChunkParts(recorder, force) {
    if (!pendingChunkParts.length) return;
    const total = pendingChunkParts.reduce((sum, part) => sum + (part?.size || 0), 0);
    if (!force && total < MIN_CHUNK_BYTES) return;
    const blob = new Blob(pendingChunkParts, { type: chunkMimeType(recorder, pendingChunkParts[0]) });
    resetPendingChunkParts();
    await uploadChunkBlob(blob, recorder);
  }

  async function postScreenChunk(event, recorder) {
    if (!event?.data || event.data.size === 0) return;
    if (!isCapturePipelineReady()) {
      resetPendingChunkParts();
      return;
    }
    pendingChunkParts.push(event.data);
    const total = pendingChunkParts.reduce((sum, part) => sum + part.size, 0);
    if (total >= MIN_CHUNK_BYTES) {
      await flushPendingChunkParts(recorder, true);
    }
  }

  function createScreenRecorder(stream) {
    const mimeTypes = ['video/webm;codecs=vp8', 'video/webm;codecs=vp9', 'video/webm'];
    for (const mimeType of mimeTypes) {
      if (!MediaRecorder.isTypeSupported(mimeType)) continue;
      try {
        return new MediaRecorder(stream, {
          mimeType,
          videoBitsPerSecond: SCREEN_VIDEO_BITS_PER_SECOND,
        });
      } catch (_) {
        /* try next */
      }
    }
    return new MediaRecorder(stream, { videoBitsPerSecond: SCREEN_VIDEO_BITS_PER_SECOND });
  }

  function stopChunkPeriodicFlush() {
    if (chunkPeriodicFlushTimer) {
      clearTimeout(chunkPeriodicFlushTimer);
      chunkPeriodicFlushTimer = null;
    }
  }

  function startChunkPeriodicFlush(recorder) {
    stopChunkPeriodicFlush();
    const tick = () => {
      if (!captureActive || !recorder || recorder.state !== 'recording') {
        stopChunkPeriodicFlush();
        return;
      }
      try {
        if (isCapturePipelineReady()) recorder.requestData();
      } catch (_) {
        /* ignore */
      }
      void flushPendingChunkParts(recorder, false);
      chunkPeriodicFlushTimer = setTimeout(tick, CAPTURE_CHUNK_TIMESLICE_MS);
    };
    chunkPeriodicFlushTimer = setTimeout(tick, CAPTURE_CHUNK_TIMESLICE_MS);
  }

  function scheduleEarlyChunkFlush(recorder) {
    stopChunkSchedule();
    if (!recorder || recorder.state !== 'recording' || !captureActive) return;
    chunkScheduleTimeout = setTimeout(() => {
      try {
        if (recorder.state === 'recording' && isCapturePipelineReady()) {
          recorder.requestData();
        }
      } catch (_) {
        /* ignore */
      }
    }, CAPTURE_FIRST_CHUNK_MS);
  }

  function startScreenRecorder(stream) {
    if (screenRecorder && screenRecorder.state === 'recording') return;
    stopScreenRecorder();
    resetPendingChunkParts();
    const recorder = createScreenRecorder(stream);
    screenChunkSeq = 0;
    recorder.ondataavailable = (event) => {
      void postScreenChunk(event, recorder);
    };
    recorder.onerror = (e) => console.warn('offscreen MediaRecorder error', e?.error);
    const timesliceMs = videoChunkTimesliceMs(screenChunkIntervalMs);
    recorder.start(timesliceMs);
    scheduleEarlyChunkFlush(recorder);
    startChunkPeriodicFlush(recorder);
    screenRecorder = recorder;
  }

  async function ensureCaptureVideo(stream) {
    if (captureVideoEl && captureVideoEl.srcObject === stream) return captureVideoEl;
    stopCaptureVideoElement();
    const video = document.createElement('video');
    video.muted = true;
    video.playsInline = true;
    video.autoplay = true;
    video.srcObject = stream;
    document.body.appendChild(video);
    try {
      await video.play();
    } catch (e) {
      console.warn('offscreen video.play', e);
    }
    captureVideoEl = video;
    return video;
  }

  async function captureScreenshotFromStream() {
    if (!captureActive || !hasLiveStream()) return false;
    try {
      const video = await ensureCaptureVideo(screenStream);
      if (!(await waitForVideoDimensions(video, DIMENSIONS_WAIT_MS))) return false;

      const w = video.videoWidth || 0;
      const h = video.videoHeight || 0;
      const scale = w > MAX_SCREENSHOT_WIDTH ? MAX_SCREENSHOT_WIDTH / w : 1;
      const canvas = document.createElement('canvas');
      canvas.width = Math.max(1, Math.round(w * scale));
      canvas.height = Math.max(1, Math.round(h * scale));
      const ctx = canvas.getContext('2d');
      if (!ctx) return false;
      ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
      if (!framesLookValid && !sampleFrameHasContent(video)) return false;
      const dataUrl = canvas.toDataURL('image/jpeg', 0.7);
      const base64 = dataUrl.split(',')[1];
      if (!base64 || base64.length < 1200) return false;

      const res = await uploadWithSessionRetry('UPLOAD_SCREENSHOT', {
        imageBase64: base64,
        capturedAt: Date.now(),
        captureSource: 'offscreen_display_media',
        captureContext: { captureHost: 'offscreen', width: canvas.width, height: canvas.height },
      });
      if (!res.ok) {
        void sendToWorker({
          type: 'PROCTORING_VIOLATION',
          payload: {
            violationType: 'screenshot_upload_failed',
            message: 'Screenshot upload failed',
            metadata: { eventCategory: 'screenshot', error: res.error || 'upload_failed' },
          },
        });
      }
      return res.ok;
    } catch (e) {
      console.warn('offscreen screenshot', e);
      return false;
    }
  }

  function scheduleNextScreenshot(isFirstPass) {
    stopScreenshotSchedule();
    if (!captureActive || !hasLiveStream()) return;
    const delay =
      isFirstPass === true
        ? CAPTURE_FIRST_SCREENSHOT_MS
        : interviewScreenshotDelayMs(screenshotIntervalMs);
    screenshotScheduleTimeout = setTimeout(() => {
      void captureScreenshotFromStream().finally(() => scheduleNextScreenshot(false));
    }, delay);
  }

  function startScreenshotCapture() {
    stopScreenshotSchedule();
    void captureScreenshotFromStream();
    scheduleNextScreenshot(true);
  }

  function ensureRecordingPipeline(stream) {
    if (!captureActive || !hasLiveStream()) return;
    startScreenRecorder(stream);
    startScreenshotCapture();
  }

  async function acquireDesktopStream(desktopStreamId) {
    const sizeHints = {
      width: { ideal: 1920, max: 3840 },
      height: { ideal: 1080, max: 2160 },
      frameRate: { ideal: 15, max: 30 },
    };

    if (desktopStreamId) {
      usedDesktopCaptureStreamId = true;
      const attempts = [
        {
          audio: false,
          video: {
            mandatory: {
              chromeMediaSource: 'desktop',
              chromeMediaSourceId: desktopStreamId,
              maxWidth: 3840,
              maxHeight: 2160,
              maxFrameRate: 30,
            },
          },
        },
        {
          audio: false,
          video: {
            chromeMediaSource: 'desktop',
            chromeMediaSourceId: desktopStreamId,
            ...sizeHints,
          },
        },
      ];
      let lastErr = null;
      for (const constraints of attempts) {
        try {
          return await navigator.mediaDevices.getUserMedia(constraints);
        } catch (e) {
          lastErr = e;
          console.warn('offscreen getUserMedia desktop', e?.name || e);
        }
      }
      const hint =
        lastErr?.name === 'NotReadableError' || lastErr?.name === 'AbortError'
          ? ' Screen picker token may have expired — reload the page and share entire screen again.'
          : '';
      throw new Error(
        (lastErr?.message || 'Could not attach to the shared screen.') + hint
      );
    }

    usedDesktopCaptureStreamId = false;
    return navigator.mediaDevices.getDisplayMedia({
      video: { displaySurface: 'monitor', ...sizeHints },
      audio: false,
      preferCurrentTab: false,
    });
  }

  async function armStreamPipeline(stream) {
    const video = await ensureCaptureVideo(stream);
    await waitForVideoDimensions(video, DIMENSIONS_WAIT_MS);
    await hintContentCheck(video);
    if (!framesLookValid && !sampleFrameHasContent(video)) {
      await new Promise((r) => setTimeout(r, 1500));
      framesLookValid = sampleFrameHasContent(video);
    }
    if (!framesLookValid && hasVideoDimensions()) {
      framesLookValid = true;
    }
    startFrameWatch(video);
    ensureRecordingPipeline(stream);
    return { ok: true, framesLookValid, hasLiveStream: hasLiveStream(), pipelineReady: isCapturePipelineReady() };
  }

  async function startDisplayCapture(desktopStreamId) {
    if (hasLiveStream() && screenRecorder && isCapturePipelineReady()) {
      return { ok: true, reused: true };
    }
    if (screenSharePromise) return screenSharePromise;

    screenSharePromise = (async () => {
      try {
        if (hasLiveStream() && screenStream) {
          return await armStreamPipeline(screenStream);
        }

        const stream = await acquireDesktopStream(desktopStreamId);
        const [videoTrack] = stream.getVideoTracks();
        if (!videoTrack) {
          stream.getTracks().forEach((t) => t.stop());
          throw new Error('No video track in screen share.');
        }
        if (!isEntireScreenShare(videoTrack, usedDesktopCaptureStreamId)) {
          stream.getTracks().forEach((t) => t.stop());
          throw new Error('Please select Entire Screen (not tab or window).');
        }
        videoTrack.onended = () => {
          stopAllCapture();
          void sendToWorker({
            type: 'CONTENT_EVENT',
            level: 'violation',
            reason: 'Screen share ended — entire screen capture was stopped.',
            metadata: { eventCategory: 'screen_capture', event: 'screen_share_ended' },
          });
        };
        screenStream = stream;
        captureActive = true;
        await chrome.storage.local.set({ proctoringOffscreenCaptureActive: true });
        return await armStreamPipeline(stream);
      } catch (err) {
        stopAllCapture();
        return { ok: false, error: err?.message || String(err) };
      } finally {
        screenSharePromise = null;
      }
    })();

    return screenSharePromise;
  }

  chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
    const t = message?.type;
    if (message?.target && message.target !== 'offscreen') {
      return false;
    }
    if (!t || !String(t).startsWith('OFFSCREEN_')) {
      return false;
    }
    (async () => {
      if (message?.type === 'OFFSCREEN_GET_STATE') {
        sendResponse({
          ok: true,
          hasLiveStream: hasLiveStream(),
          captureActive,
          framesLookValid,
          pipelineReady: isCapturePipelineReady(),
          recording: screenRecorder?.state === 'recording',
          videoWidth: captureVideoEl?.videoWidth || 0,
          videoHeight: captureVideoEl?.videoHeight || 0,
        });
        return;
      }
      if (message?.type === 'OFFSCREEN_START_CAPTURE') {
        const settings = message.settings || {};
        screenshotIntervalMs = Number(settings.screenshotIntervalMs) || DEFAULT_SCREENSHOT_MS;
        screenChunkIntervalMs = Number(settings.screenChunkIntervalMs) || DEFAULT_SCREEN_CHUNK_MS;
        sendResponse(await startDisplayCapture(message.streamId || null));
        return;
      }
      if (message?.type === 'OFFSCREEN_ENSURE_RECORDING') {
        if (hasLiveStream() && screenStream) {
          sendResponse(await armStreamPipeline(screenStream));
          return;
        }
        sendResponse({ ok: false, error: 'No live screen share in offscreen.' });
        return;
      }
      if (message?.type === 'OFFSCREEN_STOP_CAPTURE') {
        stopAllCapture();
        sendResponse({ ok: true });
        return;
      }
      if (message?.type === 'OFFSCREEN_CAPTURE_SCREENSHOT_NOW') {
        const ok = await captureScreenshotFromStream();
        sendResponse({
          ok,
          hasLiveStream: hasLiveStream(),
          pipelineReady: isCapturePipelineReady(),
        });
        return;
      }
      sendResponse({ ok: false, error: `Unknown offscreen action: ${String(t)}` });
    })().catch((err) => sendResponse({ ok: false, error: err?.message || String(err) }));
    return true;
  });
})();
