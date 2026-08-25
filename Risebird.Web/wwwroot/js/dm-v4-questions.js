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
                timer: 4500,
                type: type || 'error',
                title: message,
                customClass: {
                    popup: 'dm-v4-toast-popup',
                    title: 'dm-v4-toast-title'
                }
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
                    var isAiQuestion = q.isAIGenerated || q.IsAIGenerated || q.isGeneratedByAi || q.IsGeneratedByAi;
                    // AI-generated rows often have no saved gist yet; the avatar would speak the question.
                    if (!$.trim(gist) && !isCoding && $.trim(comment)) {
                        gist = comment;
                    }

                    html += '<div class="dm-v4-question-row" data-question-id="' + qid + '">';
                    html += '<div class="dm-v4-question-col">';
                    html += '<div class="dm-v4-question-label">Question Q' + (qi + 1);
                    if (isAiQuestion) {
                        html += ' <span class="dm-v4-badge dm-v4-badge--ai">AI</span>';
                    }
                    html += '</div>';
                    html += '<div class="dm-v4-question-text">' + (hasComment ? escapeHtml(comment) : '<span class="dm-v4-question-text--placeholder">Add question…</span>') + '</div>';
                    if ($.trim(idealAnswer)) {
                        html += '<details class="dm-v4-ideal-answer"><summary>Ideal Answer (AI)</summary><div class="dm-v4-ideal-answer__body">' + escapeHtml(idealAnswer) + '</div></details>';
                    }

                    html += '<div class="dm-v4-question-gist">';
                    html += '<div class="dm-v4-question-label">Question gist <span class="dm-v4-question-gist-hint">spoken by the AI avatar</span></div>';
                    html += '<textarea class="dm-v4-coding-gist" rows="2" data-id="' + qid + '"';
                    html += ' placeholder="What the AI avatar should speak. Required for coding questions.">' + escapeHtml(gist) + '</textarea>';
                    html += '<div class="dm-v4-question-gist-warning" hidden>Write the gist to finish enabling coding mode.</div>';
                    html += '</div>';

                    // Coding mode is editable in place so questions added earlier, or generated from
                    // the rubric, can be turned into coding questions without being retyped.
                    html += '<div class="dm-v4-question-meta dm-v4-question-coding">';
                    html += '<label class="dm-v4-question-flag"><input type="checkbox" class="dm-v4-coding-cb" data-id="' + qid + '"' + (isCoding ? ' checked' : '') + (qid ? '' : ' disabled') + ' /> Coding question</label>';
                    html += '<input type="number" min="1" max="180" step="1" class="dm-v4-coding-mins" data-id="' + qid + '"';
                    html += ' value="' + (codingTimeLimit ? escapeHtml(String(codingTimeLimit)) : '15') + '"';
                    html += ' title="Time limit in minutes"' + (isCoding ? '' : ' disabled') + ' />';
                    html += '<span class="dm-v4-coding-mins-label"' + (isCoding ? '' : ' style="opacity:.5"') + '>min</span>';
                    html += '</div>';

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
                        var needsGist = isCoding && !$.trim(gist);
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

    function loadQuestions() {
        return $.post('/IRF/GetDeliveryHeadSampleQuestionsJson', getQuestionsRequestData()).then(function (res) {
            if (res && res.success) {
                renderQuestions(res.questions || []);
            }
        });
    }

    function deleteQuestion(id) {
        if (typeof swal === 'undefined') {
            if (confirm('Remove this question?')) {
                $.post('/IRF/DeleteSkillChannelSampleQuestions', { questionid: id }, loadQuestions);
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
                $.post('/IRF/DeleteSkillChannelSampleQuestions', { questionid: id }, function () {
                    loadQuestions();
                });
            }
        });
    }

    function regenerateAiVideo($btn) {
        var qid = parseInt($btn.data('id'), 10);
        if (!qid || $btn.prop('disabled')) return;

        var $row = $btn.closest('.dm-v4-question-row');
        var hadVideo = $btn.data('has-video') === 1 || $btn.data('has-video') === '1';
        var normalLabel = aiVideoButtonLabel(hadVideo);

        $btn.prop('disabled', true).removeClass('is-success is-error')
            .html('<i class="fa fa-spinner fa-spin"></i> Generating…');

        $.ajax({
            url: '/AIInterview/CreateAIQuestionVideo',
            type: 'POST',
            data: { id: qid },
            timeout: 300000
        }).done(function (res) {
            if (res && res.success && res.videoUrl) {
                var $meta = $row.find('.dm-v4-question-meta');
                var $link = $meta.find('.dm-v4-question-video-link');
                if ($link.length) {
                    $link.attr('href', res.videoUrl);
                } else {
                    $meta.prepend('<a href="' + escapeHtml(res.videoUrl) + '" target="_blank" rel="noopener" class="dm-v4-question-link dm-v4-question-video-link">AI video ↗</a> ');
                }
                $btn.data('has-video', '1').addClass('is-success')
                    .html('<i class="fa fa-check"></i> Generated');
                showQuestionToast(res.message || 'AI video ready.', 'success');
                setTimeout(function () {
                    $btn.prop('disabled', false).removeClass('is-success')
                        .html(aiVideoButtonLabel(true));
                }, 2200);
            } else {
                var msg = (res && res.message) ? res.message : 'Video generation failed.';
                $btn.prop('disabled', false).addClass('is-error').html(normalLabel);
                showQuestionToast(msg, 'error');
            }
        }).fail(function (xhr, status) {
            var msg = status === 'timeout'
                ? 'Request timed out — Synthesia may still be processing. Try again shortly.'
                : 'Could not reach the server (' + (xhr.status || 'unknown') + ').';
            $btn.prop('disabled', false).addClass('is-error').html(normalLabel);
            showQuestionToast(msg, 'error');
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
