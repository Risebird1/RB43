const elements = {
  backendBaseUrl: document.getElementById("backendBaseUrl"),
  sessionToken: document.getElementById("sessionToken"),
  sessionId: document.getElementById("sessionId"),
  candidateId: document.getElementById("candidateId"),
  restrictedDomainsList: document.getElementById("restrictedDomainsList"),
  allowedHostsList: document.getElementById("allowedHostsList"),
  status: document.getElementById("status"),
  violationsCount: document.getElementById("violationsCount"),
  screenshotUploads: document.getElementById("screenshotUploads"),
  lastScreenshotAt: document.getElementById("lastScreenshotAt"),
  message: document.getElementById("message"),
  startBtn: document.getElementById("startBtn"),
  stopBtn: document.getElementById("stopBtn"),
  refreshBtn: document.getElementById("refreshBtn")
};

function setMessage(text, isError = false) {
  elements.message.textContent = text || "";
  elements.message.style.color = isError ? "#ffb3ba" : "#9fbee8";
}

function setStatus(state) {
  const isActive = Boolean(state?.sessionActive);
  elements.status.className = `status ${isActive ? "status-active" : "status-idle"}`;
  elements.status.textContent = isActive ? `Active: ${state.sessionId}` : "Inactive";
  elements.violationsCount.textContent = String(state?.violationCount || 0);
  elements.screenshotUploads.textContent = String(state?.screenshotUploads || 0);
  elements.lastScreenshotAt.textContent = state?.lastScreenshotAt || "Never";

  if (state?.lastError) {
    elements.status.className = "status status-error";
    elements.status.textContent = `Error: ${state.lastError}`;
  }

  const stopLocked = Boolean(isActive && state?.manualStopLocked);
  elements.stopBtn.disabled = stopLocked;
  elements.stopBtn.title = stopLocked
    ? "Session ends when the interview ends or you leave the interview tab."
    : "";
}

function renderDomainList(listEl, values) {
  if (!listEl) {
    return;
  }
  listEl.innerHTML = "";
  const entries = Array.isArray(values) ? values : [];
  if (entries.length === 0) {
    const node = document.createElement("li");
    node.textContent = "None";
    listEl.appendChild(node);
    return;
  }
  for (const value of entries) {
    const node = document.createElement("li");
    node.textContent = value;
    listEl.appendChild(node);
  }
}

function persistInputs() {
  if (!elements.backendBaseUrl || !elements.sessionToken || !elements.sessionId || !elements.candidateId) {
    return;
  }
  chrome.storage.local.set({
    "risebird.proctor.popup.inputs": {
      backendBaseUrl: elements.backendBaseUrl.value.trim(),
      sessionToken: elements.sessionToken.value.trim(),
      sessionId: elements.sessionId.value.trim(),
      candidateId: elements.candidateId.value.trim()
    }
  });
}

async function restoreInputs() {
  if (!elements.backendBaseUrl || !elements.sessionToken || !elements.sessionId || !elements.candidateId) {
    return;
  }
  const data = await chrome.storage.local.get("risebird.proctor.popup.inputs");
  const values = data["risebird.proctor.popup.inputs"] || {};
  elements.backendBaseUrl.value = values.backendBaseUrl || "";
  elements.sessionToken.value = values.sessionToken || "";
  elements.sessionId.value = values.sessionId || "";
  elements.candidateId.value = values.candidateId || "";
}

async function getActiveTab() {
  // From the toolbar popup, `currentWindow` is the popup itself — use last focused browser window.
  let tabs = await chrome.tabs.query({ active: true, lastFocusedWindow: true });
  if (!tabs[0] || typeof tabs[0].id !== "number") {
    tabs = await chrome.tabs.query({ active: true, windowType: "normal" });
  }
  if (!tabs[0] || typeof tabs[0].id !== "number") {
    throw new Error("No active exam tab found. Focus the interview tab and try again.");
  }
  return tabs[0];
}

async function sendRuntimeMessage(message) {
  const response = await chrome.runtime.sendMessage(message);
  if (!response?.ok) {
    throw new Error(response?.error || "Unknown extension runtime error.");
  }
  return response;
}

async function sendTabMessage(tabId, message) {
  const response = await chrome.tabs.sendMessage(tabId, message);
  if (!response?.ok) {
    throw new Error(response?.error || "Unable to communicate with exam tab.");
  }
  return response;
}

async function refreshState() {
  const response = await sendRuntimeMessage({ type: "GET_STATE" });
  const state = response.state || {};
  setStatus(state);
  renderDomainList(elements.restrictedDomainsList, state.restrictedDomains || []);
  renderDomainList(elements.allowedHostsList, state.allowedHosts || []);
}

async function startSession() {
  setMessage("Starting proctoring session...");
  persistInputs();

  const tab = await getActiveTab();
  if (!elements.backendBaseUrl || !elements.sessionToken || !elements.sessionId || !elements.candidateId) {
    throw new Error("Session form is missing from the popup UI.");
  }
  const payload = {
    backendBaseUrl: elements.backendBaseUrl.value.trim(),
    sessionToken: elements.sessionToken.value.trim(),
    sessionId: elements.sessionId.value.trim(),
    candidateId: elements.candidateId.value.trim(),
    tabId: tab.id
  };

  const startResponse = await sendRuntimeMessage({
    type: "START_SESSION",
    payload
  });

  try {
    await sendTabMessage(tab.id, {
      type: "START_MONITORING",
      payload: {
        chunkIntervalMs: startResponse.state.chunkIntervalMs
      }
    });
  } catch (error) {
    await sendRuntimeMessage({
      type: "STOP_SESSION",
      reason: "Monitor-only screen share setup failed.",
      force: true
    }).catch(() => undefined);
    throw error;
  }

  await refreshState();
  setMessage("Proctoring session started.");
}

async function stopSession() {
  setMessage("Stopping proctoring session...");
  const tab = await getActiveTab().catch(() => null);

  if (tab?.id) {
    await sendTabMessage(tab.id, { type: "STOP_MONITORING" }).catch(() => undefined);
  }

  await sendRuntimeMessage({
    type: "STOP_SESSION",
    reason: "Stopped by candidate/invigilator from extension."
  });
  await refreshState();
  setMessage("Proctoring session stopped.");
}

async function onStartClick() {
  elements.startBtn.disabled = true;
  try {
    await startSession();
  } catch (error) {
    setMessage(String(error.message || error), true);
  } finally {
    elements.startBtn.disabled = false;
  }
}

async function onStopClick() {
  elements.stopBtn.disabled = true;
  try {
    await stopSession();
  } catch (error) {
    setMessage(String(error.message || error), true);
  } finally {
    await refreshState().catch(() => undefined);
  }
}

async function init() {
  await restoreInputs();
  await refreshState();

  elements.startBtn.addEventListener("click", onStartClick);
  elements.stopBtn.addEventListener("click", onStopClick);
  elements.refreshBtn.addEventListener("click", () => {
    refreshState().catch((error) => setMessage(String(error.message || error), true));
  });
  setInterval(() => {
    refreshState().catch(() => undefined);
  }, 3000);
}

init().catch((error) => {
  setMessage(String(error.message || error), true);
});
