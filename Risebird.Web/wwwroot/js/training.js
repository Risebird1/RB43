/* ── RISEBIRD TRAINING — SHARED HEADER + COMPLETION LOGIC ── */

const TRAINING_KEY = 'rb-training-progress-v1';

function getProgress() {
  try { return JSON.parse(localStorage.getItem(TRAINING_KEY) || '{}'); }
  catch { return {}; }
}
function saveProgress(state) {
  localStorage.setItem(TRAINING_KEY, JSON.stringify(state));
}
function markModuleComplete(moduleId, email) {
  const p = getProgress();
  p[moduleId] = { complete: true, email: email || '', at: new Date().toISOString() };
  saveProgress(p);
}
function isModuleComplete(moduleId) {
  return !!(getProgress()[moduleId] || {}).complete;
}

// Inject the shared topbar into any module page
function renderTopbar(opts = {}) {
  const { backHref = 'index.html', backLabel = 'All Modules', context = '' } = opts;
  const html = `
    <div class="topbar">
      <div class="topbar-left">
        <a href="${backHref}" style="text-decoration:none">
          <button class="topbar-back" type="button">← ${backLabel}</button>
        </a>
        <a href="${backHref}" style="text-decoration:none">
          <img id="rb-logo-top" alt="RiseBird" />
        </a>
      </div>
      ${context ? `<div class="topbar-context"><span class="topbar-context-dot"></span>${context}</div>` : ''}
    </div>
  `;
  document.body.insertAdjacentHTML('afterbegin', html);
}

// Inject the shared footer
function renderFooter() {
  const html = `
    <footer>
      <img id="rb-logo-footer" alt="RiseBird" />
      <div>RiseBird Interviewer Training · Confidential — for certified interviewers only</div>
    </footer>
  `;
  document.body.insertAdjacentHTML('beforeend', html);
}

// Wire any in-article checklist (toggles .checked on click + persists per-page)
function wireChecklist(scopeEl = document) {
  const items = scopeEl.querySelectorAll('.cl-item');
  if (!items.length) return;
  const pageKey = 'rb-cl-' + (location.pathname.split('/').pop() || 'page');
  const saved = JSON.parse(localStorage.getItem(pageKey) || '{}');
  items.forEach((el, i) => {
    if (!el.querySelector('.cl-box svg')) {
      const box = el.querySelector('.cl-box');
      if (box) box.innerHTML = '<svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg>';
    }
    if (saved[i]) el.classList.add('checked');
    el.addEventListener('click', () => {
      el.classList.toggle('checked');
      saved[i] = el.classList.contains('checked');
      localStorage.setItem(pageKey, JSON.stringify(saved));
    });
  });
}

// Wire the completion form (email + Mark Complete button)
function wireCompletion(moduleId, moduleTitle) {
  const form = document.querySelector('.complete-form');
  const status = document.querySelector('.complete-status');
  if (!form) return;
  const input = form.querySelector('input[type=email]');
  const btn = form.querySelector('button');

  // restore prior state
  const existing = getProgress()[moduleId];
  if (existing && existing.complete) {
    if (input) input.value = existing.email || '';
    if (status) {
      status.classList.add('done');
      status.textContent = `✓ Completed${existing.email ? ' by ' + existing.email : ''} · ${new Date(existing.at).toLocaleDateString()}`;
    }
    if (btn) { btn.textContent = 'Mark Complete Again'; }
  }

  btn.addEventListener('click', (e) => {
    e.preventDefault();
    const email = (input && input.value || '').trim();
    if (!email || !email.includes('@')) {
      if (status) {
        status.classList.remove('done');
        status.textContent = 'Please enter a valid email to log completion.';
      }
      input && input.focus();
      return;
    }
    markModuleComplete(moduleId, email);

    // open confirmation email
    const subject = `[RiseBird Training] Module complete — ${moduleTitle}`;
    const body = `Module: ${moduleTitle}\nModule ID: ${moduleId}\nInterviewer: ${email}\nCompleted at: ${new Date().toLocaleString()}\n\nI have read and acknowledged the training material in this module.\n\n— Sent from RiseBird Interviewer Training`;
    const mailto = `mailto:${email}?subject=${encodeURIComponent(subject)}&body=${encodeURIComponent(body)}`;
    window.location.href = mailto;

    if (status) {
      status.classList.add('done');
      status.textContent = `✓ Logged for ${email} · email opened — please send the confirmation`;
    }
    if (btn) btn.textContent = 'Mark Complete Again';
  });
}

// inject logo source
function setLogo(src) {
  document.querySelectorAll('#rb-logo-top, #rb-logo-hero, #rb-logo-footer').forEach(el => {
    if (el) el.src = src;
  });
}
