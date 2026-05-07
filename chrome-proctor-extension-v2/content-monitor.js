/**
 * Screen chunks — standard client pattern:
 * - One getDisplayMedia(monitor) stream, one MediaRecorder, recorder.start(timesliceMs).
 * - Upload each non-empty Blob from ondataavailable after warm-up and basic quality gates
 *   (standalone WebM prefers EBML at offset 0; VP8 clusters often decode more reliably than VP9 fragments).
 * - Production alternative: stream to server and segment with ffmpeg; client-only upload remains chunked WebM.
 */
const monitorState = {
  active: false,
  stream: null,
  recorder: null,
  chunkSeq: 0,
  chunkIntervalMs: 5000,
  keyframePingIntervalId: null,
  /** When MediaRecorder started — used to skip warm-up chunks that preview black. */
  recordingStartedAt: null,
  /** Incident-triggered MediaRecorder.requestData(): accept next chunk even if small / non-EBML. */
  pendingIncidentFlushCount: 0,
  pendingEvidenceTrigger: null
};

/** Violation DOM hooks (tab blur, visibility, clipboard, etc.) — bound once per tab load. */
let violationListenersBound = false;

/** Single in-flight screen-share setup (page + popup can request capture at the same time). */
let pendingCaptureSetupPromise = null;

const bridgeState = {
  joined: false,
  lastJoinPayload: null,
  chunkIntervalMs: 5000
};

function nowIso() {
  return new Date().toISOString();
}

function blobToDataUrl(blob) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onloadend = () => resolve(reader.result);
    reader.onerror = () => reject(new Error("Unable to read media chunk."));
    reader.readAsDataURL(blob);
  });
}

async function reportViolation(type, message, metadata = {}) {
  try {
    await chrome.runtime.sendMessage({
      type: "REPORT_VIOLATION",
      payload: {
        type,
        message,
        metadata
      }
    });
  } catch (_error) {
    // Ignore to avoid recursive failures in page context.
  }
}

function emitPageSignal(type, payload = {}) {
  try {
    window.postMessage(
      {
        source: "risebird-proctor-extension",
        type,
        payload
      },
      window.location.origin
    );
  } catch (_error) {
    // Ignore page messaging errors.
  }
}

function normalizeJoinPayload(payload = {}) {
  const sessionIdCandidate =
    payload.sessionId ??
    payload.interviewId ??
    payload.interviewID ??
    "";

  return {
    backendBaseUrl: String(payload.backendBaseUrl || payload.backendUrl || window.location.origin || "").trim(),
    sessionToken: String(payload.sessionToken || "").trim(),
    sessionId: String(sessionIdCandidate || "").trim(),
    candidateId: String(payload.candidateId || payload.email || payload.candidateEmail || "").trim()
  };
}

async function syncPopupInputs(normalized) {
  try {
    await chrome.storage.local.set({
      "risebird.proctor.popup.inputs": {
        backendBaseUrl: normalized.backendBaseUrl || "",
        sessionToken: normalized.sessionToken || "",
        sessionId: normalized.sessionId || "",
        candidateId: normalized.candidateId || ""
      }
    });
  } catch (_error) {
    // Non-blocking: session bootstrap should continue even if popup cache write fails.
  }
}

function stopMediaResources() {
  if (monitorState.keyframePingIntervalId !== null) {
    window.clearInterval(monitorState.keyframePingIntervalId);
    monitorState.keyframePingIntervalId = null;
  }

  if (monitorState.recorder && monitorState.recorder.state !== "inactive") {
    monitorState.recorder.stop();
  }

  if (monitorState.stream) {
    for (const track of monitorState.stream.getTracks()) {
      track.stop();
    }
  }

  monitorState.recorder = null;
  monitorState.stream = null;
  monitorState.active = false;
  monitorState.pendingIncidentFlushCount = 0;
  monitorState.pendingEvidenceTrigger = null;
  monitorState.recordingStartedAt = null;
}

function ensureViolationListeners() {
  if (violationListenersBound) {
    return;
  }
  violationListenersBound = true;

  document.addEventListener(
    "copy",
    (event) => {
      if (!bridgeState.joined) {
        return;
      }
      event.preventDefault();
      reportViolation("COPY_ATTEMPT", "Copy action attempted.");
    },
    true
  );

  document.addEventListener(
    "cut",
    (event) => {
      if (!bridgeState.joined) {
        return;
      }
      event.preventDefault();
      reportViolation("CUT_ATTEMPT", "Cut action attempted.");
    },
    true
  );

  document.addEventListener(
    "paste",
    (event) => {
      if (!bridgeState.joined) {
        return;
      }
      event.preventDefault();
      reportViolation("PASTE_ATTEMPT", "Paste action attempted.");
    },
    true
  );

  document.addEventListener(
    "contextmenu",
    (event) => {
      if (!bridgeState.joined) {
        return;
      }
      event.preventDefault();
      reportViolation("RIGHT_CLICK_ATTEMPT", "Right click/context menu attempted.");
    },
    true
  );

  document.addEventListener("visibilitychange", () => {
    if (!bridgeState.joined) {
      return;
    }
    if (document.visibilityState === "hidden") {
      reportViolation("PAGE_HIDDEN", "Candidate moved page to background (tab switch or minimized).");
    }
  });

  document.addEventListener("fullscreenchange", () => {
    if (!bridgeState.joined) {
      return;
    }
    if (!document.fullscreenElement) {
      reportViolation("FULLSCREEN_EXIT", "Candidate exited fullscreen mode.");
    }
  });

  window.addEventListener("blur", () => {
    if (!bridgeState.joined) {
      return;
    }
    reportViolation("WINDOW_BLUR", "Browser window lost focus (e.g. switched to another app).");
  });
}

function chooseSupportedMimeType(candidates) {
  /* Prefer VP8 first: fragments often remain decodable as standalone WebM in `<video>` across browsers. */
  const fallback = ["video/webm;codecs=vp8", "video/webm;codecs=vp9", "video/webm"];
  const pool = Array.isArray(candidates) && candidates.length > 0 ? candidates : fallback;
  return pool.find((value) => MediaRecorder.isTypeSupported(value)) || "";
}

/** Minimum timeslice (ms): aligns with periodic upload cadence; larger slices carry fuller clusters per chunk. */
const MIN_SCREEN_CHUNK_INTERVAL_MS = 8000;

/** Skip periodic uploads briefly after share starts — encoder/UI warm-up often yields unusable fragments. Incident flushes bypass. */
const SCREEN_RECORDING_WARMUP_MS = 10000;

async function isEbmlWebmMagic(blob) {
  const head = await blob.slice(0, 4).arrayBuffer();
  const u = new Uint8Array(head);
  return u.length >= 4 && u[0] === 0x1a && u[1] === 0x45 && u[2] === 0xdf && u[3] === 0xa3;
}

/**
 * Drop tiny or non-EBML fragments that preview blank in the dashboard; keep EBML-leading chunks or large continuation slices.
 */
async function shouldPersistScreenChunkBody(blob) {
  if (!blob || blob.size === 0) return false;
  if (blob.size >= 131072) return true;
  const ebml = await isEbmlWebmMagic(blob);
  if (ebml && blob.size >= 16384) return true;
  if (!ebml && blob.size >= 98304) return true;
  return false;
}

async function shouldPersistPeriodicScreenChunk(blob) {
  if (monitorState.recordingStartedAt && Date.now() - monitorState.recordingStartedAt < SCREEN_RECORDING_WARMUP_MS) {
    return false;
  }
  return shouldPersistScreenChunkBody(blob);
}

function createRecorder(stream, mimeType) {
  const options = { videoBitsPerSecond: 2500000 };
  if (mimeType) {
    options.mimeType = mimeType;
  }
  try {
    return new MediaRecorder(stream, options);
  } catch (_err) {
    return mimeType ? new MediaRecorder(stream, { mimeType }) : new MediaRecorder(stream);
  }
}

async function handleRecorderChunk(event) {
  if (!monitorState.active || !event.data || event.data.size === 0) {
    return;
  }

  let incidentReason = null;
  if ((monitorState.pendingIncidentFlushCount || 0) > 0) {
    monitorState.pendingIncidentFlushCount -= 1;
    incidentReason = monitorState.pendingEvidenceTrigger || "incident";
    if ((monitorState.pendingIncidentFlushCount || 0) === 0) {
      monitorState.pendingEvidenceTrigger = null;
    }
  }

  const persist =
    incidentReason !== null ? true : await shouldPersistPeriodicScreenChunk(event.data);
  if (!persist) {
    return;
  }

  const dataUrl = await blobToDataUrl(event.data);
  const payload = {
    chunkSeq: monitorState.chunkSeq,
    mimeType: event.data.type || "video/webm",
    capturedAt: nowIso(),
    data: dataUrl,
    evidenceTrigger: incidentReason || undefined
  };

  monitorState.chunkSeq += 1;
  await chrome.runtime.sendMessage({
    type: "UPLOAD_SCREEN_CHUNK",
    payload
  });
}

async function startMonitorOnlyCapture(payload = {}) {
  if (monitorState.active) {
    return { ok: true };
  }
  if (pendingCaptureSetupPromise) {
    return pendingCaptureSetupPromise;
  }

  pendingCaptureSetupPromise = runMonitorOnlyCapture(payload).finally(() => {
    pendingCaptureSetupPromise = null;
  });

  return pendingCaptureSetupPromise;
}

async function runMonitorOnlyCapture(payload = {}) {
  ensureViolationListeners();
  const requested = Number(payload.chunkIntervalMs) || 5000;
  monitorState.chunkIntervalMs = Math.max(MIN_SCREEN_CHUNK_INTERVAL_MS, requested);
  monitorState.chunkSeq = 0;

  const stream = await navigator.mediaDevices.getDisplayMedia({
    video: {
      displaySurface: "monitor"
    },
    audio: false
  });

  const [videoTrack] = stream.getVideoTracks();
  if (!videoTrack) {
    stream.getTracks().forEach((track) => track.stop());
    throw new Error("No screen video track is available.");
  }

  const settings = videoTrack.getSettings();
  if (settings.displaySurface && settings.displaySurface !== "monitor") {
    stream.getTracks().forEach((track) => track.stop());
    await reportViolation("NON_MONITOR_SHARE_REJECTED", "Tab/window sharing was rejected.", {
      displaySurface: settings.displaySurface
    });
    throw new Error("Only entire monitor sharing is allowed.");
  }

  videoTrack.onended = async () => {
    if (!monitorState.active) {
      return;
    }
    await reportViolation("SCREEN_SHARE_STOPPED", "Screen sharing stopped during active session.");
    stopMediaResources();
  };

  const mimeType = chooseSupportedMimeType(payload.mimeCandidates);
  const recorder = createRecorder(stream, mimeType);
  recorder.ondataavailable = (event) => {
    handleRecorderChunk(event).catch(async (error) => {
      await reportViolation("SCREEN_CHUNK_PIPELINE_ERROR", "Unable to process recorder chunk.", {
        error: String(error.message || error)
      });
    });
  };
  recorder.onerror = async (event) => {
    await reportViolation("MEDIA_RECORDER_ERROR", "MediaRecorder error occurred.", {
      error: String(event.error?.message || "Unknown recorder error")
    });
  };

  recorder.start(monitorState.chunkIntervalMs);
  monitorState.stream = stream;
  monitorState.recorder = recorder;
  monitorState.active = true;
  monitorState.recordingStartedAt = Date.now();

  if (typeof videoTrack.requestFrame === "function") {
    monitorState.keyframePingIntervalId = window.setInterval(() => {
      try {
        videoTrack.requestFrame();
      } catch (_err) {
        // Ignore (track ended or unsupported).
      }
    }, 1600);
  }
  emitPageSignal("RB_PROCTORING_CAPTURE_ACTIVE", {
    chunkIntervalMs: monitorState.chunkIntervalMs
  });
  return { ok: true };
}

async function stopMonitoring() {
  if (!monitorState.active) {
    return { ok: true };
  }
  stopMediaResources();
  return { ok: true };
}

async function joinSessionFromPage(payload = {}) {
  const normalized = normalizeJoinPayload(payload);
  bridgeState.lastJoinPayload = payload;
  if (!normalized.backendBaseUrl || !normalized.sessionToken || !normalized.sessionId) {
    throw new Error("backendBaseUrl, sessionToken and sessionId are required.");
  }

  await syncPopupInputs(normalized);

  const response = await chrome.runtime.sendMessage({
    type: "START_SESSION",
    payload: normalized
  });
  if (!response?.ok) {
    throw new Error(response?.error || "Unable to start proctoring session.");
  }

  try {
    await chrome.storage.local.set({ "risebird.proctor.session.manualStopLocked": true });
  } catch (_e) {
    // Ignore storage failures; session still runs.
  }

  bridgeState.joined = true;
  bridgeState.chunkIntervalMs = Number(response?.state?.chunkIntervalMs) || bridgeState.chunkIntervalMs;
  ensureViolationListeners();
  return response;
}

async function completeSetupFromPage() {
  if (!bridgeState.joined && bridgeState.lastJoinPayload) {
    await joinSessionFromPage(bridgeState.lastJoinPayload);
  }
  if (!bridgeState.joined) {
    throw new Error("Proctoring join was not completed.");
  }
  return startMonitorOnlyCapture({ chunkIntervalMs: bridgeState.chunkIntervalMs });
}

async function stopSessionFromPage() {
  bridgeState.joined = false;
  await stopMonitoring();
  await chrome.runtime.sendMessage({
    type: "STOP_SESSION",
    reason: "Page signaled end of proctoring session.",
    force: true
  }).catch(() => undefined);
}

function handlePageSignal(data) {
  const type = data?.type;
  if (!type) {
    return;
  }

  if (type === "RB_PROCTORING_HELLO") {
    emitPageSignal("RB_PROCTORING_EXTENSION_READY");
    return;
  }

  if (type === "RB_PROCTORING_JOIN") {
    joinSessionFromPage(data.payload || {})
      .then(() => emitPageSignal("RB_PROCTORING_EXTENSION_READY"))
      .catch((error) => {
        reportViolation("SESSION_BOOTSTRAP_INVALID", "Unable to initialize session from page payload.", {
          error: String(error.message || error)
        });
        emitPageSignal("RB_PROCTORING_EXTENSION_ERROR", { error: String(error.message || error) });
      });
    return;
  }

  if (type === "RB_COMPLETE_TEST_SETUP") {
    completeSetupFromPage().catch((error) => {
      reportViolation("MONITOR_SETUP_FAILED", "Unable to start monitor capture from page setup signal.", {
        error: String(error.message || error)
      });
      emitPageSignal("RB_PROCTORING_EXTENSION_ERROR", { error: String(error.message || error) });
    });
    return;
  }

  if (type === "RB_PROCTORING_MEETING_END") {
    stopSessionFromPage().catch(() => undefined);
  }
}

window.addEventListener("message", (event) => {
  if (event.source !== window) {
    return;
  }
  handlePageSignal(event.data);
});

document.addEventListener("risebird-proctoring-postmessage", (event) => {
  handlePageSignal(event.detail);
});

chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
  (async () => {
    if (message?.type === "START_MONITORING") {
      const result = await startMonitorOnlyCapture(message.payload || {});
      sendResponse(result);
      return;
    }

    if (message?.type === "STOP_MONITORING") {
      bridgeState.joined = false;
      const result = await stopMonitoring();
      sendResponse(result);
      return;
    }

    if (message?.type === "FLUSH_SCREEN_RECORDING") {
      if (!monitorState.active || !monitorState.recorder || monitorState.recorder.state !== "recording") {
        sendResponse({ ok: false, error: "No active screen recording." });
        return;
      }
      if (typeof monitorState.recorder.requestData !== "function") {
        sendResponse({ ok: false, error: "requestData not supported." });
        return;
      }
      const reason = String(message.reason || "incident").slice(0, 120);
      monitorState.pendingEvidenceTrigger = reason;
      monitorState.pendingIncidentFlushCount = (monitorState.pendingIncidentFlushCount || 0) + 1;
      try {
        monitorState.recorder.requestData();
      } catch (_err) {
        monitorState.pendingIncidentFlushCount = Math.max(0, (monitorState.pendingIncidentFlushCount || 0) - 1);
        sendResponse({ ok: false, error: "requestData failed." });
        return;
      }
      sendResponse({ ok: true });
      return;
    }

    sendResponse({ ok: false, error: "Unknown message type." });
  })().catch((error) => {
    sendResponse({ ok: false, error: String(error.message || error) });
  });

  return true;
});
