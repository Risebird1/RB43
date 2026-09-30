/**
 * DM V4 — code project question authoring.
 *
 * Brief -> GenerateCodeProjectQuestion (returns at once with status 'validating') -> poll
 * CodeProjectValidationStatus while the container job builds the project, runs the reference
 * solution and the starter against the hidden tests, and checks the time budget. Nothing is
 * served to a candidate until the status is 'passed'.
 */
(function ($) {
    'use strict';

    var PIPELINE = [
        { key: 'generate', title: 'Generate project' },
        { key: 'build', title: 'Build in container' },
        { key: 'reference', title: 'Reference solution' },
        { key: 'starter', title: 'Starter state' },
        { key: 'budget', title: 'Time budget' },
        { key: 'publish', title: 'Publish' }
    ];
    var POLL_MS = 3000;

    var $modal = null;
    var pollTimer = null;
    var generating = false;
    var currentQuestionId = null;
    var stackImages = [];
    var defaultTimeLimit = 15;

    function panel() { return $('#dmV4QuestionsPanel'); }
    function irfId() { return parseInt(panel().data('irf-id'), 10) || parseInt($('#id').val(), 10); }
    function requestData(extra) {
        var base = typeof window.dmV4GetQuestionsRequestData === 'function' ? window.dmV4GetQuestionsRequestData() : { irfId: irfId() };
        return $.extend({}, base, extra || {});
    }
    function rubricSkills() {
        var raw = panel().data('rubric-skills') || '';
        return raw ? String(raw).split('|||').map(function (s) { return $.trim(s); }).filter(Boolean) : [];
    }
    function esc(s) { return $('<div/>').text(s == null ? '' : String(s)).html(); }
    function reloadList() { if (typeof window.dmV4ReloadQuestions === 'function') window.dmV4ReloadQuestions(); }

    function build() {
        if ($modal) return $modal;
        var skills = rubricSkills().map(function (s) { return '<option value="' + esc(s) + '">' + esc(s) + '</option>'; }).join('');
        var html = '' +
            '<div class="dm-cp-modal" role="dialog" aria-modal="true" aria-labelledby="dmCpTitle" hidden>' +
            '<div class="dm-cp-dialog">' +
            '<div class="dm-cp-dialog-head"><h2 id="dmCpTitle">New code project question</h2>' +
            '<button type="button" class="dm-cp-close" aria-label="Close">&times;</button></div>' +
            '<div class="dm-cp-dialog-body">' +
            '<p class="dm-cp-sub">Describe the project in plain English. Risebird generates it, then proves it works before publishing.</p>' +
            '<div class="dm-cp-split">' +
            // Brief
            '<div class="dm-cp-panel dm-cp-brief"><div class="ph"><h3>Question brief</h3></div><div class="dm-cp-pb">' +
            '<div class="dm-cp-fld"><label for="dmCpBrief">What should the candidate build or fix?</label>' +
            '<textarea id="dmCpBrief" maxlength="4000" placeholder="e.g. A small Spring Boot inventory service. Persistence and routing are provided. The candidate implements validation: reject negative quantities, enforce unique SKU, return 404 with a problem-detail body for unknown ids."></textarea>' +
            '<div class="dm-cp-hint">Keep it to one focused defect or one small feature — the candidate has a short slot.</div></div>' +
            '<div class="dm-cp-row2">' +
            '<div class="dm-cp-fld"><label for="dmCpStack">Stack image</label><select id="dmCpStack"><option value="">Loading…</option></select></div>' +
            '<div class="dm-cp-fld"><label for="dmCpLevel">Difficulty</label><select id="dmCpLevel"><option>Medium</option><option>Easy</option><option>Hard</option></select></div>' +
            '</div>' +
            '<div class="dm-cp-fld"><label for="dmCpSkill">Rubric skill</label><select id="dmCpSkill"><option value="">Let AI choose from the rubric</option>' + skills + '</select></div>' +
            '<button type="button" class="dm-cp-btn wide" id="dmCpGenerate">Generate &amp; validate</button>' +
            '<div class="dm-cp-note"><b>Nothing publishes until it passes.</b> The generated project is built and test-run in a throwaway container first — a question that doesn\'t compile, or that ships already solved, never reaches a candidate.</div>' +
            '<div class="dm-cp-error" id="dmCpFormError" hidden></div>' +
            '</div></div>' +
            // Generation
            '<div class="dm-cp-panel"><div class="ph"><h3>Generation</h3><span class="r" id="dmCpState">Waiting</span></div><div class="dm-cp-pb">' +
            '<div class="dm-cp-idle" id="dmCpIdle"><div class="ic" aria-hidden="true">⌘</div><p>Write a brief and select a stack, then run generation. Generating takes a minute or two; validation a little longer.</p></div>' +
            '<div id="dmCpOut" hidden><ul class="dm-cp-pipe" id="dmCpPipe" aria-live="polite"></ul>' +
            '<div class="dm-cp-error" id="dmCpFailure" hidden></div>' +
            '<div id="dmCpResult" hidden style="margin-top:16px"></div>' +
            '<div class="dm-cp-actions" id="dmCpActions" hidden>' +
            '<button type="button" class="dm-cp-btn" id="dmCpDone">Done</button>' +
            '<button type="button" class="dm-cp-btn ghost" id="dmCpRegenerate">Edit brief &amp; regenerate</button>' +
            '</div></div>' +
            '</div></div>' +
            '</div></div></div></div>';
        $modal = $(html).appendTo(document.body);

        $modal.on('click', '.dm-cp-close, #dmCpDone', close);
        $modal.on('click', function (e) { if (e.target === $modal[0] && !generating) close(); });
        $modal.on('click', '#dmCpGenerate', generate);
        $modal.on('click', '#dmCpRegenerate', function () {
            stopPolling();
            currentQuestionId = null;
            $modal.find('.dm-cp-split').removeClass('is-status-only');
            $modal.find('#dmCpTitle').text('New code project question');
            resetOutput();
            $modal.find('#dmCpBrief').trigger('focus');
        });
        $(document).on('keydown.dmCp', function (e) { if (e.key === 'Escape' && $modal && !$modal.prop('hidden') && !generating) close(); });
        return $modal;
    }

    function loadStackImages() {
        var $sel = $modal.find('#dmCpStack');
        return $.post('/IRF/GetCodeProjectStackImages', requestData()).then(function (res) {
            stackImages = (res && res.success && res.images) || [];
            defaultTimeLimit = (res && res.timeLimitMinutes) || 15;
            if (!stackImages.length) {
                $sel.html('<option value="">No stack images available</option>').prop('disabled', true);
                $modal.find('#dmCpGenerate').prop('disabled', true);
                showFormError('No stack images are registered yet. A VM image has to be baked and added to StackImages before code project questions can be generated.');
                return;
            }
            $sel.prop('disabled', false).html(stackImages.map(function (i) {
                return '<option value="' + esc(i.code) + '">' + esc(i.code) + ' — ' + esc(i.toolchain || i.displayName) + '</option>';
            }).join(''));
            $modal.find('#dmCpGenerate').prop('disabled', false);
        }, function () {
            $sel.html('<option value="">Could not load</option>');
            showFormError('Could not load stack images. Close and try again.');
        });
    }

    function showFormError(msg, list) {
        var $e = $modal.find('#dmCpFormError');
        if (!msg) { $e.prop('hidden', true).empty(); return; }
        var html = esc(msg);
        if (list && list.length) html += '<ul>' + list.map(function (x) { return '<li>' + esc(x) + '</li>'; }).join('') + '</ul>';
        $e.html(html).prop('hidden', false);
    }

    function setState(text, cls) {
        var $s = $modal.find('#dmCpState');
        $s.empty();
        if (cls) $('<span class="dm-cp-status"></span>').addClass('dm-cp-status--' + cls).text(text).appendTo($s);
        else $s.text(text);
    }

    function resetOutput() {
        $modal.find('#dmCpIdle').prop('hidden', false);
        $modal.find('#dmCpOut, #dmCpResult, #dmCpActions, #dmCpFailure').prop('hidden', true);
        $modal.find('#dmCpResult, #dmCpFailure').empty();
        showFormError(null);
        setState('Waiting');
    }

    function renderPipeline(steps) {
        var byKey = {};
        (steps || []).forEach(function (s) { byKey[s.key] = s; });
        var html = PIPELINE.map(function (p) {
            var s = byKey[p.key] || { state: 'pending' };
            var mark = s.state === 'failed' ? '✕' : '✓';
            var secs = typeof s.seconds === 'number' ? s.seconds.toFixed(1) + 's' : '';
            return '<li class="' + esc(s.state === 'pending' ? '' : s.state) + '"><span class="dot" aria-hidden="true">' + mark + '</span>' +
                '<div><div class="t">' + esc(s.title || p.title) + '</div>' + (s.detail ? '<div class="d">' + esc(s.detail) + '</div>' : '') + '</div>' +
                '<span class="ms">' + esc(secs) + '</span></li>';
        }).join('');
        $modal.find('#dmCpPipe').html(html);
        $modal.find('#dmCpIdle').prop('hidden', true);
        $modal.find('#dmCpOut').prop('hidden', false);
    }

    function renderTree(files) {
        if (!files || !files.length) return '';
        var lines = files.slice().sort(function (a, b) { return a.path.localeCompare(b.path); }).map(function (f) {
            var cls = f.editable ? 'new' : (f.readOnly ? 'ro' : '');
            var tag = f.editable ? '   ← candidate edits' : (f.readOnly ? '   (read-only)' : '');
            return '<span class="' + cls + '">' + esc(f.path + tag) + '</span>';
        });
        return lines.join('\n');
    }

    function renderResult(q) {
        if (!q) return;
        var html = '';
        html += '<div class="dm-cp-gen"><h4>Problem statement</h4><p>' + esc(q.questionText) + '</p></div>';
        if (q.questionGist) html += '<div class="dm-cp-gen"><h4>Gist <span style="text-transform:none;letter-spacing:0;font-weight:500">— spoken by the AI avatar</span></h4><p>' + esc(q.questionGist) + '</p></div>';
        if (q.requirements && q.requirements.length) {
            html += '<div class="dm-cp-gen"><h4>Requirements</h4><ul>' + q.requirements.map(function (r) {
                return '<li><b>' + esc(r.id) + '</b> ' + esc(r.text) + '</li>';
            }).join('') + '</ul></div>';
        }
        var meta = [q.skillName, q.stackImageCode, q.estimatedMinutes ? ('est. ' + q.estimatedMinutes + ' of ' + (q.timeLimitMinutes || defaultTimeLimit) + ' min') : null].filter(Boolean).join(' · ');
        html += '<label style="display:block;font-size:12px;font-weight:600;color:#4A5568;margin-bottom:5px">Generated project <span style="font-weight:400;color:#7C8798">' + esc(meta) + '</span></label>';
        html += '<div class="dm-cp-tree">' + renderTree(q.files) + '</div>';
        $modal.find('#dmCpResult').html(html).prop('hidden', false);
    }

    function applyStatus(res) {
        if (!res || !res.success) return false;
        var steps = res.detail && res.detail.steps;
        renderPipeline(steps && steps.length ? steps : [{ key: 'generate', state: 'ok' }, { key: 'build', state: 'run' }]);
        renderResult(res.question);

        var status = res.status;
        if (status === 'passed') {
            setState('Published', 'passed');
            $modal.find('#dmCpActions').prop('hidden', false);
            return true;
        }
        if (status === 'failed') {
            setState('Failed', 'failed');
            var reason = (res.detail && res.detail.failureReason) || 'Validation failed.';
            $modal.find('#dmCpFailure').html('<b>Not published.</b> ' + esc(reason) + ' Edit the brief and regenerate.').prop('hidden', false);
            $modal.find('#dmCpActions').prop('hidden', false);
            return true;
        }
        setState('Validating', 'validating');
        return false;
    }

    function stopPolling() { if (pollTimer) { clearTimeout(pollTimer); pollTimer = null; } }

    function poll() {
        stopPolling();
        if (!currentQuestionId) return;
        $.ajax({ url: '/IRF/CodeProjectValidationStatus', type: 'GET', data: requestData({ questionId: currentQuestionId }), cache: false })
            .done(function (res) {
                if (applyStatus(res)) { reloadList(); return; }
                pollTimer = setTimeout(poll, POLL_MS);
            })
            .fail(function () { pollTimer = setTimeout(poll, POLL_MS * 2); });
    }

    function generate() {
        if (generating) return;
        var brief = $.trim($modal.find('#dmCpBrief').val());
        var stack = $modal.find('#dmCpStack').val();
        if (!brief) { showFormError('Describe what the candidate should build or fix.'); return; }
        if (!stack) { showFormError('Choose a stack image.'); return; }
        showFormError(null);

        generating = true;
        var $btn = $modal.find('#dmCpGenerate').prop('disabled', true).text('Generating…');
        $modal.find('#dmCpResult, #dmCpActions, #dmCpFailure').prop('hidden', true);
        renderPipeline([{ key: 'generate', state: 'run', detail: 'Writing the project, hidden tests and reference solution…' }]);
        setState('Running', 'validating');

        $.ajax({
            url: '/IRF/GenerateCodeProjectQuestion',
            type: 'POST',
            data: requestData({
                stackImageCode: stack,
                brief: brief,
                difficulty: $modal.find('#dmCpLevel').val(),
                skillName: $modal.find('#dmCpSkill').val() || null,
                timeLimitMinutes: defaultTimeLimit
            }),
            timeout: 600000
        }).done(function (res) {
            if (res && res.questionId) {
                currentQuestionId = res.questionId;
                reloadList();
                poll();
                return;
            }
            renderPipeline([{ key: 'generate', state: 'failed', detail: (res && res.message) || 'Generation failed.' }]);
            setState('Failed', 'failed');
            showFormError((res && res.message) || 'Generation failed.', res && res.errors);
        }).fail(function (xhr, textStatus) {
            var msg = textStatus === 'timeout' ? 'Generation timed out. Try a smaller brief.' : 'Server error (' + (xhr.status || 'unknown') + ').';
            renderPipeline([{ key: 'generate', state: 'failed', detail: msg }]);
            setState('Failed', 'failed');
            showFormError(msg);
        }).always(function () {
            generating = false;
            $btn.prop('disabled', false).text('Generate & validate');
        });
    }

    function open(questionId) {
        build();
        resetOutput();
        stopPolling();
        currentQuestionId = questionId || null;
        $modal.find('.dm-cp-split').toggleClass('is-status-only', !!questionId);
        $modal.find('#dmCpTitle').text(questionId ? 'Code project question' : 'New code project question');
        $modal.prop('hidden', false);
        $('body').css('overflow', 'hidden');
        loadStackImages();
        if (questionId) poll();
        else $modal.find('#dmCpBrief').trigger('focus');
    }

    function close() {
        if (generating) return;
        stopPolling();
        if ($modal) $modal.prop('hidden', true);
        $('body').css('overflow', '');
    }

    window.dmCodeProject = { open: open };

    $(document).off('click.dmCpNew', '#dmCpNewBtn').on('click.dmCpNew', '#dmCpNewBtn', function () {
        if ($(this).prop('disabled')) return;
        open(null);
    });
})(jQuery);
