/**
 * SME question review queue (AI Code Interview / Preset). Additive to dm-v4-questions.js.
 */
(function ($) {
    'use strict';

    var EA = [
        { k: 'expectedOutput', label: 'Expected output', hint: 'Is this actually what correct code produces?' },
        { k: 'answerExplanation', label: 'Explanation', hint: 'Does the reasoning hold?' },
        { k: 'safeImplementation', label: 'Safe implementation', hint: 'Is the reference implementation correct and safe?' },
        { k: 'semanticMatchCriteria', label: 'Semantic matching criteria', hint: 'Will this accept a correct answer worded differently?' },
        { k: 'misconceptionRedFlags', label: 'Misconception red flags', hint: 'Are these genuinely wrong, not merely unusual?' }
    ];
    var MAJOR = { expectedOutput: 1, answerExplanation: 1, safeImplementation: 1, semanticMatchCriteria: 1, misconceptionRedFlags: 1, maxScore: 1 };
    var CODE_PATTERNS = [
        /=>/, /->/, /::/, /\bdef\s/, /\bfunction\s/, /\bclass\s/, /\bimport\s/,
        /\bSELECT\s/i, /;\s*$/m, /\{[^}]*\}/, /\w\s*\([^)]*\)\s*[:{]/, /```/
    ];

    var state = { questions: [], sets: [], expert: null, openId: 0, showOriginal: {}, rubric: [], rubricLevels: {}, irfId: 0 };

    function pick(obj, names, fallback) {
        if (!obj) return fallback;
        for (var i = 0; i < names.length; i++) {
            if (obj[names[i]] !== undefined && obj[names[i]] !== null) return obj[names[i]];
        }
        return fallback;
    }
    function norm(s) { return String(s == null ? '' : s).trim().replace(/\s+/g, ' '); }
    function esc(s) {
        return String(s == null ? '' : s)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }
    function toast(msg, type) {
        var $host = $('#smeRqToastHost');
        if (!$host.length) {
            $host = $('<div id="smeRqToastHost" class="sme-rq-toast-host" aria-live="polite"></div>').appendTo('body');
        }
        $host.find('.sme-rq-toast').remove();
        var $el = $('<div class="sme-rq-toast ' + (type || 'info') + '"></div>').text(msg);
        $host.append($el);
        requestAnimationFrame(function () { $el.addClass('is-in'); });
        clearTimeout(toast._t);
        toast._t = setTimeout(function () {
            $el.removeClass('is-in');
            setTimeout(function () { $el.remove(); }, 180);
        }, 2400);
    }
    function gistCodeHit(g) {
        for (var i = 0; i < CODE_PATTERNS.length; i++) if (CODE_PATTERNS[i].test(g || '')) return true;
        return false;
    }
    function spokenSecs(g) {
        return Math.round(String(g || '').split(/\s+/).filter(Boolean).length / 2.5);
    }
    function parseSnap(payload) {
        if (!payload) return null;
        try {
            var o = typeof payload === 'string' ? JSON.parse(payload) : payload;
            return {
                title: o.title || o.Comment || '',
                gist: o.gist || o.QuestionGist || '',
                expectedOutput: o.out || o.expectedOutput || o.ExpectedOutput || '',
                answerExplanation: o.exp || o.explanation || o.AnswerExplanation || '',
                safeImplementation: o.safe || o.safeImplementation || o.SafeImplementation || '',
                semanticMatchCriteria: o.sem || o.semantic || o.SemanticMatchCriteria || '',
                misconceptionRedFlags: o.red || o.redFlags || o.MisconceptionRedFlags || '',
                maxScore: o.max != null ? o.max : o.MaxScore
            };
        } catch (e) { return null; }
    }
    function mapQuestion(raw) {
        var q = {
            id: pick(raw, ['id', 'Id'], 0),
            skillName: pick(raw, ['skillName', 'SkillName'], 'General'),
            title: pick(raw, ['comment', 'Comment'], ''),
            gist: pick(raw, ['questionGist', 'QuestionGist'], ''),
            expectedOutput: pick(raw, ['expectedOutput', 'ExpectedOutput'], ''),
            answerExplanation: pick(raw, ['answerExplanation', 'AnswerExplanation'], ''),
            safeImplementation: pick(raw, ['safeImplementation', 'SafeImplementation'], ''),
            semanticMatchCriteria: pick(raw, ['semanticMatchCriteria', 'SemanticMatchCriteria'], ''),
            misconceptionRedFlags: pick(raw, ['misconceptionRedFlags', 'MisconceptionRedFlags'], ''),
            expectedAnswer: pick(raw, ['expectedAnswer', 'ExpectedAnswer'], ''),
            maxScore: pick(raw, ['maxScore', 'MaxScore'], 0) || 0,
            time: pick(raw, ['codingTimeLimitInMinutes', 'CodingTimeLimitInMinutes'], 0) || 0,
            coding: !!(pick(raw, ['isCodingQuestion', 'IsCodingQuestion'], false)),
            sentiment: !!(pick(raw, ['isFlaggedForSentimentAnalysis', 'IsFlaggedForSentimentAnalysis'], false)),
            status: pick(raw, ['vettingStatus', 'VettingStatus'], null),
            reason: pick(raw, ['rejectionReason', 'RejectionReason'], ''),
            slot: pick(raw, ['questionSlot', 'QuestionSlot'], '') || '',
            variant: pick(raw, ['questionVariant', 'QuestionVariant'], '') || '',
            videoUrl: pick(raw, ['aiQuestionVideoLink', 'AIQuestionVideoLink'], '') || '',
            videoStatus: pick(raw, ['aiVideoStatus', 'AIVideoStatus'], 0) || 0,
            isGeneratedByAi: !!(pick(raw, ['isGeneratedByAi', 'IsGeneratedByAi'], false)),
            isAIGenerated: !!(pick(raw, ['isAIGenerated', 'IsAIGenerated'], false)),
            draftPayload: pick(raw, ['aiDraftPayload', 'AIDraftPayload'], null)
        };
        if (!q.expectedOutput && !q.answerExplanation && q.expectedAnswer) {
            q.expectedOutput = q.expectedAnswer;
        }
        q.ai = parseSnap(q.draftPayload);
        if (q.status === undefined) q.status = null;
        return q;
    }
    function videoKey(x) {
        if (x.videoStatus === 1) return 'gen';
        if (x.videoStatus === 3) return 'stale';
        if (x.videoStatus === 2 || (x.videoUrl && x.videoStatus !== 4)) return 'ready';
        if (x.videoStatus === 4) return 'failed';
        return 'none';
    }
    function changedFields(x) {
        if (!x.ai) return [];
        var keys = ['title', 'gist', 'expectedOutput', 'answerExplanation', 'safeImplementation', 'semanticMatchCriteria', 'misconceptionRedFlags', 'maxScore'];
        var out = [];
        for (var i = 0; i < keys.length; i++) {
            var k = keys[i];
            if (norm(x[k]) !== norm(x.ai[k])) out.push(k);
        }
        return out;
    }
    function severityOf(x) {
        var ch = changedFields(x);
        if (!ch.length) return null;
        for (var i = 0; i < ch.length; i++) if (MAJOR[ch[i]]) return 'Major';
        return 'Minor';
    }
    function originKind(x) {
        if (x.ai) {
            return severityOf(x) ? 'edited' : 'ai';
        }
        if (x.isGeneratedByAi || x.isAIGenerated) return 'unknown';
        return 'sme';
    }
    function blockers(x) {
        var b = [];
        for (var i = 0; i < EA.length; i++) if (!norm(x[EA[i].k])) b.push('Expected answer — ' + EA[i].label.toLowerCase() + ' is empty');
        if (!x.maxScore) b.push('Max score is not set (the report renders every score against it)');
        if (!x.time) b.push('Time in minutes is not set');
        if (!norm(x.gist)) b.push('Avatar gist is empty');
        else if (gistCodeHit(x.gist)) b.push('Avatar gist contains code syntax — the avatar reads it aloud');
        var requireVideo = $('#dmV4QuestionsPanel').attr('data-is-ai-preset') === '1';
        if (requireVideo) {
            var vk = videoKey(x);
            if (vk === 'stale') b.push('AI video is stale — regenerate it before confirming');
            if (vk === 'none' || vk === 'failed') b.push('AI video has not been generated yet');
            if (vk === 'gen') b.push('AI video is still generating');
        }
        return b;
    }
    function payload(x) {
        return {
            irfId: state.irfId,
            id: x.id,
            comment: x.title,
            questionGist: x.gist,
            expectedOutput: x.expectedOutput,
            answerExplanation: x.answerExplanation,
            safeImplementation: x.safeImplementation,
            semanticMatchCriteria: x.semanticMatchCriteria,
            misconceptionRedFlags: x.misconceptionRedFlags,
            maxScore: x.maxScore,
            timeLimitMins: x.time,
            isCodingQuestion: x.coding,
            flagForSentiment: x.sentiment,
            questionSlot: x.slot,
            questionVariant: x.variant,
            skillName: x.skillName
        };
    }
    function postJson(url, body) {
        return $.ajax({
            url: url,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(body)
        });
    }

    function statusPill(x) {
        if (x.status === 1) return '<span class="sme-rq-pill green">Confirmed</span>';
        if (x.status === 2) return '<span class="sme-rq-pill red">Rejected</span>';
        return '<span class="sme-rq-pill amber">Draft — to review</span>';
    }
    function originPill(x) {
        var kind = originKind(x);
        var sev = severityOf(x);
        if (kind === 'sme') return '<span class="sme-rq-pill blue">Added by expert</span>';
        if (kind === 'unknown') return '<span class="sme-rq-pill grey">Origin: unknown</span>';
        if (kind === 'edited') return '<span class="sme-rq-pill amber">AI draft &middot; edited by you</span><span class="sme-rq-pill ' + (sev === 'Major' ? 'red' : 'grey') + '">' + sev + '</span>';
        return '<span class="sme-rq-pill grey">AI draft</span>';
    }
    function tcLine(x, k) {
        if (!x.ai || norm(x[k]) === norm(x.ai[k])) return '';
        var sev = MAJOR[k] ? 'Major' : 'Minor';
        var old = String(x.ai[k] == null ? '' : x.ai[k]);
        return '<div class="sme-rq-tc"><span class="sme-rq-pill ' + (sev === 'Major' ? 'red' : 'grey') + '">' + sev + '</span> AI wrote: <s>' + esc(old.slice(0, 150)) + (old.length > 150 ? '…' : '') + '</s></div>';
    }

    function bodyHtml(x) {
        var ro = x.status === 2 ? ' disabled' : '';
        var h = '';
        if (x.status === 2) {
            h += '<div class="sme-rq-block"><b>Rejected.</b> ' + esc(x.reason || '') + '<br><span class="sme-rq-hint">Kept for audit — never served, never selectable in a set.</span></div>';
        }
        h += '<div class="sme-rq-ea-panel sme-rq-ea"><h3>Expected answer <span class="sme-rq-pill blue">the AI scores every candidate against this</span></h3>';
        h += '<div class="sme-rq-hint">Five parts. All five must be filled before you can confirm — an empty part silently weakens grading.</div>';
        for (var i = 0; i < EA.length; i++) {
            var f = EA[i];
            var empty = !norm(x[f.k]);
            h += '<div class="sme-rq-fld"><div class="sme-rq-flabel">' + f.label + (empty ? ' <span class="sme-rq-pill red">empty</span>' : '') +
                ' <span class="sme-rq-fhint">' + f.hint + '</span></div>';
            h += '<textarea class="sme-rq-inp sme-rq-edit' + (f.k === 'safeImplementation' ? ' sme-rq-code' : '') + '" rows="' + (f.k === 'safeImplementation' ? 4 : 2) + '"' + ro + ' data-id="' + x.id + '" data-k="' + f.k + '">' + esc(x[f.k]) + '</textarea>';
            h += tcLine(x, f.k) + '</div>';
        }
        h += '</div>';
        h += '<div class="sme-rq-fld"><div class="sme-rq-flabel">Question text</div>';
        h += '<textarea class="sme-rq-inp sme-rq-edit" rows="3"' + ro + ' data-id="' + x.id + '" data-k="title">' + esc(x.title) + '</textarea>' + tcLine(x, 'title') + '</div>';

        var hit = gistCodeHit(x.gist), secs = spokenSecs(x.gist);
        h += '<div class="sme-rq-fld"><div class="sme-rq-flabel">Question gist spoken by the AI avatar' + (hit ? ' <span class="sme-rq-pill red">contains code</span>' : '') + '</div>';
        h += '<textarea class="sme-rq-inp sme-rq-edit" rows="3"' + ro + ' data-id="' + x.id + '" data-k="gist">' + esc(x.gist) + '</textarea>';
        if (hit) h += '<div class="sme-rq-bad">Gist contains code syntax — the avatar reads this aloud. Move the code into the problem statement.</div>';
        h += '<div class="sme-rq-hint" style="margin-top:5px">≈ ' + secs + ' s spoken' + (secs > 45 ? ' — over the 45 s guidance' : '') + '</div>' + tcLine(x, 'gist') + '</div>';

        h += '<div class="sme-rq-meta">';
        h += '<div class="m"><div class="sme-rq-flabel">Max score</div><input type="number" class="sme-rq-inp sme-rq-edit" data-id="' + x.id + '" data-k="maxScore"' + ro + ' value="' + esc(x.maxScore) + '"></div>';
        h += '<div class="m"><div class="sme-rq-flabel">Time (min)</div><input type="number" class="sme-rq-inp sme-rq-edit" data-id="' + x.id + '" data-k="time"' + ro + ' value="' + esc(x.time) + '"></div>';
        h += '<div class="m" style="min-width:130px"><div class="sme-rq-flabel">Flags</div>';
        h += '<label class="sme-rq-chk"><input type="checkbox" class="sme-rq-edit" data-id="' + x.id + '" data-k="coding"' + ro + (x.coding ? ' checked' : '') + '> Coding question</label>';
        h += '<label class="sme-rq-chk"><input type="checkbox" class="sme-rq-edit" data-id="' + x.id + '" data-k="sentiment"' + ro + (x.sentiment ? ' checked' : '') + '> Flag for sentiment</label></div>';
        h += '<div class="m" style="min-width:250px"><div class="sme-rq-flabel">AI question video</div>' + videoHtml(x) + '</div>';
        h += '<div class="sme-rq-spacer"></div>';
        h += '<button type="button" class="sme-rq-btn sm ghost sme-rq-toggle-orig" data-id="' + x.id + '">' + (state.showOriginal[x.id] ? 'Hide' : 'Show') + ' AI original</button>';
        h += '</div>' + tcLine(x, 'maxScore');

        if (state.showOriginal[x.id] && x.ai) {
            h += '<div class="sme-rq-repbox"><div class="sme-rq-flabel">AI original — immutable snapshot (AIDraftPayload)</div>';
            h += '<div class="sme-rq-hint"><b>Question:</b> ' + esc(x.ai.title) + '<br><b>Gist:</b> ' + esc(x.ai.gist) + '<br><b>Expected output:</b> ' + esc(x.ai.expectedOutput) + '<br><b>Max score:</b> ' + esc(x.ai.maxScore) + '</div></div>';
        }

        if (x.status !== 2) {
            var b = blockers(x);
            if (b.length) {
                h += '<div class="sme-rq-block"><b>Cannot confirm yet:</b><ul>';
                for (var j = 0; j < b.length; j++) h += '<li>' + esc(b[j]) + '</li>';
                h += '</ul></div>';
            }
            h += '<div class="sme-rq-actions">';
            h += '<button type="button" class="sme-rq-btn primary sme-rq-confirm" data-id="' + x.id + '"' + (b.length ? ' disabled' : '') + '>' + (x.status === 1 ? 'Re-confirm' : 'Confirm') + '</button>';
            h += '<button type="button" class="sme-rq-btn danger sme-rq-reject" data-id="' + x.id + '">Reject</button>';
            h += '<button type="button" class="sme-rq-btn ghost sme-rq-next" data-id="' + x.id + '">Save &amp; next</button>';
            if (x.status === 1) h += '<span class="sme-rq-hint">Confirmed by you &middot; served to candidates</span>';
            h += '</div>';
        }
        return h;
    }
    function videoHtml(x) {
        var k = videoKey(x);
        if (k === 'ready') return '<span class="sme-rq-pill green">Ready</span> ' + (x.videoUrl ? '<a href="' + esc(x.videoUrl) + '" target="_blank" rel="noopener" class="sme-rq-video-link">view</a> ' : '') + '<button type="button" class="sme-rq-btn sm ghost sme-rq-regen" data-id="' + x.id + '">Regenerate</button>';
        if (k === 'stale') return '<span class="sme-rq-pill red">Stale</span> <span class="sme-rq-hint">question changed</span> <button type="button" class="sme-rq-btn sm primary sme-rq-regen" data-id="' + x.id + '">Regenerate</button>';
        if (k === 'gen') return '<span class="sme-rq-pill amber">Generating…</span>';
        return '<span class="sme-rq-pill grey">Not generated</span> <button type="button" class="sme-rq-btn sm primary sme-rq-regen" data-id="' + x.id + '">Generate</button>';
    }

    function setIssues(s) {
        var bad = [], warn = [], slots = {};
        var qs = (s.questionIds || s.QuestionIds || []).map(byId).filter(Boolean);
        for (var i = 0; i < qs.length; i++) {
            if (qs[i].status === 2) bad.push('“' + (qs[i].title || '').slice(0, 30) + '…” was rejected');
            else if (qs[i].status !== 1) bad.push('“' + (qs[i].title || '').slice(0, 30) + '…” is not confirmed yet');
            if (qs[i].slot) {
                if (slots[qs[i].slot]) bad.push('two variants of slot ' + qs[i].slot);
                slots[qs[i].slot] = 1;
            }
        }
        for (var r = 0; r < state.rubric.length; r++) {
            var covered = qs.some(function (x) { return (x.skillName || '').toLowerCase() === state.rubric[r].toLowerCase(); });
            if (!covered) warn.push('rubric item “' + state.rubric[r].slice(0, 26) + '…” not covered');
        }
        if (qs.length * 20 !== 100) warn.push(qs.length + ' × 20 = ' + (qs.length * 20) + ' — the report’s communication score assumes 100');
        return { bad: bad, warn: warn, qs: qs };
    }
    function byId(id) {
        for (var i = 0; i < state.questions.length; i++) if (state.questions[i].id === id) return state.questions[i];
        return null;
    }

    function render() {
        var $host = $('#dmV4QuestionsSkills');
        if (!$host.length) return;
        $('#dmV4QuestionsCollapse').addClass('sme-rq-page--review');
        var conf = state.questions.filter(function (x) { return x.status === 1; }).length;
        var rej = state.questions.filter(function (x) { return x.status === 2; }).length;
        var todo = state.questions.length - conf - rej;
        var setsApproved = state.sets.filter(function (s) { return (s.reviewStatus || s.ReviewStatus) === 1; }).length;
        var ready = todo === 0 && setsApproved > 0
            ? '<span class="sme-rq-pill green">Ready for candidates</span>'
            : (todo === 0 ? '<span class="sme-rq-pill amber">All reviewed — approve a set to go live</span>' : '<span class="sme-rq-pill amber">' + todo + ' unreviewed</span>');

        var html = '<div class="sme-rq">';
        html += '<p class="sme-rq-intro">This IRF is marked for AI Code Interview. The AI has drafted the questions, the expected answers and the question sets. <b>You are the appointed SME — your job is to review them.</b> Nothing here reaches a candidate until you confirm it.</p>';
        html += '<div class="sme-rq-card"><h2>Review queue <span class="sme-rq-pill blue">' + state.questions.length + ' questions</span><span class="sme-rq-spacer"></span>' + ready + '</h2>';
        var den = Math.max(state.questions.length, 1);
        html += '<div class="sme-rq-prog"><i class="done" style="width:' + (conf / den * 100) + '%"></i><i class="rej" style="width:' + (rej / den * 100) + '%"></i></div>';
        html += '<div class="sme-rq-counts"><span><b>' + conf + '</b> confirmed</span><span><b>' + rej + '</b> rejected</span><span><b>' + todo + '</b> to review</span><span>' + state.questions.filter(function (x) { return severityOf(x); }).length + ' corrected by you</span></div>';

        var grouped = {};
        state.questions.forEach(function (q) {
            var sn = q.skillName || 'General';
            if (!grouped[sn]) grouped[sn] = [];
            grouped[sn].push(q);
        });
        var skills = state.rubric.length ? state.rubric.slice() : Object.keys(grouped);
        skills.forEach(function (sn) {
            var mine = grouped[sn] || [];
            var mc = mine.filter(function (x) { return x.status === 1; }).length;
            html += '<div class="sme-rq-rgroup"><div class="sme-rq-rhead">';
            if (state.rubricLevels[sn]) html += '<span class="sme-rq-pill blue">' + esc(String(state.rubricLevels[sn]).toUpperCase()) + '</span>';
            html += '<span class="sme-rq-rname">' + esc(sn) + '</span>';
            html += '<span class="sme-rq-pill ' + (mc ? 'green' : 'red') + '">' + mc + ' / ' + mine.length + ' confirmed</span>';
            html += '<span class="sme-rq-spacer"></span>';
            html += '<button type="button" class="sme-rq-btn sm ghost dm-v4-add-question-btn" data-skill="' + esc(sn) + '">+ Add question</button>';
            html += '<button type="button" class="sme-rq-btn sm ghost sme-rq-gen-more">&#10024; Generate more</button>';
            html += '<button type="button" class="sme-rq-btn sm ghost sme-rq-confirm-skill" data-skill="' + esc(sn) + '">Confirm all reviewed</button>';
            html += '</div>';
            if (!mine.length) html += '<div class="sme-rq-qrow"><span class="sme-rq-hint" style="color:#b91c1c">No questions — candidates cannot be served this rubric item.</span></div>';
            mine.forEach(function (x) {
                var isOpen = x.id === state.openId;
                html += '<div class="sme-rq-qrow' + (isOpen ? ' open' : '') + '" data-toggle="' + x.id + '"><div style="flex:1;min-width:0"><div class="sme-rq-qt">' + esc(x.title || 'Untitled') + '</div>';
                html += '<div class="sme-rq-qs">' + esc(x.slot || '—') + (x.variant ? ' &middot; ' + esc(x.variant) : '') + ' &middot; ' + x.maxScore + ' marks &middot; ' + x.time + ' min' + (x.coding ? ' &middot; coding' : '') + (x.sentiment ? ' &middot; sentiment' : '') + '</div></div>';
                html += originPill(x) + statusPill(x) + '</div>';
                html += '<div class="sme-rq-qbody' + (isOpen ? ' open' : '') + '">' + (isOpen ? bodyHtml(x) : '') + '</div>';
            });
            html += '</div>';
        });
        html += '</div>';

        html += '<div class="sme-rq-card"><h2>Question sets <span class="sme-rq-pill grey">AI proposed</span><span class="sme-rq-spacer"></span>';
        html += '<button type="button" class="sme-rq-btn sm" id="smeRqCreateSet">+ Create set</button></h2>';
        html += '<div class="sme-rq-hint" style="margin-bottom:13px">The AI proposed these combinations. Review each one: approve it, swap a question, or reject it. Candidate A gets Set A, candidate B gets Set B; no set mapped &rarr; seeded randomization.</div>';
        html += '<div class="sme-rq-setgrid">';
        if (!state.sets.length) html += '<div class="sme-rq-hint">No sets yet. Create one from confirmed questions.</div>';
        state.sets.forEach(function (s) {
            var iss = setIssues(s);
            var st = s.reviewStatus != null ? s.reviewStatus : s.ReviewStatus;
            var pill = st === 1 ? '<span class="sme-rq-pill green">Approved</span>' : st === 2 ? '<span class="sme-rq-pill red">Rejected</span>' : '<span class="sme-rq-pill amber">AI proposed</span>';
            var name = s.name || s.Name || 'Set';
            var sid = s.id || s.Id;
            html += '<div class="sme-rq-setcard"><h4>' + esc(name) + pill + '</h4>';
            if (st === 1 && (s.approvedBy || s.ApprovedBy)) {
                var when = s.approvedOn || s.ApprovedOn;
                var whenTxt = '';
                if (when) {
                    var d = new Date(when);
                    whenTxt = isNaN(d.getTime()) ? String(when) : d.toLocaleDateString();
                }
                html += '<div class="sme-rq-hint">Approved by ' + esc(s.approvedBy || s.ApprovedBy) + (whenTxt ? ' on ' + esc(whenTxt) : '') + '</div>';
            }
            html += '<div class="sme-rq-sl"><b>' + iss.qs.length + ' questions</b> &middot; ' + (iss.qs.length * 20) + ' comm. points<br>Slots: ' + (iss.qs.map(function (x) { return x.slot || '—'; }).join(', ') || '—') + '</div>';
            iss.bad.forEach(function (t) { html += '<div class="sme-rq-bad">&#10007; ' + esc(t) + '</div>'; });
            iss.warn.forEach(function (t) { html += '<div class="sme-rq-warn">&#9888; ' + esc(t) + '</div>'; });
            html += '<div class="sme-rq-acts">';
            html += '<button type="button" class="sme-rq-btn sm good sme-rq-approve-set" data-id="' + sid + '"' + (iss.bad.length ? ' disabled' : '') + '>Approve set</button>';
            html += '<button type="button" class="sme-rq-btn sm ghost sme-rq-adjust-set" data-id="' + sid + '">Adjust</button>';
            html += '<button type="button" class="sme-rq-btn sm danger sme-rq-reject-set" data-id="' + sid + '">Reject</button>';
            html += '<button type="button" class="sme-rq-btn sm ghost sme-rq-delete-set" data-id="' + sid + '">Delete</button>';
            html += '</div></div>';
        });
        html += '</div></div></div>';
        $host.html(html);
        renderExpert();
    }

    function renderExpert() {
        var $el = $('#smeExpertLoop');
        if (!$el.length) return;
        var e = state.expert;
        if (!e) { $el.empty(); return; }
        var canAct = window.dmV4ClientShareView === true;
        var st = e.status != null ? e.status : e.Status;
        var pill = st === 1 ? '<span class="sme-rq-pill green">Confirmed by client' + (e.confirmedOn || e.ConfirmedOn ? ' on ' + esc(e.signedOffOn || '') : '') + '</span>'
            : st === 2 ? '<span class="sme-rq-pill red">Change requested</span>'
            : '<span class="sme-rq-pill grey">Awaiting your confirmation</span>';
        var coded = e.expertCodedName || e.ExpertCodedName || 'Expert';
        var html = '<div class="sme-rq-card"><h2>Expert vetting your AI interview questions ' + pill + '</h2><div class="sme-rq-expcard">';
        html += '<div style="min-width:240px"><div class="sme-rq-hint" style="color:#15803d">Expert Coded Name:</div>';
        html += '<div class="sme-rq-codename" data-user-id="' + esc(e.expertUserId || e.ExpertUserId || '') + '" data-email="' + esc(e.expertEmail || e.ExpertEmail || '') + '">' + esc(coded) + '</div>';
        html += '<div class="sme-rq-kv">Skill channel: <b>' + esc(e.skillChannel || e.SkillChannel || '') + '</b></div>';
        html += '<div class="sme-rq-kv">Interviews conducted: <b>' + esc(e.interviewsConducted || e.InterviewsConducted || 0) + '</b></div></div>';
        html += '<div style="flex:1;min-width:280px">';
        html += '<div class="sme-rq-kv">Questions: <b>' + (e.confirmedCount || e.ConfirmedCount || 0) + ' confirmed</b> · ' + (e.rejectedCount || e.RejectedCount || 0) + ' rejected · ' + (e.correctedCount || e.CorrectedCount || 0) + ' corrected</div>';
        html += '<div class="sme-rq-kv">Sets approved: <b>' + esc(e.approvedSetNames || e.ApprovedSetNames || '—') + '</b></div>';
        html += '<div class="sme-rq-kv">Signed off: <b>' + esc(e.signedOffOn || e.SignedOffOn || '—') + '</b></div>';
        html += '<div class="sme-rq-hint" style="margin-top:10px">Confirming records your assurance that this expert vets your questions. It does not hold scheduling unless your channel requires it.</div>';
        if (canAct && st !== 1) {
            html += '<div style="display:flex;gap:9px;margin-top:12px;flex-wrap:wrap">';
            html += '<button type="button" class="sme-rq-btn good" id="smeRqConfirmExpert">Confirm this expert</button>';
            html += '<button type="button" class="sme-rq-btn danger" id="smeRqChangeExpert">Request a different expert</button></div>';
        } else if (!canAct) {
            html += '<div class="sme-rq-hint" style="margin-top:12px">Delivery view — confirmation is recorded by the client.</div>';
        } else {
            html += '<div class="sme-rq-hint" style="margin-top:12px">Recorded. Read-only from here.</div>';
        }
        html += '</div></div></div>';
        $el.html(html);
    }

    function nextUnreviewed(id) {
        var i, found = false;
        for (i = 0; i < state.questions.length; i++) {
            if (state.questions[i].id === id) { found = true; continue; }
            if (found && (state.questions[i].status === 0 || state.questions[i].status == null)) { state.openId = state.questions[i].id; return; }
        }
        for (i = 0; i < state.questions.length; i++) {
            if (state.questions[i].status === 0 || state.questions[i].status == null) { state.openId = state.questions[i].id; return; }
        }
        state.openId = 0;
    }

    function loadBundle() {
        state.irfId = parseInt($('#dmV4QuestionsPanel').data('irf-id'), 10) || 0;
        var raw = $('#dmV4QuestionsPanel').data('rubric-skills') || '';
        state.rubric = String(raw).split('|||').map(function (s) { return $.trim(s); }).filter(Boolean);
        state.rubricLevels = {};
        var levels = $('#dmV4QuestionsPanel').data('rubric-levels');
        if (typeof levels === 'string') {
            try { levels = JSON.parse(levels); } catch (e) { levels = {}; }
        }
        if (levels && typeof levels === 'object') state.rubricLevels = levels;
        var data = { irfId: state.irfId };
        if (window.dmV4ClientShareView === true) data.Type = window.dmV4ClientShareType || 'ublc';
        function applyBundle(res) {
            if (res && res.success) {
                state.questions = (res.questions || []).map(mapQuestion);
                state.sets = res.sets || [];
                state.expert = res.expert;
                if (!state.openId && state.questions.length) {
                    var first = state.questions.filter(function (x) { return x.status === 0 || x.status == null; })[0];
                    state.openId = first ? first.id : state.questions[0].id;
                }
            } else {
                toast((res && res.message) || 'Could not load review queue.', 'error');
            }
            render();
        }
        return $.post('/IRF/GetSMEQuestionReviewBundle', data)
            .done(applyBundle)
            .fail(function (xhr) {
                toast('Could not load review queue (HTTP ' + (xhr.status || '?') + ').', 'error');
                render();
            });
    }

    function bind() {
        var $root = $('#dmV4QuestionsCollapse');
        $root.off('.smeRq');
        $root.on('click.smeRq', '[data-toggle]', function (e) {
            if ($(e.target).closest('button, a, input, textarea, label').length) return;
            var id = parseInt($(this).data('toggle'), 10);
            state.openId = state.openId === id ? 0 : id;
            render();
        });
        $root.on('input.smeRq change.smeRq', '.sme-rq-edit', function () {
            var id = parseInt($(this).data('id'), 10);
            var k = $(this).data('k');
            var x = byId(id);
            if (!x) return;
            var v = $(this).is(':checkbox') ? $(this).prop('checked') : $(this).val();
            if (k === 'maxScore' || k === 'time') v = parseInt(v, 10) || 0;
            var prevTitle = x.title, prevGist = x.gist;
            x[k] = v;
            if ((k === 'title' || k === 'gist') && videoKey(x) === 'ready' && (norm(prevTitle) !== norm(x.title) || norm(prevGist) !== norm(x.gist))) {
                x.videoStatus = 3;
                toast('Question changed — regenerate the AI video before confirming.', 'warning');
            }
        });
        $root.on('blur.smeRq', '.sme-rq-edit', function () {
            var id = parseInt($(this).data('id'), 10);
            var x = byId(id);
            if (!x) return;
            postJson('/IRF/SaveSMEQuestionReview', payload(x)).done(function (res) {
                if (res && res.staleVideo) x.videoStatus = 3;
                render();
            });
        });
        $root.on('click.smeRq', '.sme-rq-confirm', function () {
            var x = byId(parseInt($(this).data('id'), 10));
            if (!x || blockers(x).length) return;
            postJson('/IRF/ConfirmSMEQuestion', { irfId: state.irfId, id: x.id, question: payload(x) }).done(function (res) {
                if (res && res.success) {
                    x.status = 1;
                    toast('Confirmed.', 'success');
                    nextUnreviewed(x.id);
                    render();
                } else if (res && res.blockers) {
                    toast(res.blockers[0], 'error');
                    render();
                }
            });
        });
        $root.on('click.smeRq', '.sme-rq-reject', function () {
            var id = parseInt($(this).data('id'), 10);
            var reason = window.prompt('Reject this question. Reason (required):', '');
            if (reason === null || !$.trim(reason)) { toast('Rejection needs a reason.', 'error'); return; }
            postJson('/IRF/RejectSMEQuestion', { irfId: state.irfId, id: id, reason: $.trim(reason) }).done(function (res) {
                if (res && res.success) { toast('Rejected — never served, kept for audit.', 'success'); loadBundle(); }
            });
        });
        $root.on('click.smeRq', '.sme-rq-next', function () {
            var x = byId(parseInt($(this).data('id'), 10));
            if (!x) return;
            postJson('/IRF/SaveSMEQuestionReview', payload(x)).always(function () {
                nextUnreviewed(x.id);
                render();
            });
        });
        $root.on('click.smeRq', '.sme-rq-toggle-orig', function (e) {
            e.stopPropagation();
            var id = parseInt($(this).data('id'), 10);
            state.showOriginal[id] = !state.showOriginal[id];
            render();
        });
        $root.on('click.smeRq', '.sme-rq-regen', function (e) {
            e.stopPropagation();
            var id = parseInt($(this).data('id'), 10);
            var x = byId(id);
            if (!x) return;
            x.videoStatus = 1;
            render();
            $.ajax({ url: '/AIInterview/CreateAIQuestionVideo', type: 'POST', data: { id: id }, timeout: 300000 })
                .done(function (res) {
                    if (res && res.success) {
                        x.videoStatus = 2;
                        x.videoUrl = res.videoUrl || x.videoUrl;
                        toast(res.message || 'AI video regenerated from the gist only.', 'success');
                    } else {
                        x.videoStatus = 4;
                        toast((res && res.message) || 'Video generation failed.', 'error');
                    }
                    render();
                })
                .fail(function () { x.videoStatus = 4; toast('Video generation failed.', 'error'); render(); });
        });
        $root.on('click.smeRq', '.sme-rq-gen-more', function (e) {
            e.stopPropagation();
            $('#dmV4GenerateQuestionsBtn').trigger('click');
        });
        $root.on('click.smeRq', '.sme-rq-confirm-skill', function (e) {
            e.stopPropagation();
            var skill = $(this).data('skill');
            postJson('/IRF/ConfirmSMEQuestionsForSkill', { irfId: state.irfId, skillName: skill })
                .done(function (res) {
                    if (res && res.success) {
                        toast((res.confirmed || 0) + ' confirmed' + (res.skipped ? ', ' + res.skipped + ' still blocked' : '') + '.', 'success');
                        loadBundle();
                    } else toast((res && res.message) || 'Could not confirm.', 'error');
                });
        });
        $root.on('click.smeRq', '.sme-rq-approve-set', function () {
            postJson('/IRF/ApproveAICodeInterviewQuestionSet', { irfId: state.irfId, id: parseInt($(this).data('id'), 10) })
                .done(function (res) { if (res && res.success) { toast('Set approved.', 'success'); loadBundle(); } else toast((res && res.message) || 'Could not approve.', 'error'); });
        });
        $root.on('click.smeRq', '.sme-rq-reject-set', function () {
            postJson('/IRF/RejectAICodeInterviewQuestionSet', { irfId: state.irfId, id: parseInt($(this).data('id'), 10) })
                .done(function (res) { if (res && res.success) { toast('Set rejected — behaves as no set.', 'success'); loadBundle(); } });
        });
        $root.on('click.smeRq', '.sme-rq-delete-set', function () {
            if (!window.confirm('Soft-delete this set? Existing interviews keep their frozen questions.')) return;
            postJson('/IRF/DeleteAICodeInterviewQuestionSet', { irfId: state.irfId, id: parseInt($(this).data('id'), 10) })
                .done(function (res) { if (res && res.success) { toast('Set deleted.', 'success'); loadBundle(); } else toast((res && res.message) || 'Could not delete.', 'error'); });
        });
        $root.on('click.smeRq', '.sme-rq-adjust-set', function () {
            var sid = parseInt($(this).data('id'), 10);
            var s = null;
            for (var i = 0; i < state.sets.length; i++) if ((state.sets[i].id || state.sets[i].Id) === sid) s = state.sets[i];
            if (!s) return;
            var ids = s.questionIds || s.QuestionIds || [];
            var lines = ids.map(function (id) {
                var q = byId(id);
                return id + ': ' + ((q && q.title) || 'Question') + (q && q.slot ? ' [' + q.slot + ']' : '');
            }).join('\n');
            var removeId = parseInt(window.prompt('Replace which question?\n' + lines, ids[0] || ''), 10);
            if (!removeId) return;
            var removeQ = byId(removeId);
            var alts = state.questions.filter(function (x) {
                if (x.status !== 1 || x.id === removeId) return false;
                if (ids.indexOf(x.id) >= 0) return false;
                if (removeQ && removeQ.slot && x.slot && x.slot !== removeQ.slot) return false;
                return true;
            });
            if (!alts.length) { toast('No other confirmed question in the same slot.', 'error'); return; }
            var addLines = alts.map(function (x) {
                return x.id + ': ' + (x.title || 'Question') + (x.slot ? ' [' + x.slot + ']' : '');
            }).join('\n');
            var addId = parseInt(window.prompt('Replace with a confirmed question in the same slot:\n' + addLines, alts[0].id), 10);
            if (!addId) return;
            postJson('/IRF/SwapAICodeInterviewQuestionSetItem', { irfId: state.irfId, setId: sid, removeQuestionId: removeId, addQuestionId: addId })
                .done(function (res) { if (res && res.success) { toast('Set adjusted.', 'success'); loadBundle(); } else toast((res && res.message) || 'Could not swap.', 'error'); });
        });
        $root.on('click.smeRq', '#smeRqCreateSet', function () {
            var confirmed = state.questions.filter(function (x) { return x.status === 1; });
            if (!confirmed.length) { toast('Confirm question before creating a set', 'error'); return; }
            var name = window.prompt('Set name', 'Set ' + String.fromCharCode(65 + state.sets.length));
            if (!name) return;
            var ids = confirmed.map(function (x) { return x.id; }).slice(0, 5);
            postJson('/IRF/SaveAICodeInterviewQuestionSet', { irfId: state.irfId, name: name, questionIds: ids })
                .done(function (res) { if (res && res.success) { toast('Set created from confirmed questions.', 'success'); loadBundle(); } else toast((res && res.message) || 'Could not create set.', 'error'); });
        });
        $(document).off('click.smeRqExpert').on('click.smeRqExpert', '#smeRqConfirmExpert', function () {
            postJson('/IRF/SaveExpertInLoopClientConfirmation', { irfId: state.irfId, scope: 0, status: 1 })
                .done(function (res) { if (res && res.success) { toast('Expert confirmed.', 'success'); loadBundle(); } });
        });
        $(document).off('click.smeRqChange').on('click.smeRqChange', '#smeRqChangeExpert', function () {
            var c = window.prompt('Request a different expert. Comment (required):', '');
            if (c === null || !$.trim(c)) { toast('A comment is required.', 'error'); return; }
            postJson('/IRF/SaveExpertInLoopClientConfirmation', { irfId: state.irfId, scope: 0, status: 2, clientComment: $.trim(c) })
                .done(function (res) { if (res && res.success) { toast('Change requested — delivery notified.', 'success'); loadBundle(); } });
        });
        $(document).off('click.smeRqInsights').on('click.smeRqInsights', '.sme-rq-codename', function () {
            var email = ($(this).data('email') || '').toString();
            var userId = parseInt($(this).data('user-id'), 10) || 0;
            var coded = $.trim($(this).text());
            if (window.dmV4ClientShareView === true && userId) {
                $.get('/IRF/GetClientShareInterviewerInsights', {
                    irfId: state.irfId,
                    userId: userId,
                    Type: window.dmV4ClientShareType || 'ublc'
                }, function (data) {
                    if (typeof openInterviewerInsightsModel === 'function') openInterviewerInsightsModel();
                    $('#divInsightsmodel').html(data);
                    $('#txtheaderInsights').text(coded ? coded + ' (Client view)' : 'Expert');
                    $('#hidlastlogin').text($('#LastLogin').val() || '');
                }).fail(function () { toast('Could not load expert insights.', 'error'); });
                return;
            }
            if (email && typeof InsightsDetails === 'function') {
                InsightsDetails(email);
            }
        });
        $(document).off('keydown.smeRqEnter').on('keydown.smeRqEnter', function (e) {
            if (e.key !== 'Enter' || e.shiftKey || e.ctrlKey) return;
            if (!$(e.target).is('textarea, input')) return;
            if (!$(e.target).closest('.sme-rq-qbody.open').length) return;
            var $btn = $('.sme-rq-qbody.open .sme-rq-confirm');
            if ($btn.length && !$btn.prop('disabled')) {
                e.preventDefault();
                $btn.trigger('click');
            }
        });
    }

    window.dmV4SmeReviewEnabled = function () {
        if (window.dmV4ClientShareView === true) return false;
        var panel = $('#dmV4QuestionsPanel');
        var v = panel.attr('data-is-ai-interview');
        return v === '1' || v === 'true' || panel.data('is-ai-interview') === 1 || panel.data('is-ai-interview') === true;
    };
    window.dmV4LoadSmeReview = function () {
        bind();
        return loadBundle();
    };
    window.dmV4LoadExpertLoop = function () {
        state.irfId = parseInt($('#dmV4QuestionsPanel').data('irf-id'), 10) || 0;
        var data = { irfId: state.irfId };
        if (window.dmV4ClientShareView === true) data.Type = window.dmV4ClientShareType || 'ublc';
        return $.post('/IRF/GetSMEQuestionReviewBundle', data).then(function (res) {
            if (!res || !res.success) return;
            state.questions = (res.questions || []).map(mapQuestion);
            state.sets = res.sets || [];
            state.expert = res.expert;
            renderExpert();
        });
    };
})(jQuery);
