const statusEl = document.getElementById('status');
const violationsEl = document.getElementById('violations');
const messageEl = document.getElementById('message');
const eventsEl = document.getElementById('events');
let refreshInFlight = false;
let liveRefreshTimer = null;

function showMessage(text, isError = false) {
  messageEl.textContent = text;
  messageEl.style.color = isError ? '#b42318' : '#1f6feb';
}

/** Optional broad http(s) hosts — requested on user click so Chrome shows the permission dialog (store-friendly vs static broad host_permissions). */
async function ensureWebAccessForProctoring() {
  const origins = ['http://*/*', 'https://*/*'];
  try {
    if (await chrome.permissions.contains({ origins })) return true;
  } catch (e) {
    console.warn('ensureWebAccessForProctoring contains', e);
  }
  return new Promise((resolve) => {
    chrome.permissions.request({ origins }, (granted) => {
      if (chrome.runtime.lastError) {
        console.warn(chrome.runtime.lastError);
        resolve(false);
        return;
      }
      resolve(!!granted);
    });
  });
}

function sendMessage(payload) {
  return new Promise((resolve, reject) => {
    chrome.runtime.sendMessage(payload, (response) => {
      if (chrome.runtime.lastError) {
        return reject(new Error(chrome.runtime.lastError.message));
      }
      if (response == null) {
        return reject(new Error('Extension did not respond. Reload the extension or this popup.'));
      }
      if (!response.ok) return reject(new Error(response.error ?? 'Unknown error'));
      resolve(response);
    });
  });
}

function escapeHtml(text) {
  const div = document.createElement('div');
  div.textContent = text == null ? '' : String(text);
  return div.innerHTML;
}

function formatEventMetaLine(m) {
  if (!m || typeof m !== 'object') return '';
  const parts = [];
  if (m.activeTabHostname) parts.push(`host: ${m.activeTabHostname}`);
  if (m.activeTabUrl) parts.push(`tab url: ${String(m.activeTabUrl).slice(0, 100)}`);
  if (m.lastActiveTabHostname) parts.push(`last tab host: ${m.lastActiveTabHostname}`);
  if (m.lastActiveTabUrl) parts.push(`last tab url: ${String(m.lastActiveTabUrl).slice(0, 100)}`);
  if (m.lastTabTitle) parts.push(`last title: ${String(m.lastTabTitle).slice(0, 50)}${String(m.lastTabTitle).length > 50 ? '…' : ''}`);
  if (Array.isArray(m.recentTabHosts) && m.recentTabHosts.length) {
    parts.push(`recent hosts: ${m.recentTabHosts.join(', ')}`);
  }
  if (m.tabTitle) parts.push(`title: ${String(m.tabTitle).slice(0, 60)}${String(m.tabTitle).length > 60 ? '…' : ''}`);
  if (m.captureModeLabel || m.displaySurface) parts.push(`capture: ${m.captureModeLabel || m.displaySurface}`);
  if (m.pageHostname) parts.push(`page: ${m.pageHostname}`);
  if (m.isEntireScreen === true) parts.push('entire screen: yes');
  if (m.isEntireScreen === false) parts.push('entire screen: no');
  if (m.isAllowedExamHost === false) parts.push('exam host: no');
  if (m.isAllowedExamHost === true) parts.push('exam host: yes');
  if (m.blocked === true) parts.push('status: BLOCKED');
  if (m.blockedHostname) parts.push(`blocked host: ${m.blockedHostname}`);
  if (m.requestUrl) parts.push(`url: ${String(m.requestUrl).slice(0, 100)}`);
  if (m.ruleId != null) parts.push(`rule: ${m.ruleId}`);
  if (m.detectionSource) parts.push(`via: ${m.detectionSource}`);
  if (m.error) parts.push(`error: ${String(m.error).slice(0, 100)}`);
  if (m.chunkSeq != null) parts.push(`chunk: ${m.chunkSeq}`);
  return parts.length ? parts.join(' · ') : '';
}

function renderEvents(events) {
  if (!eventsEl) return;
  if (!events?.length) {
    eventsEl.innerHTML = '<div class="hint">No events yet.</div>';
    return;
  }
  eventsEl.innerHTML = [...events]
    .reverse()
    .slice(0, 40)
    .map((event) => {
      const level = (event.level || event.type || 'info').toUpperCase();
      const viol = String(event.level || event.type || '').toLowerCase() === 'violation';
      const metaLine = formatEventMetaLine(event.metadata);
      const cat = event.metadata && event.metadata.eventCategory ? ` [${event.metadata.eventCategory}]` : '';
      return `<div class="event${viol ? ' violation' : ''}"><div class="ev-head">${escapeHtml(level)}${escapeHtml(cat)} · ${escapeHtml(
        new Date(event.timestamp).toLocaleTimeString()
      )}</div><div class="ev-reason">${escapeHtml(event.reason || '')}</div>${
        metaLine ? `<div class="ev-meta">${escapeHtml(metaLine)}</div>` : ''
      }</div>`;
    })
    .join('');
}

async function applySavedJoinFields() {
  try {
    const data = await chrome.storage.local.get(['proctoringLastJoin']);
    const j = data.proctoringLastJoin;
    if (!j) return null;
    document.getElementById('name').value = j.name != null ? String(j.name) : '';
    document.getElementById('email').value = j.email != null ? String(j.email) : '';
    document.getElementById('token').value = j.sessionToken != null ? String(j.sessionToken) : '';
    document.getElementById('interviewId').value =
      j.interviewId !== undefined && j.interviewId !== null && j.interviewId !== '' ? String(j.interviewId) : '';
    return j;
  } catch (e) {
    console.warn('applySavedJoinFields', e);
    return null;
  }
}

async function refreshState() {
  if (refreshInFlight) return;
  refreshInFlight = true;
  try {
    const savedJoin = await applySavedJoinFields();
    const { state } = await sendMessage({ type: 'GET_STATE' });
    const needWebAccess =
      state.joined === true && state.hasWebMonitoringAccess !== true;

    let status =
      state.sessionActive ? 'Running' : state.joined ? 'Joined' : 'Idle';
    statusEl.textContent = status;
    const screenshotInfo = state.lastScreenshotAt
      ? ` · screenshots: ${state.screenshotUploads || 0} (last ${new Date(state.lastScreenshotAt).toLocaleTimeString()})`
      : ` · screenshots: ${state.screenshotUploads || 0}`;
    violationsEl.textContent = `${state.violations}${screenshotInfo}`;

    const banner = document.getElementById('webAccessBanner');
    const enableBtn = document.getElementById('enableWebMonitoring');
    if (banner) {
      banner.hidden = !needWebAccess;
      if (needWebAccess && enableBtn) {
        requestAnimationFrame(() => {
          try {
            enableBtn.focus();
          } catch (_) {
            /* ignore */
          }
        });
      }
    }
    document.getElementById('clipboardBlocking').checked = Boolean(state.settings.clipboardBlocking);
    const actionsSection = document.getElementById('sessionActionsSection');
    if (actionsSection) {
      // Popup is status-only now; hide manual action controls always.
      actionsSection.hidden = true;
    }
    renderEvents(state.events);
  } catch (e) {
    showMessage(e.message || String(e), true);
  } finally {
    refreshInFlight = false;
  }
}

document.getElementById('joinSession').addEventListener('click', async () => {
  try {
    const payload = {
      name: document.getElementById('name').value.trim(),
      email: document.getElementById('email').value.trim(),
      sessionToken: document.getElementById('token').value.trim(),
      interviewId: document.getElementById('interviewId').value.trim(),
    };

    if (!payload.sessionToken || !payload.interviewId) {
      throw new Error('Session Token and Interview ID are required.');
    }

    if (!(await ensureWebAccessForProctoring())) {
      throw new Error(
        'Allow access to all websites when Chrome prompts — required to monitor tabs and block restricted sites during the exam.'
      );
    }

    await sendMessage({ type: 'JOIN_SESSION', payload });
    await applySavedJoinFields();
    showMessage('Session joined — name, email, token, and interview ID are saved for next time.');
    await refreshState();
  } catch (error) {
    showMessage(error.message, true);
  }
});

document.getElementById('startSession').addEventListener('click', async () => {
  try {
    if (!(await ensureWebAccessForProctoring())) {
      throw new Error(
        'Allow access to all websites when Chrome prompts — required for tab monitoring and screenshots during the exam.'
      );
    }
    await sendMessage({ type: 'START_SESSION' });
    showMessage('Exam monitoring started.');
    await refreshState();
  } catch (error) {
    showMessage(error.message, true);
  }
});

document.getElementById('stopSession').addEventListener('click', async () => {
  try {
    await sendMessage({ type: 'END_EXAM' });
    showMessage('Exam monitoring stopped.');
    await refreshState();
  } catch (error) {
    showMessage(error.message, true);
  }
});

document.getElementById('saveSettings').addEventListener('click', async () => {
  try {
    await sendMessage({
      type: 'UPDATE_SETTINGS',
      settings: {
        clipboardBlocking: document.getElementById('clipboardBlocking').checked,
      },
    });
    showMessage('Settings saved.');
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

document.getElementById('refresh').addEventListener('click', () =>
  refreshState().catch((e) => showMessage(e.message, true))
);

const enableWebBtn = document.getElementById('enableWebMonitoring');
if (enableWebBtn) {
  enableWebBtn.addEventListener('click', async () => {
    try {
      const ok = await ensureWebAccessForProctoring();
      if (!ok) {
        showMessage('Permission was not granted. Click the button again and allow access in Chrome.', true);
        return;
      }
      showMessage('Full tab monitoring enabled.');
      await refreshState();
    } catch (e) {
      showMessage(e.message || String(e), true);
    }
  });
}

chrome.storage.onChanged.addListener((changes, area) => {
  if (area !== 'local') return;
  if (!changes.proctoringLastJoin && !changes.proctoringEvents && !changes.sessionActive) return;
  refreshState().catch((e) => showMessage(e.message, true));
});

function startLiveRefresh() {
  if (liveRefreshTimer) return;
  liveRefreshTimer = window.setInterval(() => {
    refreshState().catch((e) => showMessage(e.message, true));
  }, 4000);
}

window.addEventListener('beforeunload', () => {
  if (!liveRefreshTimer) return;
  clearInterval(liveRefreshTimer);
  liveRefreshTimer = null;
});

startLiveRefresh();
refreshState().catch((e) => showMessage(e.message, true));
