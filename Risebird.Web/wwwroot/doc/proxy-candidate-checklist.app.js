(function () {
  const SECTIONS = [
    {
      id: 'pre-setup',
      icon: '🛡',
      title: 'Pre-Interview Setup',
      subtitle: 'Run these checks before the interview begins',
      items: [
        { label: "Verify LinkedIn photo against candidate's live video", note: 'Cross-reference profile photo with the face on camera during introduction. Zoom in if needed.', tags: ['critical'], isNew: true },
        { label: 'Ask candidate to show government-issued ID on camera', note: 'Compare name and photo to resume. Note any discrepancy immediately.', tags: ['critical'], isNew: true },
        { label: 'Request camera 360° pan of the interview environment', note: 'Look for additional screens, people, printed notes, or reference material in the room.', tags: ['new'], isNew: true },
        { label: 'Confirm full desktop share is active — not a tab or window', note: 'OS taskbar or dock should be fully visible in the shared screen. Tab-only sharing is not acceptable.', tags: ['warn', 'new'], isNew: true },
        { label: 'Ask candidate to disable virtual or blurred background', note: 'Real environment required. A blurred/virtual background conceals the physical setup.', tags: ['new'], isNew: true },
        { label: 'Run a latency / jitter check at session start', note: 'Note baseline connection quality. High latency may indicate VPN or remote session proxying.', tags: ['new'], isNew: true },
      ],
    },
    {
      id: 'screen-verify',
      icon: '🖥',
      title: 'Screen Verification',
      subtitle: "Full desktop share is your primary defence — here's how to use it effectively",
      items: [
        { label: 'Confirm full desktop share is active — OS taskbar/dock must be visible', note: 'Full screen share prevents candidates from freely switching to ChatGPT, Claude, or AI tools in other tabs or windows without it being visible to you. Tab-only sharing defeats this entirely.', tags: ['critical', 'new'], isNew: true },
        { label: "Expand the system tray and look for suspicious app icons", note: "The system tray (bottom-right on Windows) is where apps like AnyDesk, TeamViewer, and VNC hide. Ask the candidate to click the '^' arrow to reveal all hidden icons — an AnyDesk or remote-access icon here is an immediate red flag.", tags: ['critical', 'new'], isNew: true },
        { label: 'Ask candidate to open Task Manager (Win) or Activity Monitor (Mac)', note: 'Scroll through all running processes. Flag: AnyDesk, TeamViewer, VNC, remote desktop, screen-mirroring, or AI overlay apps.', tags: ['warn', 'new'], isNew: true },
        { label: 'Ask to scroll all open browser tabs before coding begins', note: 'Full screen share makes any tab switch visible — but confirm upfront. Look for open ChatGPT, Gemini, Copilot, Claude, or Stack Overflow tabs. Ask them to close any that are open.', tags: ['warn', 'new'], isNew: true },
        { label: 'Check for multiple monitors — drag window to screen edge', note: 'Ask to show Display Settings. A second connected display is a proxy enabler and remains hidden in a single-screen share.', tags: ['warn', 'new'], isNew: true },
        { label: 'Ask candidate to scroll the entire taskbar / dock', note: 'Minimized or hidden windows may contain reference material or a live coaching session.', tags: ['new'], isNew: true },
      ],
    },
    {
      id: 'ai-assistants',
      icon: '🤖',
      title: 'AI Interview Assistant Software',
      subtitle: 'Invisible AI tools that survive screen share — the hardest category to detect',
      items: [
        { label: 'Be aware: tools like Parteek AI, Interview Coder, and Final Round AI are invisible during screen share', note: "These apps run as a system overlay or separate process that does NOT appear in the shared screen. The candidate sees AI-generated answers floating on their screen while your view shows only the IDE or browser. Screen share alone will NOT catch these.", tags: ['critical'], isNew: true },
        { label: 'Watch for the candidate reading from a fixed point slightly above or beside the shared screen area', note: 'AI overlay tools position answer text just outside the shared window. Candidates reading from them have a tell: eyes fixed on one corner, not tracking the conversation.', tags: ['critical'], isNew: true },
        { label: 'Ask the candidate to move and resize the shared window to a different part of the screen', note: 'If an AI overlay is positioned relative to a fixed window location, asking them to move it may force them to reposition — causing a visible moment of hesitation or distraction.', tags: ['warn'], isNew: true },
        { label: 'Switch to interactive, conversational probing — AI tools cannot coach in real time for follow-ups', note: "Ask unexpected follow-up questions immediately after an answer: 'Walk me through your thought process step by step', 'What would happen if X changed?', 'Have you personally encountered this issue?' — AI-assisted answers break down quickly under unpredictable probing.", tags: ['critical'], isNew: true },
        { label: 'Vary your question style mid-interview — mix abstract, hypothetical, and deeply personal questions', note: 'AI tools are tuned for standard technical questions. Abstract reasoning, opinion-based questions, or personal experience questions expose the gap between coached and authentic knowledge.', tags: ['warn'], isNew: true },
        { label: 'Check screen share for any transparent/overlay windows using Alt+Tab or Mission Control', note: 'Ask the candidate to press Alt+Tab (Windows) or swipe up for Mission Control (Mac) so you can see all open windows including overlays that aren\'t visible in the normal desktop view.', tags: ['warn'], isNew: true },
      ],
    },
    {
      id: 'interactive-probing',
      icon: '🎯',
      title: 'Interactive Probing Techniques',
      subtitle: 'A session designed to expose reading, scripting, or AI assistance in real time',
      items: [
        { label: 'Never ask questions in a predictable sequence — break the pattern frequently', note: 'Any scripted or AI-assisted session relies on the interviewer following an expected flow. Randomly jump between topics, difficulty levels, and question types to break any pre-prepared flow.', tags: ['warn'], isNew: true },
        { label: 'Ask candidates to explain their answer in a completely different way', note: "'Can you explain that as if I have no technical background?' or 'Give me an analogy for that.' Genuine understanding produces flexible explanations. Coached answers collapse.", tags: ['warn'], isNew: true },
        { label: 'Insert a deliberate factual error and see if the candidate corrects it', note: "Say something slightly wrong about their answer and pause. A candidate reading from AI-generated text won't notice. Someone who genuinely knows the subject will correct you immediately.", tags: ['critical'], isNew: true },
        { label: 'Introduce a hypothetical that changes a core assumption in their answer', note: "'What if the dataset was 100x larger?' or 'What if you couldn't use that library?' — AI-assisted responses are brittle to dynamic changes in the problem scope.", tags: ['warn'], isNew: true },
        { label: 'Ask them to code live with narration — speak every step out loud as they type', note: 'Requiring simultaneous speaking and coding is cognitively demanding. Candidates relying on AI or a coach cannot multitask this way naturally. Gaps in narration while typing are significant.', tags: ['critical'], isNew: true },
        { label: 'Pause and ask an unrelated personal question mid-answer', note: "'Where were you based when you worked at [company]?' — An abrupt context switch mid-answer disrupts AI coaching flow.", tags: ['warn'], isNew: true },
      ],
    },
    {
      id: 'eye-head',
      icon: '👁',
      title: 'Eye & Head Movement',
      subtitle: 'Observe gaze patterns and head motion throughout',
      items: [
        { label: 'Candidate looking away from screen continuously during coding or Q&A', note: 'Indicates reading from an external source, phone, or second display placed off-camera.', tags: ['warn'] },
        { label: "Multiple screen reflections visible in the candidate's eyes", note: "Zoom into the candidate's face to check. Reflections that don't match the shared screen are a strong signal.", tags: ['critical'] },
        { label: 'Repetitive side-to-side head movement during answers', note: 'May indicate reading from a second monitor or notes placed to the side.', tags: ['warn'] },
        { label: 'Candidate asks to adjust camera repeatedly', note: 'Repositioning the camera mid-interview to hide something is suspicious.', tags: [] },
      ],
    },
    {
      id: 'lips-body',
      icon: '🎤',
      title: 'Lips & Body Language',
      subtitle: 'Watch for physical mismatches between voice and movement',
      items: [
        { label: 'Lip movement and voice are out of sync while speaking', note: 'Classic sign of audio being relayed through a separate device or splitter. Most visible when candidate is explaining something.', tags: ['critical'] },
        { label: 'Heavy beard or poor lighting concealing the lip area', note: 'Request better lighting or camera angle adjustment to see the full face.', tags: ['warn'] },
        { label: "Candidate's face cut off at the upper lip — lower face not visible", note: 'Intentionally hiding lower face to mask lip-sync mismatch. Ask them to adjust camera downward.', tags: ['warn'] },
        { label: 'Non-expressive or unusually flat body language throughout', note: 'Scripted or coached responses often result in robotic, low-affect delivery.', tags: [] },
        { label: 'Either excessive movement (distraction) or complete stillness (scripted)', note: 'Both extremes are suspicious. Natural conversation has moderate, spontaneous movement.', tags: ['warn'] },
      ],
    },
    {
      id: 'apps-devices',
      icon: '📱',
      title: 'Suspicious Applications & Devices',
      subtitle: 'Look for hardware and software that enables remote coaching',
      items: [
        { label: 'Screen mirroring or remote desktop app visible in background', note: 'AnyDesk, TeamViewer, Splashtop, VNC Viewer, Chrome Remote Desktop — any of these are red flags.', tags: ['critical'] },
        { label: 'Other suspicious devices connected — USB hubs, unknown peripherals', note: 'Visible in device manager or taskbar. Includes Bluetooth audio splitters and capture cards.', tags: ['warn'] },
        { label: 'Headphone placed in front of lips (not over ears)', note: 'The headphone microphone is used as a relay earpiece. The candidate listens through an earpiece hidden behind hair or collar.', tags: ['critical'] },
        { label: 'Signs of multiple devices being used simultaneously', note: "Watch for the candidate's eyes glancing at a second device, or momentary pauses while reading from a phone.", tags: ['warn'] },
        { label: 'Speaker or audio splitter setup', note: 'Allows answers to be relayed through a phone conference. Voice of a third party may be faintly audible.', tags: ['critical'] },
        { label: 'Unusual overhead or directional lighting on the face', note: 'Professional studio-style lighting during a casual interview can indicate a well-prepared impersonation setup.', tags: [] },
      ],
    },
    {
      id: 'audio-voice',
      icon: '🔊',
      title: 'Audio & Voice Signals',
      subtitle: 'Listen carefully for patterns that suggest coached or relayed answers',
      items: [
        { label: 'Voice tone and facial expressions are not in sync — flat affect', note: 'Monotone delivery when explaining something that should require active thought is unnatural.', tags: ['warn'] },
        { label: 'Background notifications, pings, or other voices audible', note: 'Muffled whispering, keyboard sounds in the background, or a second voice is a critical signal.', tags: ['critical'] },
        { label: 'Unusual pauses followed by abnormally precise, scripted-sounding answers', note: 'Short delay followed by perfectly articulated technical answers without any self-correction.', tags: ['warn'] },
        { label: 'Audio lags behind lip movement beyond normal network delay', note: 'Distinct from connection latency — this is deliberate audio routing through a relay.', tags: ['critical'] },
      ],
    },
    {
      id: 'behavior',
      icon: '💬',
      title: 'Behavior & Response Quality',
      subtitle: 'Probe for authentic knowledge versus coached or scripted answers',
      items: [
        { label: 'Non-satisfactory answers to personal resume questions', note: 'Ask about a specific project, team, or incident from their stated experience. Inability to recall details is suspicious.', tags: ['warn'] },
        { label: 'Answers are technically correct but lack personal context or narrative', note: 'Correct answers delivered without any authentic narration, hesitation, or self-correction.', tags: ['warn'] },
        { label: 'Sharp performance drop on follow-up or adaptive questions', note: "Scripted answers don't scale. Probe with unexpected follow-ups and note sudden deterioration.", tags: ['critical'] },
        { label: 'Cannot name colleagues, managers, or team structure from listed roles', note: "Ask casually: 'Who did you report to?' or 'What was your team size?' — a real candidate answers instantly.", tags: ['warn'] },
        { label: 'Inconsistency between verbal answers and written resume details', note: "Dates, company names, tech stacks, or role titles don't match what was submitted.", tags: ['critical'] },
      ],
    },
    {
      id: 'environment',
      icon: '🔉',
      title: 'Suspicious Environment',
      subtitle: 'Assess the physical space and session continuity',
      items: [
        { label: 'Inconsistent camera angles or setup across interview rounds', note: 'Different room, lighting, or background each time may indicate different people being interviewed.', tags: ['warn'] },
        { label: 'Abrupt disruptions — camera cuts out or repositions without explanation', note: 'Sudden loss of video or audio mid-answer, followed by resumption with no acknowledgment.', tags: ['critical'] },
        { label: 'Background sounds inconsistent with a home or office environment', note: 'Echo, ambient noise typical of a call center, or the sound of multiple simultaneous conversations.', tags: ['warn'] },
        { label: 'Environment changes mid-session — different background noise or lighting', note: 'A shift in environment mid-interview may indicate a handoff to a different person.', tags: ['critical'] },
      ],
    },
    {
      id: 'photo-identity',
      icon: '🪪',
      title: 'Photo & Identity Verification',
      subtitle: "Cross-reference the candidate's identity against available records",
      items: [
        { label: "LinkedIn profile photo does not match the person on video", note: 'Compare facial features carefully — bone structure, not just hair or skin tone.', tags: ['critical'] },
        { label: 'Profile was recently created or has fewer than 50 connections', note: 'A thin or newly created LinkedIn profile for someone claiming years of experience is suspicious.', tags: ['warn'] },
        { label: 'Resume photo (if provided) does not match video candidate', note: 'Some resumes include a headshot. Compare carefully.', tags: ['critical'] },
        { label: 'Candidate hesitates or refuses ID verification request', note: 'A legitimate candidate has no reason to refuse showing ID when asked politely.', tags: ['critical'] },
      ],
    },
  ];

  function saveSession() {
    localStorage.setItem(
      'proxySession',
      JSON.stringify({
        email: document.getElementById('interviewerEmail').value,
        candidate: document.getElementById('candidateName').value,
        role: document.getElementById('roleApplied').value,
      })
    );
  }

  function restoreSession() {
    try {
      var s = JSON.parse(localStorage.getItem('proxySession') || '{}');
      if (s.email) document.getElementById('interviewerEmail').value = s.email;
      if (s.candidate) document.getElementById('candidateName').value = s.candidate;
      if (s.role) document.getElementById('roleApplied').value = s.role;
    } catch (e) {}
  }

  function sendReport() {
    var email = document.getElementById('interviewerEmail').value.trim();
    var candidate = document.getElementById('candidateName').value.trim() || 'Unknown Candidate';
    var role = document.getElementById('roleApplied').value.trim() || 'Not specified';
    var date = new Date().toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
    var flagged = [];
    SECTIONS.forEach(function (sec) {
      sec.items.forEach(function (item, i) {
        var key = sec.id + '-' + i;
        if (state[key]) {
          var severity = item.tags.indexOf('critical') >= 0 ? '[CRITICAL]' : item.tags.indexOf('warn') >= 0 ? '[HIGH RISK]' : '[NOTED]';
          flagged.push(severity + ' ' + sec.title + ': ' + item.label);
        }
      });
    });
    var total = SECTIONS.reduce(function (a, s) {
      return a + s.items.length;
    }, 0);
    var checked = Object.keys(state).filter(function (k) {
      return state[k];
    }).length;
    var subject = encodeURIComponent('Proxy Detection Report — ' + candidate + ' — ' + date);
    var body = 'RISEBIRD PROXY DETECTION REPORT\n================================\n\n';
    body += 'Interviewer: ' + (email || 'Not specified') + '\n';
    body += 'Candidate:   ' + candidate + '\n';
    body += 'Role:        ' + role + '\n';
    body += 'Date:        ' + date + '\n';
    body += 'Checklist:   ' + checked + ' of ' + total + ' items reviewed\n\n';
    if (flagged.length > 0) {
      body += 'SIGNALS OBSERVED (' + flagged.length + ')\n-----------------------------\n';
      flagged.forEach(function (f) {
        body += '• ' + f + '\n';
      });
    } else {
      body += 'No signals flagged.\n';
    }
    body += '\n\nVerdict: ';
    var criticalCount = flagged.filter(function (f) {
      return f.indexOf('[CRITICAL]') === 0;
    }).length;
    if (criticalCount >= 2) body += 'STRONG PROXY INDICATORS — recommend rejection and escalation.\n';
    else if (criticalCount === 1 || flagged.length >= 3) body += 'SUSPICIOUS — recommend additional verification round.\n';
    else if (flagged.length > 0) body += 'MINOR FLAGS — monitor in follow-up rounds.\n';
    else body += 'No proxy signals detected.\n';
    body += '\n---\nGenerated by RiseBird Interviewer Checklist\nrisebird.io';
    var to = email || '';
    window.location.href = 'mailto:' + to + '?subject=' + subject + '&body=' + encodeURIComponent(body);
  }

  // State is always driven by the backend on load; start empty until the fetch resolves.
  var state = {};

  function save() {
    localStorage.setItem('proxyChecklist', JSON.stringify(state));
  }

  function toggle(key) {
    state[key] = !state[key];
    save();
    render();
  }

  function resetAll() {
    if (!confirm('Reset all checklist items?')) return;
    state = {};
    save();
    render();
  }

  function render() {
    var main = document.getElementById('main');
    var nav = document.getElementById('sectionNav');
    main.innerHTML = '';
    nav.innerHTML = '';
    var totalItems = 0;
    var totalChecked = 0;
    SECTIONS.forEach(function (sec) {
      var checkedCount = sec.items.filter(function (_, i) {
        return state[sec.id + '-' + i];
      }).length;
      var total = sec.items.length;
      totalItems += total;
      totalChecked += checkedCount;
      var btn = document.createElement('button');
      btn.type = 'button';
      btn.className = 'nav-btn';
      btn.textContent = sec.title;
      btn.onclick = function () {
        document.getElementById('sec-' + sec.id).scrollIntoView({ behavior: 'smooth', block: 'start' });
      };
      nav.appendChild(btn);
      var secEl = document.createElement('section');
      secEl.className = 'section';
      secEl.id = 'sec-' + sec.id;
      var fillPct = total > 0 ? Math.round((checkedCount / total) * 100) : 0;
      secEl.innerHTML =
        '<div class="section-header">' +
        '<div class="section-icon">' +
        sec.icon +
        '</div>' +
        '<div class="section-meta">' +
        '<div class="section-title">' +
        sec.title +
        '</div>' +
        '<div class="section-subtitle">' +
        sec.subtitle +
        '</div>' +
        '</div>' +
        '<div class="section-progress">' +
        '<div class="sec-bar-bg"><div class="sec-bar-fill" style="width:' +
        fillPct +
        '%"></div></div>' +
        '<div class="sec-count">' +
        checkedCount +
        '/' +
        total +
        '</div>' +
        '</div>' +
        '</div>' +
        '<div class="items" id="items-' +
        sec.id +
        '"></div>';
      main.appendChild(secEl);
      var itemsEl = secEl.querySelector('#items-' + sec.id);
      sec.items.forEach(function (item, i) {
        var key = sec.id + '-' + i;
        var checked = !!state[key];
        var classes = ['item'];
        if (item.isNew) classes.push('new-item');
        else if (item.tags.indexOf('warn') >= 0 || item.tags.indexOf('critical') >= 0) classes.push('warn');
        if (checked) classes.push('checked');
        var tagHTML = item.tags
          .map(function (t) {
            if (t === 'warn') return '<span class="tag tag-warn">High Risk</span>';
            if (t === 'critical') return '<span class="tag tag-critical">Critical</span>';
            return '';
          })
          .join('');
        if (item.isNew) tagHTML += '<span class="tag tag-new">New</span>';
        var el = document.createElement('div');
        el.className = classes.join(' ');
        el.setAttribute('role', 'checkbox');
        el.setAttribute('aria-checked', checked);
        el.innerHTML =
          '<div class="item-checkbox"><svg width="12" height="9" viewBox="0 0 12 9" fill="none"><path d="M1 4L4.5 7.5L11 1" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg></div>' +
          '<div class="item-body">' +
          '<div class="item-label">' +
          item.label +
          '</div>' +
          (item.note ? '<div class="item-note">' + item.note + '</div>' : '') +
          (tagHTML ? '<div class="item-tags">' + tagHTML + '</div>' : '') +
          '</div>';
        el.addEventListener('click', function () {
          toggle(key);
        });
        itemsEl.appendChild(el);
      });
    });
    var pct = totalItems > 0 ? Math.round((totalChecked / totalItems) * 100) : 0;
    var globalFill = document.getElementById('globalFill');
    if (globalFill) globalFill.style.width = pct + '%';
    var globalCount = document.getElementById('globalCount');
    if (globalCount) globalCount.textContent = totalChecked + ' / ' + totalItems;
    document.getElementById('allClear').classList.toggle('visible', totalChecked === totalItems && totalItems > 0);
  }

  function submitChecklistToBackend() {
    /** Same pattern as EditInterviewerProfile / dashboard: swal.fire("Successful"|"Failed", text, "success"|"error") */
    function getSwal() {
      if (typeof swal !== 'undefined' && typeof swal.fire === 'function') return swal;
      if (typeof Swal !== 'undefined' && typeof Swal.fire === 'function') return Swal;
      return null;
    }
    function fireSuccess(text) {
      var msg = text || 'Checklist saved successfully.';
      var s = getSwal();
      if (s) return s.fire('Successful', msg, 'success');
      alert(msg);
      return Promise.resolve();
    }
    function fireFailed(text) {
      var msg = text || 'Please try again.';
      var s = getSwal();
      if (s) return s.fire('Failed', msg, 'error');
      alert(msg);
      return Promise.resolve();
    }

    var url = window.__proxyChecklistSubmitUrl;
    if (!url) {
      fireFailed('Save URL is not configured.');
      return;
    }
    var tokenInput = document.querySelector('#proxyChecklistAntiForgery input[name="__RequestVerificationToken"]');
    if (!tokenInput || !tokenInput.value) {
      fireFailed('Security token missing. Refresh the page and try again.');
      return;
    }
    var btn = document.getElementById('btnSaveChecklist');
    var payload = JSON.stringify({
      InterviewerEmail: document.getElementById('interviewerEmail').value,
      CandidateName: document.getElementById('candidateName').value,
      RoleApplied: document.getElementById('roleApplied').value,
      ChecklistState: state,
    });
    var fd = new FormData();
    fd.append('__RequestVerificationToken', tokenInput.value);
    fd.append('payload', payload);
    if (btn) {
      btn.disabled = true;
      btn.style.opacity = '0.7';
    }

    fetch(url, { method: 'POST', body: fd, credentials: 'same-origin' })
      .then(function (r) {
        return r.text().then(function (t) {
          var data = {};
          try {
            data = t ? JSON.parse(t) : {};
          } catch (e) {
            throw new Error(
              'Unexpected response from server. Try refreshing the page or signing in again.'
            );
          }
          return data;
        });
      })
      .then(function (data) {
        if (data.success) {
          return fireSuccess(data.message);
        }
        return fireFailed(data.message || 'Something went wrong. Please try again.');
      })
      .catch(function (err) {
        var msg =
          err && err.message
            ? err.message
            : 'Could not reach the server. Check your connection and try again.';
        return fireFailed(msg);
      })
      .finally(function () {
        if (btn) {
          btn.disabled = false;
          btn.style.opacity = '';
        }
      });
  }

  window.submitChecklistToBackend = submitChecklistToBackend;

  function loadStateFromBackend() {
    var url = window.__proxyChecklistLoadUrl;
    if (!url) { render(); return; }

    fetch(url, { method: 'GET', credentials: 'same-origin' })
      .then(function (r) { return r.json(); })
      .then(function (data) {
        if (data && data.success && data.record && data.record.checklistState &&
            typeof data.record.checklistState === 'object') {
          // Backend has saved state — restore it
          state = data.record.checklistState;
        } else {
          // No saved state in DB — start with all checkboxes unchecked
          state = {};
        }
        save(); // keep localStorage in sync with whatever we resolved
        render();
      })
      .catch(function () {
        // Network or parse error — start clean (all unchecked)
        state = {};
        save();
        render();
      });
  }

  render();
  loadStateFromBackend();
})();
