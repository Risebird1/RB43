const monitorState = {
  active: false,
  stream: null,
  recorder: null,
  chunkSeq: 0,
  chunkIntervalMs: 5000,
  eventListenersBound: false
};

const bridgeState = {
  joined: false,
  lastJoinPayload: null,
  chunkIntervalMs: 5000,
  popupOpenedForSessionId: null
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

async function requestPopupOpen(normalized = {}, reason = "page-join") {
  const sessionId = String(normalized.sessionId || "").trim();
  if (!sessionId) {
    return;
  }
  if (bridgeState.popupOpenedForSessionId === sessionId) {
    return;
  }

  try {
    const response = await chrome.runtime.sendMessage({
      type: "OPEN_PROCTORING_POPUP",
      payload: {
        sessionId,
        reason
      }
    });
    if (response?.ok) {
      bridgeState.popupOpenedForSessionId = sessionId;
    }
  } catch (_error) {
    // Non-blocking: page bootstrap should continue even when popup open fails.
  }
}

function stopMediaResources() {
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
}

function bindViolationListeners() {
  if (monitorState.eventListenersBound) {
    return;
  }

  document.addEventListener(
    "copy",
    (event) => {
      if (!monitorState.active) {
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
      if (!monitorState.active) {
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
      if (!monitorState.active) {
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
      if (!monitorState.active) {
        return;
      }
      event.preventDefault();
      reportViolation("RIGHT_CLICK_ATTEMPT", "Right click/context menu attempted.");
    },
    true
  );

  document.addEventListener("visibilitychange", () => {
    if (!monitorState.active) {
      return;
    }
    if (document.visibilityState === "hidden") {
      reportViolation("PAGE_HIDDEN", "Candidate moved page to background.");
    }
  });

  document.addEventListener("fullscreenchange", () => {
    if (!monitorState.active) {
      return;
    }
    if (!document.fullscreenElement) {
      reportViolation("FULLSCREEN_EXIT", "Candidate exited fullscreen mode.");
    }
  });

  window.addEventListener("blur", () => {
    if (!monitorState.active) {
      return;
    }
    reportViolation("WINDOW_BLUR", "Browser window lost focus.");
  });

  monitorState.eventListenersBound = true;
}

function chooseSupportedMimeType(candidates) {
  const fallback = ["video/webm;codecs=vp9", "video/webm;codecs=vp8", "video/webm"];
  const pool = Array.isArray(candidates) && candidates.length > 0 ? candidates : fallback;
  return pool.find((value) => MediaRecorder.isTypeSupported(value)) || "";
}

function createRecorder(stream, mimeType) {
  const options = mimeType ? { mimeType } : undefined;
  return new MediaRecorder(stream, options);
}

async function handleRecorderChunk(event) {
  if (!monitorState.active || !event.data || event.data.size === 0) {
    return;
  }

  const dataUrl = await blobToDataUrl(event.data);
  const payload = {
    chunkSeq: monitorState.chunkSeq,
    mimeType: event.data.type || "video/webm",
    capturedAt: nowIso(),
    data: dataUrl
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

  bindViolationListeners();
  monitorState.chunkIntervalMs = Number(payload.chunkIntervalMs) || 5000;
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
  await requestPopupOpen(normalized, "page-join");

  const response = await chrome.runtime.sendMessage({
    type: "START_SESSION",
    payload: normalized
  });
  if (!response?.ok) {
    throw new Error(response?.error || "Unable to start proctoring session.");
  }

  bridgeState.joined = true;
  bridgeState.chunkIntervalMs = Number(response?.state?.chunkIntervalMs) || bridgeState.chunkIntervalMs;
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
  await stopMonitoring();
  await chrome.runtime.sendMessage({
    type: "STOP_SESSION",
    reason: "Page signaled end of proctoring session."
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
    // Legacy operator flow: keep the screen-share action in extension popup button.
    // The popup is prefilled and auto-opened from join to let candidate select "Entire Screen" manually.
    requestPopupOpen(normalizeJoinPayload(bridgeState.lastJoinPayload || {}), "complete-test-setup").catch(() => undefined);
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
      const result = await stopMonitoring();
      sendResponse(result);
      return;
    }

    sendResponse({ ok: false, error: "Unknown message type." });
  })().catch((error) => {
    sendResponse({ ok: false, error: String(error.message || error) });
  });

  return true;
});
