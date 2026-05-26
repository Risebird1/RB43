/**

 * DM V4 — sample questions (wireframe list + legacy popup for add).

 */

(function ($) {

    'use strict';



    function getPanel() {

        return $('#dmV4QuestionsPanel');

    }



    function getIrfId() {

        return parseInt(getPanel().data('irf-id'), 10) || parseInt($('#id').val(), 10);

    }



    function isAiPreset() {

        return getPanel().data('is-ai-preset') === '1' || getPanel().data('is-ai-preset') === 1;

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



    function groupBySkill(questions) {

        var map = {};

        questions.forEach(function (q) {

            var sn = q.skillName || q.SkillName || 'General';

            if (!map[sn]) map[sn] = [];

            map[sn].push(q);

        });

        return map;

    }



    function openQuestionPopup(skillName) {

        var irfId = getIrfId();

        if (typeof OpenQuestionModal === 'function') {

            OpenQuestionModal(irfId, skillName || null);

        }

    }



    function renderQuestions(questions) {

        var $host = $('#dmV4QuestionsSkills');

        var rubricSkills = getRubricSkills();

        updateCountBadge(questions, rubricSkills);



        var grouped = groupBySkill(questions);

        var skillsToShow = rubricSkills.length

            ? rubricSkills.slice()

            : Object.keys(grouped).sort();



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

                    var expected = q.expectedAnswer || q.ExpectedAnswer || '';

                    var video = q.aiQuestionVideoLink || q.AIQuestionVideoLink || '';

                    html += '<div class="dm-v4-question-row" data-question-id="' + qid + '">';

                    html += '<div class="dm-v4-question-col">';

                    html += '<div class="dm-v4-question-label">Question Q' + (qi + 1) + '</div>';

                    html += '<div class="dm-v4-question-text">' + (comment ? escapeHtml(comment) : '<span class="dm-v4-question-text--placeholder">Add question…</span>') + '</div>';

                    html += '</div><div class="dm-v4-question-col">';

                    html += '<div class="dm-v4-question-label">Expected answer</div>';

                    html += '<div class="dm-v4-question-text' + (expected ? '' : ' dm-v4-question-text--muted') + '">';

                    html += expected ? escapeHtml(expected) : 'Add expected answer…';

                    html += '</div>';

                    if (isAiPreset()) {

                        html += '<div class="dm-v4-question-meta">';

                        if (video) {

                            html += '<a href="' + escapeHtml(video) + '" target="_blank" class="dm-v4-question-link">AI video ↗</a>';

                        } else if (comment) {

                            html += '<button type="button" class="dm-v4-btn dm-v4-btn--ghost dm-v4-btn--xs dm-v4-ai-video-btn" data-id="' + qid + '"><i class="fa fa-video-camera"></i> AI video</button>';

                        }

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



    function loadQuestions() {

        return $.post('/IRF/GetDeliveryHeadSampleQuestionsJson', { irfId: getIrfId() }).then(function (res) {

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



    function initQuestionsV4() {

        if (!getPanel().length) return;



        loadQuestions();



        $(document).off('click.dmV4GenQ', '#dmV4GenerateQuestionsBtn').on('click.dmV4GenQ', '#dmV4GenerateQuestionsBtn', function () {

            openQuestionPopup(null);

        });

        $(document).off('click.dmV4AddQ', '.dm-v4-add-question-btn').on('click.dmV4AddQ', '.dm-v4-add-question-btn', function () {

            openQuestionPopup($(this).data('skill'));

        });

        $(document).off('click.dmV4DelQ', '.dm-v4-delete-question-btn').on('click.dmV4DelQ', '.dm-v4-delete-question-btn', function () {

            deleteQuestion(parseInt($(this).data('id'), 10));

        });

        $(document).off('click.dmV4AiVid', '.dm-v4-ai-video-btn').on('click.dmV4AiVid', '.dm-v4-ai-video-btn', function () {

            if (typeof CreateAIQuestionVideo === 'function') CreateAIQuestionVideo($(this).data('id'));

        });



        $(document).off('change.dmV4Flag', '.dm-v4-flag-cb').on('change.dmV4Flag', '.dm-v4-flag-cb', function (e) {

            e.preventDefault();

            var $cb = $(this);

            if ($cb.prop('checked') && typeof FlagQuestionForSentimentAnalysis === 'function') {

                FlagQuestionForSentimentAnalysis($cb.data('id'));

            }

        });



        $('#dmV4QuestionsCollapse').off('click.dmV4QReload').on('click.dmV4QReload', '.dm-v4-collapse__head', function () {

            var $c = $('#dmV4QuestionsCollapse');

            if (!$c.hasClass('is-open')) return;

            setTimeout(loadQuestions, 200);

        });

    }



    window.dmV4RefreshQuestions = loadQuestions;

    window.dmV4InitQuestions = initQuestionsV4;

})(jQuery);

