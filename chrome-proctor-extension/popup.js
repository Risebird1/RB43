const statusEl = document.getElementById('status');
const violationsEl = document.getElementById('violations');
const messageEl = document.getElementById('message');
const restrictedDomainsEl = document.getElementById('restrictedDomains');
const eventsEl = document.getElementById('events');

let screenStream = null;
let cameraStream = null;

function showMessage(text, isError = false) {
  messageEl.textContent = text;
  messageEl.style.color = isError ? '#b42318' : '#1f6feb';
}

function sendMessage(payload) {
  return new Promise((resolve, reject) => {
    chrome.runtime.sendMessage(payload, (response) => {
      if (chrome.runtime.lastError) {
        reject(new Error(chrome.runtime.lastError.message));
        return;
      }
      if (!response?.ok) {
        reject(new Error(response?.error ?? 'Unknown error'));
        return;
      }
      resolve(response);
    });
  });
}

function stopTracks(stream) {
  if (!stream) return;
  stream.getTracks().forEach((track) => track.stop());
}

async function runCaptureCheck() {
  screenStream = await navigator.mediaDevices.getDisplayMedia({ video: true, audio: false });
  cameraStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });

  await sendMessage({
    type: 'CAPTURE_STATUS',
    reason: 'Screen and camera capture granted.',
    metadata: {
      screenTracks: screenStream.getVideoTracks().length,
      cameraTracks: cameraStream.getVideoTracks().length,
      micTracks: cameraStream.getAudioTracks().length
    }
  });
}

function stopCaptureCheck() {
  stopTracks(screenStream);
  stopTracks(cameraStream);
  screenStream = null;
  cameraStream = null;
}

function escapeHtml(text) {
  const div = document.createElement('div');
  div.textContent = text == null ? '' : String(text);
  return div.innerHTML;
}

/** Rule + host for list row; parses metadata.url if hostname missing. */
function domainDetailsFromEvent(event) {
  const m = event.metadata || {};
  let host = m.hostname;
  if (!host && m.url) {
    try {
      host = new URL(m.url).hostname;
    } catch {
      /* ignore */
    }
  }
  const rule = m.matchedRestrictedDomain;
  if (!host && !rule) return '';
  const parts = [];
  if (rule) parts.push(`rule ${rule}`);
  if (host) parts.push(`host ${host}`);
  return parts.length ? ` · ${parts.join(' · ')}` : '';
}

function renderEvents(events) {
  if (!events.length) {
    eventsEl.innerHTML = 'No events yet.';
    return;
  }

  const recent = [...events].reverse().slice(0, 20);
  eventsEl.innerHTML = recent
    .map((event) => {
      const cls = event.type === 'violation' ? 'event violation' : 'event';
      const time = new Date(event.timestamp).toLocaleTimeString();
      const detail = domainDetailsFromEvent(event);
      return `<div class="${cls}">${escapeHtml(event.type.toUpperCase())} • ${escapeHtml(time)} — ${escapeHtml(event.reason)}${escapeHtml(detail)}</div>`;
    })
    .join('');
}

async function refreshState() {
  try {
    const response = await sendMessage({ type: 'GET_STATE' });
    const state = response.state;
    statusEl.textContent = state.sessionActive ? 'Running' : 'Stopped';
    violationsEl.textContent = String(state.violations);
    restrictedDomainsEl.value = state.settings.restrictedDomains.join(', ');
    renderEvents(state.events || []);
  } catch (error) {
    showMessage(error.message, true);
  }
}

document.getElementById('startSession').addEventListener('click', async () => {
  try {
    await sendMessage({ type: 'START_SESSION' });
    showMessage('Proctoring session started.');
    await refreshState();
  } catch (error) {
    showMessage(error.message, true);
  }
});

document.getElementById('stopSession').addEventListener('click', async () => {
  try {
    await sendMessage({ type: 'STOP_SESSION' });
    showMessage('Proctoring session stopped.');
    await refreshState();
  } catch (error) {
    showMessage(error.message, true);
  }
});

document.getElementById('startCapture').addEventListener('click', async () => {
  try {
    await runCaptureCheck();
    showMessage('Capture check started (screen/camera/mic).');
  } catch (error) {
    await sendMessage({
      type: 'CAPTURE_STATUS',
      reason: 'Capture check failed.',
      metadata: { error: error.message }
    });
    showMessage(error.message, true);
  }
});

document.getElementById('stopCapture').addEventListener('click', async () => {
  try {
    stopCaptureCheck();
    await sendMessage({
      type: 'CAPTURE_STATUS',
      reason: 'Capture check stopped by proctor.'
    });
    showMessage('Capture check stopped.');
  } catch (error) {
    showMessage(error.message, true);
  }
});

document.getElementById('clearData').addEventListener('click', async () => {
  try {
    const hours = Number(document.getElementById('hours').value || 1);
    await sendMessage({ type: 'CLEAR_BROWSING_DATA', hours });
    showMessage(`Browsing data cleared for last ${hours} hour(s).`);
    await refreshState();
  } catch (error) {
    showMessage(error.message, true);
  }
});

document.getElementById('saveSettings').addEventListener('click', async () => {
  try {
    const restrictedDomains = restrictedDomainsEl.value
      .split(',')
      .map((item) => item.trim().toLowerCase())
      .filter(Boolean);

    await sendMessage({
      type: 'UPDATE_SETTINGS',
      settings: { restrictedDomains }
    });

    showMessage('Monitoring rules saved.');
    await refreshState();
  } catch (error) {
    showMessage(error.message, true);
  }
});

document.getElementById('clearEvents').addEventListener('click', async () => {
  try {
    await sendMessage({ type: 'CLEAR_EVENTS' });
    showMessage('Event log cleared.');
    await refreshState();
  } catch (error) {
    showMessage(error.message, true);
  }
});

document.getElementById('downloadReport').addEventListener('click', async () => {
  try {
    const response = await sendMessage({ type: 'GET_REPORT' });
    const blob = new Blob([JSON.stringify(response.report, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `proctoring-report-${Date.now()}.json`;
    anchor.click();
    URL.revokeObjectURL(url);
    showMessage('Report downloaded.');
  } catch (error) {
    showMessage(error.message, true);
  }
});

document.getElementById('refresh').addEventListener('click', refreshState);

window.addEventListener('beforeunload', stopCaptureCheck);

refreshState();
