/**
 * DM V4 — sample questions (wireframe list + Synthesia regenerate/generate per question).
 */
(function ($) {
    'use strict';

    /** Forward-compatible whitelist — must stay aligned with DmSynthesiaVideoViewMode.cs */
    var SYNTHESIA_AI_INTERVIEW_TYPES = ['Preset', '2'];

    function getPanel() {
        return $('#dmV4QuestionsPanel');
    }

    function getIrfId() {
        return parseInt(getPanel().data('irf-id'), 10) || parseInt($('#id').val(), 10);
    }

    function usesSynthesia() {
        var panel = getPanel();
        if (panel.data('is-ai-preset') === '1' || panel.data('is-ai-preset') === 1) return true;
        var isAi = panel.data('is-ai-interview') === '1' || panel.data('is-ai-interview') === 1
            || panel.data('is-ai-interview') === true;
        var type = String(panel.data('ai-interview-type') || '').trim();
        if (!isAi || !type) return false;
        return SYNTHESIA_AI_INTERVIEW_TYPES.some(function (t) {
            return t.toLowerCase() === type.toLowerCase();
        });
    }

    function getRubricSkills() {
        var raw = getPanel().data('rubric-skills') || '';
        if (!raw) return [];
        return String(raw).split('|||').map(function (s) { return $.trim(s); }).filter(Boolean);
    }

    function escapeHtml(s) {
        return $('<div/>').text(s || '').html();
    }

    function skillLevelLabel(skillName) {
        var $opt = $('#SkillTable tbody tr.dm-v4-rubric-row').filter(function () {
            var v = $(this).find('[id$="__SkillName"]').val();
            return v && v.toLowerCase() === String(skillName).toLowerCase();
        }).first().find('[id$="__LevelId"] option:selected');
        return $opt.length ? $opt.text() : 'Proficient';
    }

    function levelChipClass(label) {
        return 'dm-v4-level-chip dm-v4-level-chip--' + String(label || 'proficient').toLowerCase().replace(/\s+/g, '');
    }

    function updateCountBadge(questions, rubricSkills) {
        var skills = rubricSkills.length ? rubricSkills : [];
        var withQ = 0;
        skills.forEach(function (sn) {
            if (questions.some(function (q) {
                return q.skillName && q.skillName.toLowerCase() === sn.toLowerCase() && q.comment && $.trim(q.comment);
            })) withQ += 1;
        });
        var total = Math.max(skills.length, 1);
        $('#dmV4QuestionsCount').text(withQ + ' of ' + total + ' skills covered');
    }

    function groupBySkill(questions, rubricSkills) {
        var map = {};
        var rubricLookup = {};
        (rubricSkills || []).forEach(function (sn) {
            rubricLookup[String(sn).toLowerCase()] = sn;
        });
        questions.forEach(function (q) {
            var raw = $.trim(q.skillName || q.SkillName || '') || 'General';
            var sn = rubricLookup[raw.toLowerCase()] || raw;
            if (!map[sn]) map[sn] = [];
            map[sn].push(q);
        });
        return map;
    }

    function openQuestionPopup(skillName) {
        var irfId = getIrfId();
        if (!irfId) {
            showQuestionToast('Could not find this skill channel.', 'error');
            return;
        }
        if (typeof window.OpenQuestionModal === 'function') {
            window.OpenQuestionModal(irfId, skillName || null);
            return;
        }
        openQuestionsModal(irfId, skillName || null);
    }

    function openQuestionsModal(irfId, presetSkill) {
        var $modal = $('#PopupDetailsModal');
        if (!$modal.length) {
            showQuestionToast('Question popup is missing on this page.', 'error');
            return;
        }
        $('#changememberHeading').text('Questions');
        $('#PopupDetailsModal #divpopupModel').html('<div style="padding:24px;text-align:center;">Loading questions…</div>');
        $modal.css({ display: 'block', zIndex: 10070 });

        $.ajax({
            url: '/IRF/SkillChannelSampleQuestions',
            data: { id: irfId },
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        }).done(function (data) {
            $('#PopupDetailsModal #divpopupModel').html(data);
            if (presetSkill) {
                var $skillSelect = $('#PopupDetailsModal').find('select[id$="__SkillName"]').first();
                if ($skillSelect.length) {
                    $skillSelect.val(presetSkill);
                    if (!$skillSelect.val()) {
                        $skillSelect.find('option').filter(function () {
                            return $(this).text().trim().toLowerCase() === String(presetSkill).trim().toLowerCase();
                        }).first().prop('selected', true);
                    }
                }
            }
        }).fail(function (xhr) {
            $('#PopupDetailsModal #divpopupModel').html(
                '<p class="text-danger" style="padding:20px;">Could not load the question form (' +
                (xhr.status || 'error') + ').</p>'
            );
        });
    }

    function showQuestionToast(message, type) {
        if (typeof swal !== 'undefined') {
            swal.fire({
                toast: true,
                position: 'top-end',
                showConfirmButton: false,
                // Failures need long enough to actually be read.
                timer: (type === 'error') ? 9000 : 4500,
                type: type || 'error',
                title: message,
                // Must be a string: the bundled SweetAlert2 (v8) splits this value and calls
                // forEach on it, so the object form ({popup, title}) introduced in v9 throws
                // "t.forEach is not a function" and no toast appears at all. The title is styled
                // via ".dm-v4-toast-popup .swal2-title" instead.
                customClass: 'dm-v4-toast-popup'
            });
        } else {
            alert(message);
        }
    }

    function aiVideoButtonLabel(hasVideo) {
        return hasVideo ? 'Regenerate AI video' : 'Generate AI video';
    }

    function renderQuestions(questions) {
        var $host = $('#dmV4QuestionsSkills');
        var rubricSkills = getRubricSkills();
        var synthesia = usesSynthesia();
        updateCountBadge(questions, rubricSkills);

        var grouped = groupBySkill(questions, rubricSkills);
        var skillsToShow = rubricSkills.length ? rubricSkills.slice() : Object.keys(grouped).sort();
        Object.keys(grouped).forEach(function (sn) {
            if (skillsToShow.indexOf(sn) === -1) skillsToShow.push(sn);
        });

        if (!skillsToShow.length && !questions.length) {
            $host.html('<p class="dm-v4-questions-empty">No sample questions yet. Use <strong>Generate with AI</strong> or <strong>Add question</strong> on a rubric skill.</p>');
            return;
        }

        if (!skillsToShow.length) {
            skillsToShow = Object.keys(grouped).sort();
        }

        var html = '';
        skillsToShow.forEach(function (skillName) {
            var qs = grouped[skillName] || [];
            var lvl = skillLevelLabel(skillName);
            html += '<div class="dm-v4-questions-skill-card" data-skill="' + escapeHtml(skillName) + '">';
            html += '<div class="dm-v4-questions-skill-head">';
            html += '<div class="dm-v4-questions-skill-head__left">';
            html += '<span class="' + levelChipClass(lvl) + '">' + escapeHtml(lvl) + '</span>';
            html += '<span class="dm-v4-questions-skill-name">' + escapeHtml(skillName) + '</span>';
            html += '<span class="dm-v4-badge dm-v4-badge--neutral dm-v4-badge--xs">' + qs.length + ' q</span>';
            html += '</div>';
            html += '<button type="button" class="dm-v4-btn dm-v4-btn--ghost dm-v4-btn--sm dm-v4-add-question-btn" data-skill="' + escapeHtml(skillName) + '">';
            html += '<i class="fa fa-plus"></i> Add question</button>';
            html += '</div><div class="dm-v4-questions-skill-body">';

            if (!qs.length) {
                html += '<p class="dm-v4-questions-skill-empty">No questions for this skill yet.</p>';
            } else {
                qs.forEach(function (q, qi) {
                    var qid = q.id;
                    var comment = q.comment || q.Comment || '';
                    var idealAnswer = q.idealAnswer || q.IdealAnswer || '';
                    var expected = q.expectedAnswer || q.ExpectedAnswer || idealAnswer || '';
                    var video = q.aiQuestionVideoLink || q.AIQuestionVideoLink || '';
                    var hasComment = !!$.trim(comment);
                    var hasVideo = !!$.trim(video);
                    var isCoding = q.isCodingQuestion || q.IsCodingQuestion;
                    var codingTimeLimit = q.codingTimeLimitInMinutes || q.CodingTimeLimitInMinutes;
                    var gist = q.questionGist || q.QuestionGist || '';
                    var isListening = q.isListeningComprehension || q.IsListeningComprehension;
                    var listeningAudio = q.listeningAudioUrl || q.ListeningAudioUrl || '';
                    var listeningTranscript = q.listeningAudioTranscription || q.ListeningAudioTranscription || '';
                    var isAiQuestion = q.isAIGenerated || q.IsAIGenerated || q.isGeneratedByAi || q.IsGeneratedByAi;
                    var isCodeProject = q.isCodeProjectQuestion || q.IsCodeProjectQuestion;
                    var projectStatus = isCodeProject ? (codeProjectStatuses[qid] || 'pending') : null;
                    // AI-generated rows often have no saved gist yet; the avatar would speak the question.
                    // Never fall back for a listening comprehension question: its text describes what
                    // the candidate is meant to listen for, so narrating it would give the exercise
                    // away. Pre-filling here would also satisfy the gist check and silently save the
                    // question text as the gist.
                    if (!$.trim(gist) && !isCoding && !isListening && $.trim(comment)) {
                        gist = comment;
                    }

                    html += '<div class="dm-v4-question-row" data-question-id="' + qid + '">';
                    html += '<div class="dm-v4-question-col">';
                    html += '<div class="dm-v4-question-label">Question Q' + (qi + 1);
                    if (isAiQuestion) {
                        html += ' <span class="dm-v4-badge dm-v4-badge--ai">AI</span>';
                    }
                    if (isCodeProject) {
                        html += ' <span class="dm-v4-badge dm-v4-badge--neutral dm-v4-badge--xs">Code project</span>';
                        html += ' <span class="dm-cp-status dm-cp-status--' + escapeHtml(projectStatus) + '">' + escapeHtml(codeProjectStatusLabel(projectStatus)) + '</span>';
                    }
                    html += '</div>';
                    html += '<div class="dm-v4-question-text">' + (hasComment ? escapeHtml(comment) : '<span class="dm-v4-question-text--placeholder">Add question…</span>') + '</div>';
                    if ($.trim(idealAnswer)) {
                        html += '<details class="dm-v4-ideal-answer"><summary>Ideal Answer (AI)</summary><div class="dm-v4-ideal-answer__body">' + escapeHtml(idealAnswer) + '</div></details>';
                    }

                    html += '<div class="dm-v4-question-gist">';
                    html += '<div class="dm-v4-question-label">Question gist <span class="dm-v4-question-gist-hint">spoken by the AI avatar</span></div>';
                    if (isCodeProject) {
                        // Generated with the project and validated with it; change it by regenerating.
                        html += '<div class="dm-v4-question-text">' + escapeHtml(gist) + '</div>';
                    } else {
                    html += '<textarea class="dm-v4-coding-gist" rows="2" data-id="' + qid + '"';
                    html += ' placeholder="What the AI avatar should speak. Required for coding questions.">' + escapeHtml(gist) + '</textarea>';
                    html += '<div class="dm-v4-question-gist-warning" hidden>Write the gist to finish enabling coding mode.</div>';
                    }
                    html += '</div>';

                    // A code project question opens a machine, not the editor: it must never be
                    // toggled into a coding or listening question, so those controls are replaced
                    // by a link to its validation pipeline.
                    if (isCodeProject) {
                        html += '<div class="dm-v4-question-meta">';
                        html += '<button type="button" class="dm-v4-btn dm-v4-btn--ghost dm-v4-btn--xs dm-cp-view-btn" data-id="' + qid + '">View project &amp; validation</button>';
                        if (projectStatus !== 'passed') {
                            html += '<span class="dm-v4-question-gist-hint">Not served to candidates until validation passes.</span>';
                        }
                        html += '</div>';
                    } else {
                    // Coding mode is editable in place so questions added earlier, or generated from
                    // the rubric, can be turned into coding questions without being retyped.
                    html += '<div class="dm-v4-question-meta dm-v4-question-coding">';
                    html += '<label class="dm-v4-question-flag"><input type="checkbox" class="dm-v4-coding-cb" data-id="' + qid + '"' + (isCoding ? ' checked' : '') + (qid ? '' : ' disabled') + ' /> Coding question</label>';
                    html += '<input type="number" min="1" max="180" step="1" class="dm-v4-coding-mins" data-id="' + qid + '"';
                    html += ' value="' + (codingTimeLimit ? escapeHtml(String(codingTimeLimit)) : '15') + '"';
                    html += ' title="Time limit in minutes"' + (isCoding ? '' : ' disabled') + ' />';
                    html += '<span class="dm-v4-coding-mins-label"' + (isCoding ? '' : ' style="opacity:.5"') + '>min</span>';
                    html += '</div>';

                    // Listening comprehension: the candidate hears the clip (max twice) and types a
                    // summary within three minutes. Needs the clip, a transcript to score against,
                    // and a gist for the avatar to introduce the exercise with.
                    html += '<div class="dm-v4-question-meta dm-v4-question-listening">';
                    html += '<label class="dm-v4-question-flag"><input type="checkbox" class="dm-v4-listening-cb" data-id="' + qid + '"' + (isListening ? ' checked' : '') + (qid ? '' : ' disabled') + ' /> Listening comprehension</label>';
                    html += '<div class="dm-v4-listening-body"' + (isListening ? '' : ' hidden') + '>';
                    html += '<div class="dm-v4-listening-audio-row">';
                    html += '<input type="file" class="dm-v4-listening-file" data-id="' + qid + '" accept="audio/mpeg,audio/wav,audio/mp4,audio/ogg,audio/aac,.mp3,.wav,.m4a,.ogg,.aac"' + (qid ? '' : ' disabled') + ' />';
                    html += '<button type="button" class="dm-v4-btn dm-v4-btn--ghost dm-v4-btn--xs dm-v4-listening-upload-btn" data-id="' + qid + '"' + (qid ? '' : ' disabled') + '>Upload audio</button>';
                    html += '<a href="' + escapeHtml(listeningAudio) + '" target="_blank" rel="noopener" class="dm-v4-question-link dm-v4-listening-audio-link"' + (listeningAudio ? '' : ' hidden') + '>Audio ↗</a>';
                    html += '</div>';
                    html += '<div class="dm-v4-question-label">Audio transcript <span class="dm-v4-question-gist-hint">scored against the candidate\'s summary; never shown to them</span></div>';
                    html += '<textarea class="dm-v4-listening-transcript" rows="2" data-id="' + qid + '"';
                    html += ' placeholder="Exact transcript of the audio clip">' + escapeHtml(listeningTranscript) + '</textarea>';

                    // A question flagged directly in the database (before this panel existed) can be
                    // missing pieces. Say so on load rather than only when the checkbox is toggled.
                    var listeningGaps = [];
                    if (isListening) {
                        if (!listeningAudio) listeningGaps.push('audio clip');
                        if (!$.trim(listeningTranscript)) listeningGaps.push('transcript');
                        if (!$.trim(gist)) listeningGaps.push('question gist');
                    }
                    if (listeningGaps.length) {
                        html += '<div class="dm-v4-listening-warning">Incomplete — still needs the ' + listeningGaps.join(', ') + '.</div>';
                    } else {
                        html += '<div class="dm-v4-listening-warning" hidden></div>';
                    }
                    html += '</div>';
                    html += '</div>';
                    }

                    html += '</div><div class="dm-v4-question-col">';
                    html += '<div class="dm-v4-question-label">Expected answer</div>';
                    html += '<div class="dm-v4-question-text' + (expected ? '' : ' dm-v4-question-text--muted') + '">';
                    html += expected ? escapeHtml(expected) : 'Add expected answer…';
                    html += '</div>';

                    if (synthesia) {
                        html += '<div class="dm-v4-question-meta">';
                        if (hasVideo) {
                            html += '<a href="' + escapeHtml(video) + '" target="_blank" rel="noopener" class="dm-v4-question-link dm-v4-question-video-link">AI video ↗</a>';
                        }
                        var btnLabel = aiVideoButtonLabel(hasVideo);
                        // A coding question is narrated from its gist, so that is what must exist
                        var needsGist = (isCoding || isCodeProject) && !$.trim(gist);
                        var btnDisabled = !qid || !hasComment || needsGist;
                        html += '<button type="button" class="dm-v4-btn dm-v4-btn--ghost dm-v4-btn--xs dm-v4-regen-ai-video-btn"';
                        html += ' data-id="' + qid + '" data-has-video="' + (hasVideo ? '1' : '0') + '"';
                        if (btnDisabled) {
                            html += needsGist
                                ? ' disabled title="Write the question gist before generating video"'
                                : ' disabled title="Add question text before generating video"';
                        }
                        html += '>' + btnLabel + '</button>';

                        var flagged = q.isFlaggedForSentimentAnalysis || q.IsFlaggedForSentimentAnalysis;
                        html += '<label class="dm-v4-question-flag"><input type="checkbox" class="flag-checkbox dm-v4-flag-cb" data-id="' + qid + '"' + (flagged ? ' checked' : '') + ' /> Flag for sentiment</label>';
                        html += '</div>';
                    }

                    html += '</div>';
                    html += '<button type="button" class="dm-v4-icon-btn dm-v4-icon-btn--danger dm-v4-delete-question-btn" data-id="' + qid + '" title="Remove"><i class="fa fa-trash"></i></button>';
                    html += '</div>';
                });
            }

            html += '</div></div>';
        });

        var covered = rubricSkills.filter(function (sn) {
            return grouped[sn] && grouped[sn].some(function (q) { return q.comment || q.Comment; });
        }).length;
        var missing = rubricSkills.length - covered;
        if (missing > 0) {
            html += '<div class="dm-v4-questions-more-hint">' + missing + ' more ' + (missing === 1 ? 'skill doesn\'t' : 'skills don\'t') + ' have sample questions yet. Use <strong>Generate with AI</strong> above or add manually per skill.</div>';
        }

        $host.html(html);
    }

    function getQuestionsRequestData() {
        var data = { irfId: getIrfId() };
        if (window.dmV4ClientShareView === true) {
            data.Type = (window.dmV4ClientShareType || 'ublc').toString();
        }
        return data;
    }

    /** questionId -> validation status for code project questions. */
    var codeProjectStatuses = {};

    function codeProjectStatusLabel(status) {
        switch (status) {
            case 'passed': return 'Validated';
            case 'failed': return 'Validation failed';
            case 'validating': return 'Validating…';
            default: return 'Pending';
        }
    }

    function loadCodeProjectStatuses() {
        return $.post('/IRF/GetCodeProjectStatuses', getQuestionsRequestData()).then(function (res) {
            codeProjectStatuses = {};
            ((res && res.statuses) || []).forEach(function (s) { codeProjectStatuses[s.questionId] = s.status; });
        }, function () { codeProjectStatuses = {}; return $.Deferred().resolve(); });
    }

    function loadQuestions() {
        var statuses = loadCodeProjectStatuses();
        return $.post('/IRF/GetDeliveryHeadSampleQuestionsJson', getQuestionsRequestData()).then(function (res) {
            return statuses.then(function () { return res; }, function () { return res; });
        }).then(function (res) {
            if (res && res.success) {
                renderQuestions(res.questions || []);
                // Rendering replaces the buttons, so any render still in flight — including one
                // started before a page reload — has to be picked back up here.
                resumePendingAiVideos();
            }
        });
    }

    // A question already dealt into a question set cannot always be removed, and removing one that
    // was dealt but never used discards the channel's sets. Both outcomes have to reach the user,
    // so the response is reported rather than reloading the list regardless.
    function reportDeleteOutcome(res) {
        loadQuestions();

        var message = (res && res.message) ? res.message : null;
        var refused = !(res && res.success);

        if (!message && !refused) return;

        if (typeof swal === 'undefined') {
            if (message) alert(message);
            return;
        }

        if (refused) {
            swal.fire({ title: 'Not removed', html: message || 'The question could not be removed.', type: 'error' });
        }
        else if (res.setsDiscarded > 0) {
            swal.fire({ title: 'Question sets discarded', html: message, type: 'warning' });
        }
    }

    function deleteQuestion(id) {
        if (typeof swal === 'undefined') {
            if (confirm('Remove this question?')) {
                $.post('/IRF/DeleteSkillChannelSampleQuestions', { questionid: id }, reportDeleteOutcome);
            }
            return;
        }

        swal.fire({
            title: 'Are you sure?',
            text: 'Remove this question from the skill channel?',
            type: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Yes, remove'
        }).then(function (result) {
            if (result && result.value) {
                $.post('/IRF/DeleteSkillChannelSampleQuestions', { questionid: id }, reportDeleteOutcome);
            }
        });
    }

    // A Synthesia render takes minutes, and how many is not predictable. So the server only starts
    // the render and we poll it here: the button stays in its spinner with the elapsed time on it
    // until the video actually lands, instead of turning red while the render is still running and
    // telling the user something failed that hadn't.
    var AI_VIDEO_POLL_MS = 10000;
    var AI_VIDEO_MAX_WAIT_MS = 20 * 60 * 1000;
    var AI_VIDEO_MAX_CONSECUTIVE_NETWORK_ERRORS = 6;
    var AI_VIDEO_STORE_KEY = 'dmV4PendingAiVideos';

    /** qid -> true while a poll loop owns that question, so nothing polls it twice. */
    var activeAiVideoPolls = {};
    var aiVideoTicker = null;

    // Kept in localStorage so a reload mid-render resumes the wait. Without it the render finishes
    // at Synthesia but nothing ever collects the video, and the question keeps no link.
    function readPendingAiVideos() {
        try {
            var raw = window.localStorage.getItem(AI_VIDEO_STORE_KEY);
            var parsed = raw ? JSON.parse(raw) : null;
            return (parsed && typeof parsed === 'object') ? parsed : {};
        } catch (e) {
            return {};
        }
    }

    function writePendingAiVideos(map) {
        try {
            window.localStorage.setItem(AI_VIDEO_STORE_KEY, JSON.stringify(map || {}));
        } catch (e) { /* private mode / blocked storage: polling still works for this page view */ }
    }

    function rememberPendingAiVideo(qid, videoId, startedAt) {
        var map = readPendingAiVideos();
        map[String(qid)] = { videoId: videoId, startedAt: startedAt };
        writePendingAiVideos(map);
    }

    function forgetPendingAiVideo(qid) {
        var map = readPendingAiVideos();
        if (map.hasOwnProperty(String(qid))) {
            delete map[String(qid)];
            writePendingAiVideos(map);
        }
    }

    function findAiVideoButton(qid) {
        return $('.dm-v4-regen-ai-video-btn').filter(function () {
            return String($(this).data('id')) === String(qid);
        }).first();
    }

    function formatElapsed(ms) {
        var total = Math.max(0, Math.round(ms / 1000));
        var mins = Math.floor(total / 60);
        var secs = total % 60;
        return mins ? (mins + 'm ' + secs + 's') : (secs + 's');
    }

    function paintAiVideoPending($btn, startedAt) {
        if (!$btn || !$btn.length) return;
        var elapsed = formatElapsed(Date.now() - startedAt);

        // Only the counter is rewritten once the pending markup is in place — replacing the whole
        // button every second would restart the spinner's animation on each tick.
        var $elapsed = $btn.find('.dm-v4-ai-video-elapsed');
        if ($elapsed.length) {
            $elapsed.text(elapsed);
            return;
        }

        $btn.prop('disabled', true).removeClass('is-success is-error').addClass('is-pending')
            .attr('title', 'Synthesia is rendering this video. You can leave this page — generation continues.')
            .html('<i class="fa fa-spinner fa-spin"></i> Generating… <span class="dm-v4-ai-video-elapsed">' +
                escapeHtml(elapsed) + '</span>');
    }

    // One shared ticker keeps every pending button's elapsed time moving between polls, so the
    // button visibly stays alive rather than looking stuck for ten seconds at a time.
    function ensureAiVideoTicker() {
        if (aiVideoTicker) return;
        aiVideoTicker = setInterval(function () {
            var qids = Object.keys(activeAiVideoPolls);
            if (!qids.length) {
                clearInterval(aiVideoTicker);
                aiVideoTicker = null;
                return;
            }
            qids.forEach(function (qid) {
                var entry = activeAiVideoPolls[qid];
                if (!entry) return;
                var $btn = findAiVideoButton(qid);
                if ($btn.length) paintAiVideoPending($btn, entry.startedAt);
            });
        }, 1000);
    }

    function endAiVideoPoll(qid) {
        delete activeAiVideoPolls[String(qid)];
        forgetPendingAiVideo(qid);
    }

    // The toast disappears after a few seconds, which is no use for diagnosing a failure that
    // happened while nobody was watching — so the reason also stays on the button as its tooltip
    // and goes to the console.
    function reportAiVideoFailure($btn, message, detail) {
        if (window.console && console.error) {
            console.error('[AI video] ' + message, detail || '');
        }
        if ($btn && $btn.length) {
            $btn.attr('title', message);
        }
        showQuestionToast(message, 'error');
    }

    function resetAiVideoButton($btn) {
        if (!$btn || !$btn.length) return;
        var hadVideo = $btn.data('has-video') === 1 || $btn.data('has-video') === '1';
        $btn.prop('disabled', false).removeClass('is-success is-pending').addClass('is-error')
            .html(aiVideoButtonLabel(hadVideo));
    }

    function failAiVideoPoll(qid, message, detail) {
        endAiVideoPoll(qid);
        var $btn = findAiVideoButton(qid);
        resetAiVideoButton($btn);
        reportAiVideoFailure($btn, message, detail);
    }

    function completeAiVideoPoll(qid, res) {
        endAiVideoPoll(qid);
        var $btn = findAiVideoButton(qid);

        if ($btn.length) {
            // Scoped to the button's own meta row: a question row has several .dm-v4-question-meta
            // blocks (coding, listening, video), and an unscoped prepend lands in all of them.
            var $meta = $btn.closest('.dm-v4-question-meta');
            var $link = $meta.find('.dm-v4-question-video-link');
            if ($link.length) {
                $link.attr('href', res.videoUrl);
            } else {
                $meta.prepend('<a href="' + escapeHtml(res.videoUrl) + '" target="_blank" rel="noopener" class="dm-v4-question-link dm-v4-question-video-link">AI video ↗</a> ');
            }
            $btn.data('has-video', '1').removeClass('is-error is-pending').addClass('is-success')
                .removeAttr('title')
                .html('<i class="fa fa-check"></i> Generated');
            setTimeout(function () {
                var $current = findAiVideoButton(qid);
                if ($current.length) {
                    $current.prop('disabled', false).removeClass('is-success')
                        .html(aiVideoButtonLabel(true));
                }
            }, 2200);
        }

        showQuestionToast(res.message || 'AI video ready.', 'success');
    }

    function startAiVideoPolling(qid, videoId, startedAt) {
        qid = String(qid);
        if (activeAiVideoPolls[qid]) return;
        // Held in memory as well as in storage so the elapsed counter keeps running even where
        // localStorage is unavailable.
        activeAiVideoPolls[qid] = { videoId: videoId, startedAt: startedAt };

        var consecutiveNetworkErrors = 0;
        var lastStatus = null;
        paintAiVideoPending(findAiVideoButton(qid), startedAt);
        ensureAiVideoTicker();

        function scheduleNext() {
            setTimeout(poll, AI_VIDEO_POLL_MS);
        }

        function poll() {
            if (!activeAiVideoPolls[qid]) return;

            if (Date.now() - startedAt > AI_VIDEO_MAX_WAIT_MS) {
                failAiVideoPoll(qid, 'Still not ready after ' + formatElapsed(AI_VIDEO_MAX_WAIT_MS) +
                    '. Synthesia may be stuck on this render — please try again.');
                return;
            }

            $.ajax({
                url: '/AIInterview/GetAIQuestionVideoStatus',
                type: 'POST',
                data: { id: parseInt(qid, 10), videoId: videoId },
                timeout: 360000
            }).done(function (res) {
                if (!activeAiVideoPolls[qid]) return;
                consecutiveNetworkErrors = 0;

                // A signed-out session answers a POST with the login page, not JSON. That is not a
                // failed render, so say what it actually is instead of blaming the generation.
                if (typeof res !== 'object' || res === null) {
                    failAiVideoPoll(qid, 'Your session looks to have expired — sign in again, then reopen this page to pick the video up.', res);
                    return;
                }

                if (res.success && res.ready && res.videoUrl) {
                    completeAiVideoPoll(qid, res);
                    return;
                }
                if (res.success) {
                    // Logged on change so the real Synthesia status values are visible while a
                    // render is in flight, rather than being guessed at.
                    if (res.status && res.status !== lastStatus) {
                        lastStatus = res.status;
                        if (window.console && console.log) {
                            console.log('[AI video] Synthesia status for question ' + qid + ': ' + res.status);
                        }
                    }
                    scheduleNext();
                    return;
                }
                failAiVideoPoll(qid, res.message || 'Video generation failed.', res);
            }).fail(function (xhr, status) {
                if (!activeAiVideoPolls[qid]) return;

                // A dropped or timed-out poll says nothing about the render, which is running at
                // Synthesia either way — so keep waiting unless the server stays unreachable.
                consecutiveNetworkErrors += 1;
                if (consecutiveNetworkErrors >= AI_VIDEO_MAX_CONSECUTIVE_NETWORK_ERRORS) {
                    failAiVideoPoll(qid, status === 'timeout'
                        ? 'Lost contact with the server while saving the video. Reload to check whether it arrived.'
                        : 'Could not reach the server (' + (xhr.status || 'unknown') + ').');
                    return;
                }
                scheduleNext();
            });
        }

        poll();
    }

    /** Re-attaches spinners and poll loops after the question list is (re)rendered or reloaded. */
    function resumePendingAiVideos() {
        var pending = readPendingAiVideos();
        Object.keys(pending).forEach(function (qid) {
            var entry = pending[qid] || {};
            if (!entry.videoId || !entry.startedAt) {
                forgetPendingAiVideo(qid);
                return;
            }
            if (Date.now() - entry.startedAt > AI_VIDEO_MAX_WAIT_MS) {
                forgetPendingAiVideo(qid);
                return;
            }
            if (!findAiVideoButton(qid).length) return;
            startAiVideoPolling(qid, entry.videoId, entry.startedAt);
        });
    }

    function regenerateAiVideo($btn) {
        var qid = parseInt($btn.data('id'), 10);
        if (!qid || $btn.prop('disabled')) return;
        if (activeAiVideoPolls[String(qid)]) return;

        var startedAt = Date.now();
        paintAiVideoPending($btn, startedAt);

        $.ajax({
            url: '/AIInterview/CreateAIQuestionVideo',
            type: 'POST',
            data: { id: qid },
            timeout: 120000
        }).done(function (res) {
            if (res && typeof res === 'object' && res.success && res.videoId) {
                rememberPendingAiVideo(qid, res.videoId, startedAt);
                showQuestionToast(res.message || 'Generating the AI video. This takes a few minutes.', 'info');
                startAiVideoPolling(qid, res.videoId, startedAt);
                return;
            }
            // Nothing was started, so there is no render to keep waiting on.
            resetAiVideoButton($btn);
            var msg = (typeof res !== 'object' || res === null)
                ? 'Your session looks to have expired — sign in again and try once more.'
                : (res.message || 'Video generation failed.');
            reportAiVideoFailure($btn, msg, res);
        }).fail(function (xhr, status) {
            resetAiVideoButton($btn);
            reportAiVideoFailure($btn, status === 'timeout'
                ? 'Timed out asking Synthesia to start the render. Please try again.'
                : 'Could not reach the server (' + (xhr.status || 'unknown') + ').', xhr);
        });
    }

    function saveCodingMode(id, isCoding, minutes, $cb, gist) {
        var qid = parseInt(id, 10);
        if (!qid) return;

        var payload = getQuestionsRequestData();
        payload.id = qid;
        payload.isCodingQuestion = isCoding;
        if (isCoding) {
            payload.codingTimeLimitInMinutes = parseInt(minutes, 10) || 15;
        }
        if ($.trim(gist || '')) {
            payload.questionGist = $.trim(gist);
        }

        $.post('/IRF/SetQuestionCodingMode', payload).done(function (res) {
            if (res && res.success) {
                showQuestionToast(isCoding
                    ? 'Saved. Candidates will get the code editor for this question.'
                    : 'Saved.', 'success');
            } else {
                // Put the checkbox back so the panel never claims a state the server rejected
                $cb.prop('checked', !isCoding);
                showQuestionToast((res && res.message) ? res.message : 'Could not update the question.', 'error');
            }
        }).fail(function (xhr) {
            $cb.prop('checked', !isCoding);
            showQuestionToast('Could not reach the server (' + (xhr.status || 'unknown') + ').', 'error');
        });
    }

    function saveListeningMode(id, isListening, transcript, gist, $cb) {
        var qid = parseInt(id, 10);
        if (!qid) return;

        var payload = getQuestionsRequestData();
        payload.id = qid;
        payload.isListeningComprehension = isListening;
        if ($.trim(transcript || '')) {
            payload.listeningAudioTranscription = $.trim(transcript);
        }
        if ($.trim(gist || '')) {
            payload.questionGist = $.trim(gist);
        }

        $.post('/IRF/SetQuestionListeningMode', payload).done(function (res) {
            if (res && res.success) {
                showQuestionToast(isListening
                    ? 'Saved. Candidates will get the audio clip and a summary box for this question.'
                    : 'Saved.', 'success');
            } else {
                // Put the checkbox back so the panel never claims a state the server rejected
                $cb.prop('checked', !isListening);
                $cb.closest('.dm-v4-question-listening').find('.dm-v4-listening-body').prop('hidden', !$cb.prop('checked'));
                showQuestionToast((res && res.message) ? res.message : 'Could not update the question.', 'error');
            }
        }).fail(function (xhr) {
            $cb.prop('checked', !isListening);
            $cb.closest('.dm-v4-question-listening').find('.dm-v4-listening-body').prop('hidden', !$cb.prop('checked'));
            showQuestionToast('Could not reach the server (' + (xhr.status || 'unknown') + ').', 'error');
        });
    }

    function uploadListeningAudio($btn) {
        var qid = parseInt($btn.data('id'), 10);
        if (!qid || $btn.prop('disabled')) return;

        var $wrap = $btn.closest('.dm-v4-question-listening');
        var $file = $wrap.find('.dm-v4-listening-file');
        var file = ($file[0] && $file[0].files && $file[0].files[0]) || null;
        if (!file) {
            showQuestionToast('Choose an audio file first.', 'error');
            return;
        }

        var data = new FormData();
        data.append('irfId', getIrfId());
        data.append('id', qid);
        data.append('audioFile', file);

        var origHtml = $btn.html();
        $btn.prop('disabled', true).html('<i class="fa fa-spinner fa-spin"></i> Uploading…');

        $.ajax({
            url: '/IRF/UploadListeningAudio',
            type: 'POST',
            data: data,
            processData: false,
            contentType: false,
            timeout: 300000
        }).done(function (res) {
            if (res && res.success && res.audioUrl) {
                $wrap.find('.dm-v4-listening-audio-link')
                    .attr('href', res.audioUrl)
                    .prop('hidden', false);
                $file.val('');
                $btn.prop('disabled', false).html(origHtml);
                showQuestionToast(res.message || 'Audio uploaded.', 'success');
            } else {
                $btn.prop('disabled', false).html(origHtml);
                showQuestionToast((res && res.message) ? res.message : 'Upload failed.', 'error');
            }
        }).fail(function (xhr, status) {
            var msg = status === 'timeout'
                ? 'Upload timed out. Try a smaller clip.'
                : 'Could not reach the server (' + (xhr.status || 'unknown') + ').';
            $btn.prop('disabled', false).html(origHtml);
            showQuestionToast(msg, 'error');
        });
    }

    function initQuestionsV4() {
        if (!getPanel().length) return;

        loadQuestions();
        if ((window.location.hash || '') === '#sample-questions') {
            var $collapse = $('#dmV4QuestionsCollapse');
            var $panel = $('#dmV4QuestionsPanel');
            $collapse.addClass('is-open');
            $collapse.find('.dm-v4-collapse__head').attr('aria-expanded', 'true');
            $panel.prop('hidden', false).show();
            var el = document.getElementById('dmV4QuestionsCollapse');
            if (el && el.scrollIntoView) {
                el.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
        }

        $(document).off('click.dmV4GenQ', '#dmV4GenerateQuestionsBtn').on('click.dmV4GenQ', '#dmV4GenerateQuestionsBtn', function () {
            var $btn = $(this);
            if ($btn.prop('disabled')) return;
            var irfId = getIrfId();
            if (!irfId) return;

            $btn.prop('disabled', true);
            var origHtml = $btn.html();
            $btn.html('<i class="fa fa-spinner fa-spin"></i> Generating questions from your rubric…');

            $.ajax({
                url: '/IRF/GenerateQuestionsFromRubric',
                type: 'POST',
                data: getQuestionsRequestData(),
                timeout: 300000
            })
                .done(function (res) {
                    if (res && res.success) {
                        loadQuestions();
                        var msg = res.count + ' question(s) created from rubric.';
                        if (res.warning) msg += ' ' + res.warning;
                        if (typeof swal !== 'undefined') swal.fire('Questions Generated', msg, res.warning ? 'warning' : 'success');
                    } else {
                        if (typeof swal !== 'undefined') swal.fire('Generation Failed', res.message || 'Could not generate questions.', 'error');
                        else alert(res.message || 'Could not generate questions.');
                    }
                })
                .fail(function (xhr, textStatus) {
                    var msg = textStatus === 'timeout'
                        ? 'Request timed out — AI generation can take up to a minute. Please try again.'
                        : 'Server error (' + (xhr.status || 'unknown') + ').';
                    if (typeof swal !== 'undefined') swal.fire('Error', msg, 'error');
                    else alert(msg);
                })
                .always(function () {
                    $btn.prop('disabled', false).html(origHtml);
                });
        });

        // dm-v4-code-project.js refreshes the list when a project publishes or fails.
        window.dmV4ReloadQuestions = loadQuestions;
        window.dmV4GetQuestionsRequestData = getQuestionsRequestData;
        $(document).off('click.dmCpView', '.dm-cp-view-btn').on('click.dmCpView', '.dm-cp-view-btn', function () {
            if (window.dmCodeProject) window.dmCodeProject.open(parseInt($(this).data('id'), 10));
        });

        $(document).off('click.dmV4AddQ', '.dm-v4-add-question-btn').on('click.dmV4AddQ', '.dm-v4-add-question-btn', function () {
            openQuestionPopup($(this).data('skill'));
        });

        $(document).off('click.dmV4DelQ', '.dm-v4-delete-question-btn').on('click.dmV4DelQ', '.dm-v4-delete-question-btn', function () {
            deleteQuestion(parseInt($(this).data('id'), 10));
        });

        $(document).off('click.dmV4RegenVid', '.dm-v4-regen-ai-video-btn').on('click.dmV4RegenVid', '.dm-v4-regen-ai-video-btn', function () {
            regenerateAiVideo($(this));
        });

        $(document).off('change.dmV4Flag', '.dm-v4-flag-cb').on('change.dmV4Flag', '.dm-v4-flag-cb', function (e) {
            e.preventDefault();
            var $cb = $(this);
            if ($cb.prop('checked') && typeof FlagQuestionForSentimentAnalysis === 'function') {
                FlagQuestionForSentimentAnalysis($cb.data('id'));
            }
        });

        $(document).off('change.dmV4Coding', '.dm-v4-coding-cb').on('change.dmV4Coding', '.dm-v4-coding-cb', function () {
            var $cb = $(this);
            var $row = $cb.closest('.dm-v4-question-row');
            var $mins = $row.find('.dm-v4-coding-mins');
            var $gist = $row.find('.dm-v4-coding-gist');
            var isCoding = $cb.prop('checked');

            $mins.prop('disabled', !isCoding);
            $row.find('.dm-v4-coding-mins-label').css('opacity', isCoding ? '' : '.5');
            if (isCoding && !$mins.val()) {
                $mins.val(15);
            }

            // A coding question has no narratable text without a gist, so hold the save
            // until one is written rather than letting the server reject the toggle.
            if (isCoding && !$.trim($gist.val())) {
                $row.find('.dm-v4-question-gist-warning').prop('hidden', false);
                $gist.focus();
                return;
            }

            $row.find('.dm-v4-question-gist-warning').prop('hidden', true);
            saveCodingMode($cb.data('id'), isCoding, $mins.val(), $cb, $gist.val());
        });

        $(document).off('change.dmV4CodingMins', '.dm-v4-coding-mins').on('change.dmV4CodingMins', '.dm-v4-coding-mins', function () {
            var $mins = $(this);
            var $row = $mins.closest('.dm-v4-question-row');
            var $cb = $row.find('.dm-v4-coding-cb');
            if (!$cb.prop('checked')) return;
            saveCodingMode($mins.data('id'), true, $mins.val(), $cb, $row.find('.dm-v4-coding-gist').val());
        });

        $(document).off('change.dmV4CodingGist', '.dm-v4-coding-gist').on('change.dmV4CodingGist', '.dm-v4-coding-gist', function () {
            var $gist = $(this);
            var $row = $gist.closest('.dm-v4-question-row');
            var $cb = $row.find('.dm-v4-coding-cb');
            if (!$.trim($gist.val())) return;

            $row.find('.dm-v4-question-gist-warning').prop('hidden', true);
            saveCodingMode($gist.data('id'), $cb.prop('checked'), $row.find('.dm-v4-coding-mins').val(), $cb, $gist.val());
        });

        $(document).off('change.dmV4Listening', '.dm-v4-listening-cb').on('change.dmV4Listening', '.dm-v4-listening-cb', function () {
            var $cb = $(this);
            var $row = $cb.closest('.dm-v4-question-row');
            var $wrap = $cb.closest('.dm-v4-question-listening');
            var $body = $wrap.find('.dm-v4-listening-body');
            var $transcript = $wrap.find('.dm-v4-listening-transcript');
            var $warning = $wrap.find('.dm-v4-listening-warning');
            var $gist = $row.find('.dm-v4-coding-gist');
            var isListening = $cb.prop('checked');

            $body.prop('hidden', !isListening);

            // Hold the save until the clip, transcript and gist are all present, rather than
            // firing a request the server will reject.
            if (isListening) {
                var hasAudio = !$wrap.find('.dm-v4-listening-audio-link').prop('hidden');
                if (!hasAudio) {
                    $warning.text('Upload the audio clip first.').prop('hidden', false);
                    $cb.prop('checked', false);
                    $body.prop('hidden', false);
                    return;
                }
                if (!$.trim($transcript.val())) {
                    $warning.text('Add the audio transcript — the candidate\'s summary is scored against it.').prop('hidden', false);
                    $cb.prop('checked', false);
                    $transcript.focus();
                    return;
                }
                if (!$.trim($gist.val())) {
                    $warning.text('Write the question gist — the AI avatar reads it to introduce the exercise.').prop('hidden', false);
                    $cb.prop('checked', false);
                    $gist.focus();
                    return;
                }
            }

            $warning.prop('hidden', true);
            saveListeningMode($cb.data('id'), isListening, $transcript.val(), $gist.val(), $cb);
        });

        $(document).off('change.dmV4ListeningTranscript', '.dm-v4-listening-transcript').on('change.dmV4ListeningTranscript', '.dm-v4-listening-transcript', function () {
            var $transcript = $(this);
            var $row = $transcript.closest('.dm-v4-question-row');
            var $wrap = $transcript.closest('.dm-v4-question-listening');
            var $cb = $wrap.find('.dm-v4-listening-cb');
            if (!$cb.prop('checked') || !$.trim($transcript.val())) return;

            $wrap.find('.dm-v4-listening-warning').prop('hidden', true);
            saveListeningMode($transcript.data('id'), true, $transcript.val(), $row.find('.dm-v4-coding-gist').val(), $cb);
        });

        $(document).off('click.dmV4ListeningUpload', '.dm-v4-listening-upload-btn').on('click.dmV4ListeningUpload', '.dm-v4-listening-upload-btn', function () {
            uploadListeningAudio($(this));
        });

        $('#dmV4QuestionsCollapse').off('click.dmV4QReload').on('click.dmV4QReload', '.dm-v4-collapse__head', function () {
            var $c = $('#dmV4QuestionsCollapse');
            if (!$c.hasClass('is-open')) return;
            setTimeout(loadQuestions, 200);
        });
    }

    window.dmV4RefreshQuestions = loadQuestions;
    window.dmV4InitQuestions = initQuestionsV4;
    window.dmV4UsesSynthesia = usesSynthesia;
})(jQuery);
