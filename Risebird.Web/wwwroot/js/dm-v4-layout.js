/**

 * DM Approval V4 — layout + rubric/interviewer UI (functionality unchanged).

 */

(function ($) {

    'use strict';



    var drawerRowIndex = null;



    function pinSection(bodyId) {

        var $body = $('#' + bodyId);

        if (!$body.length) return;

        $body.removeClass('hidden').show();

        $body.closest('.card').find('label.question').hide();

        $body.closest('.card').find('.expand-panel, .collapsible-panel').hide();

    }



    function syncLevelFormatCheckboxes() {
        $('.dm-v4-lf-format-option').each(function () {
            var $opt = $(this);
            var $cb = $opt.find('.dm-v4-lf-format-cb');
            $opt.toggleClass('is-checked', $cb.is(':checked'));
        });
    }

    function initLevelFormatV4() {
        if (!$('#dm-v4-level-format-grid').length) return;
        syncLevelFormatCheckboxes();
        syncSenioritySelectDefault();
        $(document).off('change.dmV4FormatCb', '.dm-v4-lf-format-cb').on('change.dmV4FormatCb', '.dm-v4-lf-format-cb', function () {
            syncLevelFormatCheckboxes();
        });
    }

    function syncSenioritySelectDefault() {
        var $job = $('#dmV4JobLevel');
        if (!$job.length) return;
        var val = ($job.val() || '').toString();
        if (val === '' || val === '0') {
            $job.val('');
        }
    }

    function moveLevelFormat() {
        initLevelFormatV4();
    }

    window.dmV4SyncFormatCheckboxes = syncLevelFormatCheckboxes;

    var dmToggleTrackHtml = '<span class="dm-v4-dm-toggle__track" aria-hidden="true"><span class="dm-v4-dm-toggle__knob"></span></span>';

    function wrapInputAsDmToggle($input) {
        var $toggle = $('<label class="dm-v4-dm-toggle"></label>');
        $input.detach().appendTo($toggle);
        $toggle.append(dmToggleTrackHtml);
        if ($input.is(':checked')) {
            $toggle.addClass('is-on');
        }
        return $toggle;
    }

    function buildDmToggleButton(isOn) {
        return $(
            '<button type="button" class="dm-v4-dm-toggle dm-v4-dm-toggle--btn' + (isOn ? ' is-on' : '') + '" role="switch" aria-checked="' + (isOn ? 'true' : 'false') + '">' +
            dmToggleTrackHtml +
            '</button>'
        );
    }

    function getEtiquettePanelEl() {
        return $('#dmV4AdvancedPanel');
    }

    function getEtiquetteIrfId() {
        var $panel = getEtiquettePanelEl();
        var id = $panel.data('irf-id') || $panel.attr('data-irf-id') || $('#id').val();
        return id ? parseInt(id, 10) : 0;
    }

    function getEtiquetteRequestData(extra) {
        var data = $.extend({ irfId: getEtiquetteIrfId() }, extra || {});
        if (window.dmV4ClientShareView === true) {
            data.Type = getClientShareTypeParam();
        }
        return data;
    }

    function normalizeEtiquetteState(raw) {
        if (!raw) return null;
        var dress = (raw.dressCode || raw.DressCode || 'formal').toString().toLowerCase();
        var camera = raw.cameraRequired != null ? raw.cameraRequired : raw.CameraRequired;
        var noEating = raw.noEatingRule != null ? raw.noEatingRule : raw.NoEatingRule;
        return {
            dressCode: dress === 'match' ? 'match' : 'formal',
            cameraRequired: camera === true || camera === 'true' || camera === 1 || camera === '1',
            noEatingRule: !(noEating === false || noEating === 'false' || noEating === 0 || noEating === '0')
        };
    }

    function readEtiquetteFromPanelData() {
        var $panel = getEtiquettePanelEl();
        return normalizeEtiquetteState({
            dressCode: $panel.attr('data-etiquette-dress'),
            cameraRequired: $panel.attr('data-etiquette-camera'),
            noEatingRule: $panel.attr('data-etiquette-no-eating')
        });
    }

    function syncEtiquettePanelDataAttrs(state) {
        var normalized = normalizeEtiquetteState(state);
        if (!normalized) return;
        var $panel = getEtiquettePanelEl();
        $panel.attr('data-etiquette-dress', normalized.dressCode);
        $panel.attr('data-etiquette-camera', normalized.cameraRequired ? '1' : '0');
        $panel.attr('data-etiquette-no-eating', normalized.noEatingRule ? '1' : '0');
    }

    function applyEtiquettePanelState(state) {
        var normalized = normalizeEtiquetteState(state);
        if (!normalized) return;
        var $card = $('.dm-v4-advanced-card--etiquette');
        if (!$card.length) return;
        $card.find('.dm-v4-etiquette-option[data-etiquette="dress"]').each(function () {
            var value = ($(this).attr('data-dress-value') || 'formal').toLowerCase();
            $(this).toggleClass('is-on', value === normalized.dressCode);
        });
        var $camera = $card.find('.dm-v4-etiquette-list li[data-rule="camera"] .dm-v4-dm-toggle--btn').first();
        var $noEating = $card.find('.dm-v4-etiquette-list li[data-rule="no-eating"] .dm-v4-dm-toggle--btn').first();
        if ($camera.length) {
            $camera.toggleClass('is-on', normalized.cameraRequired).attr('aria-checked', normalized.cameraRequired ? 'true' : 'false');
        }
        if ($noEating.length) {
            $noEating.toggleClass('is-on', normalized.noEatingRule).attr('aria-checked', normalized.noEatingRule ? 'true' : 'false');
        }
        syncEtiquettePanelDataAttrs(normalized);
    }

    function readEtiquetteFromDom() {
        var $card = $('.dm-v4-advanced-card--etiquette');
        var dressCode = 'formal';
        $card.find('.dm-v4-etiquette-option[data-etiquette="dress"].is-on').each(function () {
            dressCode = ($(this).attr('data-dress-value') || 'formal').toLowerCase() === 'match' ? 'match' : 'formal';
        });
        var $camera = $card.find('.dm-v4-etiquette-list li[data-rule="camera"] .dm-v4-dm-toggle--btn').first();
        var $noEating = $card.find('.dm-v4-etiquette-list li[data-rule="no-eating"] .dm-v4-dm-toggle--btn').first();
        return {
            dressCode: dressCode,
            cameraRequired: $camera.hasClass('is-on'),
            noEatingRule: $noEating.hasClass('is-on')
        };
    }

    function saveEtiquetteToServer(state) {
        var normalized = normalizeEtiquetteState(state);
        if (!normalized || !getEtiquetteIrfId()) return $.Deferred().resolve({ success: false }).promise();
        return $.post('/IRF/UpdateDmInterviewerEtiquette', getEtiquetteRequestData({
            dressCode: normalized.dressCode,
            cameraRequired: normalized.cameraRequired,
            noEatingRule: normalized.noEatingRule
        })).then(function (res) {
            if (res && res.success) {
                syncEtiquettePanelDataAttrs(normalized);
            }
            return res;
        });
    }

    function loadEtiquetteFromServer() {
        if (!getEtiquetteIrfId()) return $.Deferred().resolve().promise();
        return $.post('/IRF/GetDmInterviewerEtiquetteJson', getEtiquetteRequestData()).then(function (res) {
            if (res && res.success && res.etiquette) {
                applyEtiquettePanelState(res.etiquette);
            }
        });
    }

    function buildAdvancedNested() {
        var $target = $('#dm-v4-advanced-nested');
        if (!$target.length) return;
        $target.empty();

        var $checksSource = $('#hidden_fields_eleven');
        var $checksCard = $('<div class="dm-v4-advanced-card"></div>');
        $checksCard.append(
            '<div class="dm-v4-advanced-card__head">' +
            '<span class="dm-v4-advanced-card__title">Candidate checks</span>' +
            '<span class="dm-v4-badge dm-v4-badge--green dm-v4-badge--xs">DEFAULTS ON</span></div>'
        );
        var $checksInner = $('<div class="dm-v4-advanced-card__body dm-v4-advanced-checks"></div>');
        var $checksList = $('<ul class="dm-v4-advanced-list"></ul>');

        var $consentForm = $checksSource.find('#divconcentform').first();
        if ($consentForm.length) {
            $checksInner.append($('<div class="dm-v4-advanced-consent-source" aria-hidden="true"></div>').append($consentForm.detach()));
        }

        var $consentRow = $('<li class="dm-v4-advanced-toggle-row dm-v4-advanced-toggle-row--consent"></li>');
        $consentRow.append(
            '<span class="dm-v4-advanced-consent-label">' +
            '<span class="dm-v4-advanced-consent-text">Job consent acknowledgement</span>' +
            '<button type="button" class="dm-v4-consent-edit" title="Edit consent form" aria-label="Edit consent form">' +
            '<i class="fa fa-pencil"></i></button>' +
            '</span>' +
            '<span class="dm-v4-dm-toggle is-on dm-v4-dm-toggle--static" aria-hidden="true">' + dmToggleTrackHtml + '</span>'
        );
        $checksList.append($consentRow);

        var $idCb = $checksSource.find('#CandidateIdverification');
        var $snippetCb = $checksSource.find('#CodeSnippetRequired');
        if ($idCb.length) {
            var $row1 = $('<li class="dm-v4-advanced-toggle-row"></li>');
            $row1.append('<span>Candidate ID verification</span>').append(wrapInputAsDmToggle($idCb));
            $checksList.append($row1);
        }
        if ($snippetCb.length) {
            var $row2 = $('<li class="dm-v4-advanced-toggle-row"></li>');
            $row2.append('<span>Coded snippet evaluation</span>').append(wrapInputAsDmToggle($snippetCb));
            $checksList.append($row2);
        }
        $checksInner.append($checksList);
        $checksCard.append($checksInner);
        $target.append($checksCard);

        var etiquetteState = readEtiquetteFromPanelData() || {
            dressCode: 'formal',
            cameraRequired: true,
            noEatingRule: true
        };

        var $etiquetteCard = $(
            '<div class="dm-v4-advanced-card dm-v4-advanced-card--etiquette">' +
            '<div class="dm-v4-advanced-card__head"><span class="dm-v4-advanced-card__title">Interviewer etiquette</span>' +
            '<span class="dm-v4-badge dm-v4-badge--green dm-v4-badge--xs">DEFAULTS ON</span></div>' +
            '<div class="dm-v4-advanced-card__body">' +
            '<div class="dm-v4-etiquette-label">Dress code</div>' +
            '<label class="dm-v4-etiquette-option' + (etiquetteState.dressCode === 'formal' ? ' is-on' : '') + '" data-etiquette="dress" data-dress-value="formal"><span class="dm-v4-etiquette-radio"></span><span><strong>Strict formal</strong><span class="dm-v4-etiquette-sub">Business attire · neutral background</span></span></label>' +
            '<label class="dm-v4-etiquette-option' + (etiquetteState.dressCode === 'match' ? ' is-on' : '') + '" data-etiquette="dress" data-dress-value="match"><span class="dm-v4-etiquette-radio"></span><span><strong>Interviewer\'s choice — match candidate</strong><span class="dm-v4-etiquette-sub">Mirror role\'s expected level</span></span></label>' +
            '<ul class="dm-v4-etiquette-list"></ul></div></div>'
        );
        var $etiquetteList = $etiquetteCard.find('.dm-v4-etiquette-list');
        $etiquetteList.append(
            $('<li data-etiquette="rule" data-rule="camera"></li>').append('<span>Camera-on with neutral background required</span>').append(buildDmToggleButton(etiquetteState.cameraRequired))
        );
        $etiquetteList.append(
            $('<li data-etiquette="rule" data-rule="no-eating"></li>').append('<span>No eating, walking or smoking during interview</span>').append(buildDmToggleButton(etiquetteState.noEatingRule))
        );
        $target.append($etiquetteCard);

        var $flowSource = $('#hidden_fields_nine');
        var $video = $flowSource.find('#vidvideoinstruction').first();
        var irfId = $('#id').val();
        var $flowCard = $('<div class="dm-v4-advanced-card dm-v4-advanced-card--full"></div>');
        $flowCard.append(
            '<div class="dm-v4-advanced-card__head">' +
            '<span class="dm-v4-advanced-card__title">Interview flow</span>' +
            '<span class="dm-v4-badge dm-v4-badge--neutral dm-v4-badge--xs">OPTIONAL</span></div>'
        );
        var $flowGrid = $('<div class="dm-v4-flow-grid"></div>');
        var $flowVideo = $(
            '<div class="dm-v4-flow-block">' +
            '<div class="dm-v4-flow-block__title">Flow video <span class="dm-v4-badge dm-v4-badge--neutral dm-v4-badge--xs">OPTIONAL</span></div>' +
            '<div class="dm-v4-flow-block__sub">Played for interviewers before the live session.</div>' +
            '<div class="dm-v4-flow-block__media"></div>' +
            '<button type="button" class="dm-v4-btn dm-v4-btn--ghost dm-v4-btn--sm dm-v4-flow-upload-btn"><i class="fa fa-upload"></i> Upload</button>' +
            '</div>'
        );
        if ($video.length) {
            $video.addClass('dm-v4-flow-video');
            $flowVideo.find('.dm-v4-flow-block__media').append($video.detach());
        }
        $flowVideo.find('.dm-v4-flow-upload-btn').attr('data-irf-id', irfId);
        var $flowDoc = $(
            '<div class="dm-v4-flow-block">' +
            '<div class="dm-v4-flow-block__title">Interview flow document <span class="dm-v4-badge dm-v4-badge--neutral dm-v4-badge--xs">OPTIONAL</span></div>' +
            '<div class="dm-v4-flow-block__sub">Structured agenda, calibration notes and pass-the-rubric steps.</div>' +
            '<div class="dm-v4-flow-doc-placeholder"><i class="fa fa-file-pdf-o"></i> No document uploaded</div>' +
            '</div>'
        );
        $flowGrid.append($flowVideo).append($flowDoc);
        $flowCard.append($flowGrid);
        $target.append($flowCard);

        $('.dm-v4-legacy-advanced-source, .dm-v4-legacy-flow-source').hide();
    }

    function initAdvancedV4() {
        $(document).off('click.dmV4Consent', '.dm-v4-consent-edit').on('click.dmV4Consent', '.dm-v4-consent-edit', function () {
            if (typeof openPopupConsentModal === 'function') openPopupConsentModal();
        });
        $(document).off('click.dmV4FlowUp', '.dm-v4-flow-upload-btn').on('click.dmV4FlowUp', '.dm-v4-flow-upload-btn', function () {
            var irfId = $(this).data('irf-id') || $('#id').val();
            if (typeof OpenVideoInstructionModal === 'function') OpenVideoInstructionModal(irfId);
        });
        $(document).off('change.dmV4AdvCheck', '#dm-v4-advanced-nested input[type=checkbox]').on('change.dmV4AdvCheck', '#dm-v4-advanced-nested input[type=checkbox]', function () {
            $(this).closest('.dm-v4-dm-toggle').toggleClass('is-on', this.checked);
            var id = this.id;
            if (id && typeof ChangeCandidateCheck === 'function') {
                ChangeCandidateCheck(id);
            }
        });
        $(document).off('click.dmV4EtiquetteDress', '.dm-v4-etiquette-option[data-etiquette="dress"]').on('click.dmV4EtiquetteDress', '.dm-v4-etiquette-option[data-etiquette="dress"]', function () {
            var $card = $(this).closest('.dm-v4-advanced-card--etiquette');
            var previous = readEtiquetteFromDom();
            $card.find('.dm-v4-etiquette-option[data-etiquette="dress"]').removeClass('is-on');
            $(this).addClass('is-on');
            var next = readEtiquetteFromDom();
            if (next.dressCode !== previous.dressCode) {
                saveEtiquetteToServer(next);
            }
        });
        $(document).off('click.dmV4EtiquetteRule', '.dm-v4-etiquette-list .dm-v4-dm-toggle--btn').on('click.dmV4EtiquetteRule', '.dm-v4-etiquette-list .dm-v4-dm-toggle--btn', function () {
            var on = !$(this).hasClass('is-on');
            $(this).toggleClass('is-on', on).attr('aria-checked', on ? 'true' : 'false');
            saveEtiquetteToServer(readEtiquetteFromDom());
        });
    }

    function initDmV4Collapses() {
        $(document).off('click.dmV4Collapse', '.dm-v4-collapse__head').on('click.dmV4Collapse', '.dm-v4-collapse__head', function () {
            var $collapse = $(this).closest('.dm-v4-collapse');
            var $panel = $collapse.find('.dm-v4-collapse__body').first();
            var open = !$collapse.hasClass('is-open');
            $collapse.toggleClass('is-open', open);
            $(this).attr('aria-expanded', open ? 'true' : 'false');
            if (open) {
                $panel.prop('hidden', false).slideDown(180);
                if ($collapse.attr('id') === 'dmV4AdvancedCollapse') {
                    loadEtiquetteFromServer();
                }
            } else {
                $panel.slideUp(180, function () { $panel.prop('hidden', true); });
            }
        });
    }



    function syncElimWrap($row) {

        var $cb = $row.find('.dm-v4-elim-input').first();

        if (!$cb.length) return;

        var on = $cb.is(':checked');

        var $wrap = $row.find('.dm-v4-elim-wrap');

        $wrap.find('[data-elim-label="yes"]').toggleClass('is-on', on);

        $wrap.find('[data-elim-label="no"]').toggleClass('is-on', !on);

        $row.toggleClass('dm-v4-rubric-row--elim', on);

    }



    function updateRubricTotals() {

        var $rows = $('#SkillTable tbody tr.dm-v4-rubric-row');

        var weight = 0;

        var time = 0;

        var elim = 0;

        $rows.each(function () {

            var $r = $(this);

            var w = parseInt($r.find('[id$="__Weightage"]').val(), 10);

            var t = parseInt($r.find('[id$="__Timelimit"]').val(), 10);

            if (!isNaN(w)) weight += w;

            if (!isNaN(t)) time += t;

            if ($r.find('.dm-v4-elim-input').is(':checked')) elim += 1;

        });

        var n = $rows.length;

        $('#dmV4SkillCountBadge').text(n + (n === 1 ? ' SKILL' : ' SKILLS'));

        $('#dmV4WeightTotal').text(weight + '%');

        $('#dmV4TimeTotal').text(time + 'm');

        $('#dmV4ElimTotal').text(elim + (elim === 1 ? ' elim' : ' elim'));

        var $pill = $('#dmV4WeightPill');

        if ($pill.length) {

            $pill.text('Total weightage ' + weight + '/100');

            var hasPending = $pill.hasClass('dm-v4-weight-pill--pending');

            $pill.toggleClass('dm-v4-weight-pill--ok', weight === 100 && !hasPending);

            $pill.toggleClass('dm-v4-weight-pill--warn', weight !== 100);

            if (weight !== 100) {
                $pill.removeClass('dm-v4-weight-pill--pending');
            }

        }

    }



    function reindexRubricRows() {

        $('#SkillTable tbody tr.dm-v4-rubric-row').each(function (i) {

            var $row = $(this);

            $row.attr('data-row-index', i);

            $row.find('.dm-v4-rubric-details-btn').attr('data-row-index', i);

            $row.find('input, select, textarea').each(function () {

                var name = this.getAttribute('name');

                if (!name) return;

                var match = /^SkillCollection\[\d+\]\.(.+)$/.exec(name);

                if (!match) return;

                var prop = match[1];

                this.setAttribute('name', 'SkillCollection[' + i + '].' + prop);

                if (this.type === 'hidden' && prop === 'isEliminationskill') {

                    return;

                }

                this.setAttribute('id', 'SkillCollection_' + i + '__' + prop);

            });

        });

    }



    function dmV4PrepareRubricFormForSubmit() {

        $('#SkillTable input.dm-v4-inline-edit-input:visible').each(function () {

            $(this).trigger('blur');

        });

        if (typeof closeDrawer === 'function' && drawerRowIndex !== null) {

            closeDrawer(true);

        }

        reindexRubricRows();

    }



    window.dmV4PrepareRubricFormForSubmit = dmV4PrepareRubricFormForSubmit;



    window.dmV4OnRubricFormSubmit = function () {

        if (typeof dmV4PrepareRubricFormForSubmit === 'function') {

            dmV4PrepareRubricFormForSubmit();

        }

        return typeof CheckConditions === 'function' ? CheckConditions() : true;

    };



    function getRowFields(index) {

        var prefix = 'SkillCollection_' + index + '__';

        return {

            skill: $('#' + prefix + 'SkillName'),

            concepts: $('#' + prefix + 'Concepts'),

            expectations: $('#' + prefix + 'Natureofquestion')

        };

    }



    function openDrawer(index) {

        var f = getRowFields(index);

        if (!f.skill.length) return;

        drawerRowIndex = index;

        $('#dmV4DrawerTitle').text(f.skill.val() || 'Skill ' + (index + 1));

        $('#dmV4DrawerEyebrow').text('Row ' + (index + 1));

        $('#dmV4DrawerConcepts').val(f.concepts.val() || '');

        $('#dmV4DrawerExpectations').val(f.expectations.val() || '');

        $('#dmV4RubricOverlay, #dmV4RubricDrawer').addClass('is-open').attr('aria-hidden', 'false');

    }



    function closeDrawer(save) {

        if (save && drawerRowIndex !== null) {

            var f = getRowFields(drawerRowIndex);

            if (f.concepts.length) f.concepts.val($('#dmV4DrawerConcepts').val());

            if (f.expectations.length) f.expectations.val($('#dmV4DrawerExpectations').val());

        }

        drawerRowIndex = null;

        $('#dmV4RubricOverlay, #dmV4RubricDrawer').removeClass('is-open').attr('aria-hidden', 'true');

    }



    function dmV4SaveRubricDrawerAndSubmit() {

        closeDrawer(true);

        var form = document.getElementById('irfForm');

        if (!form) return;

        if (typeof dmV4OnRubricFormSubmit === 'function') {

            if (dmV4OnRubricFormSubmit()) {

                form.submit();

            }

            return;

        }

        form.submit();

    }



    window.dmV4SaveRubricDrawerAndSubmit = dmV4SaveRubricDrawerAndSubmit;



    var DM_V4_SKILL_EDIT_SVG = '<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.4" width="11" height="11">' +
        '<path d="M3 13l.7-3 6.6-6.6a1 1 0 011.4 0l1 1a1 1 0 010 1.4L6.1 12.3 3 13z" stroke-linejoin="round"/></svg>';



    function dmV4EscapeHtml(s) {

        return String(s || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');

    }



    function buildEditableSkillHtml(value) {

        var v = $.trim(value);

        var text = v

            ? '<span class="dm-v4-editable-skill__text">' + dmV4EscapeHtml(v) + '</span>'

            : '<span class="dm-v4-editable-skill__text"><span class="dm-v4-editable-skill__placeholder">Untitled skill</span></span>';

        return '<button type="button" class="dm-v4-editable-skill" title="Click to edit">' + text +

            '<span class="dm-v4-editable-skill__icon" aria-hidden="true">' + DM_V4_SKILL_EDIT_SVG + '</span></button>';

    }



    function syncEditableSkillButton($btn, value) {

        var $fresh = $(buildEditableSkillHtml(value));

        $btn.html($fresh.html()).attr('title', 'Click to edit');

    }



    function startEditableSkillEdit($btn, $input) {

        if ($btn.data('dm-v4-editing')) return;

        $btn.data('dm-v4-editing', true);

        var orig = $.trim($input.val());

        $btn.hide();

        $input.addClass('dm-v4-inline-edit-input dm-v4-inline-edit-input--skill').show().focus().select();



        var finished = false;

        var finish = function (commit) {

            if (finished) return;

            finished = true;

            var v = commit ? $.trim($input.val()) : orig;

            if (!commit) $input.val(orig);

            else $input.val(v);

            $input.hide().removeClass('dm-v4-inline-edit-input dm-v4-inline-edit-input--skill');

            syncEditableSkillButton($btn, $input.val());

            $btn.show().data('dm-v4-editing', false);

            if (commit) {

                $input.trigger('change');

                var $row = $input.closest('tr');

                var idx = parseInt($row.attr('data-row-index'), 10);

                if (!isNaN(idx) && drawerRowIndex === idx) {

                    $('#dmV4DrawerTitle').text($.trim($input.val()) || ('Skill ' + (idx + 1)));

                }

            }

        };



        $input.off('blur.dmV4SkillEdit keydown.dmV4SkillEdit');

        $input.on('blur.dmV4SkillEdit', function () { finish(true); });

        $input.on('keydown.dmV4SkillEdit', function (e) {

            if (e.key === 'Enter') {

                e.preventDefault();

                finish(true);

            }

            if (e.key === 'Escape') {

                e.preventDefault();

                finish(false);

            }

        });

    }



    function buildFieldPillHtml(value, kind) {

        var n = parseInt(value, 10);

        if (isNaN(n)) n = 0;

        var suffix = kind === 'weight' ? '%' : 'm';

        var w = kind === 'weight' ? 80 : 70;

        return '<button type="button" class="dm-v4-field-pill dm-v4-field-pill--' + kind + '" data-kind="' + kind + '" data-value="' + n + '" title="Click to edit" style="max-width:' + w + 'px">' + n + suffix + '</button>';

    }



    function syncFieldPill($pill, value, kind) {

        var n = parseInt(value, 10);

        if (isNaN(n)) n = 0;

        var suffix = kind === 'weight' ? '%' : 'm';

        $pill.attr('data-value', n).text(n + suffix);

    }



    function startFieldPillEdit($pill) {

        if ($pill.data('dm-v4-editing')) return;

        $pill.data('dm-v4-editing', true);

        var kind = $pill.data('kind');

        var $td = $pill.closest('td');

        var $input = $td.find(kind === 'weight' ? '[id$="__Weightage"]' : '[id$="__Timelimit"]').first();

        if (!$input.length) {

            $pill.data('dm-v4-editing', false);

            return;

        }

        var orig = parseInt($input.val(), 10) || 0;

        var min = parseInt($input.attr('min'), 10);

        var max = parseInt($input.attr('max'), 10);

        var $edit = $('<input type="number" class="dm-v4-inline-edit-input dm-v4-inline-edit-input--field" />').val(orig);

        if (!isNaN(min)) $edit.attr('min', min);

        if (!isNaN(max)) $edit.attr('max', max);

        if (kind === 'weight') $edit.css('max-width', '80px');

        else $edit.css('max-width', '70px');



        $pill.hide();

        $pill.after($edit);

        $edit.focus().select();



        var finished = false;

        var finish = function (commit) {

            if (finished) return;

            finished = true;

            var n = commit ? (parseInt($edit.val(), 10) || 0) : orig;

            if (commit && !isNaN(min)) n = Math.max(min, n);

            if (commit && !isNaN(max)) n = Math.min(max, n);

            $edit.remove();

            $pill.data('dm-v4-editing', false).show();

            if (commit) {

                $input.val(n).trigger('change');

                syncFieldPill($pill, n, kind);

                if (kind === 'weight') updateRubricTotals();

            } else {

                syncFieldPill($pill, orig, kind);

            }

        };



        $edit.on('blur.dmV4FieldEdit', function () { finish(true); });

        $edit.on('keydown.dmV4FieldEdit', function (e) {

            if (e.key === 'Enter') {

                e.preventDefault();

                finish(true);

            }

            if (e.key === 'Escape') {

                e.preventDefault();

                finish(false);

            }

        });

    }



    var DM_V4_LEVEL_CHEV = '<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M4 6.5l4 4 4-4" stroke-linecap="round" stroke-linejoin="round"/></svg>';



    function rubricLevelDisplayClass(label) {

        var key = String(label || '').trim().toLowerCase().replace(/\s+/g, '');

        if (!key || key === 'select') return 'dm-v4-level-chip-display--select';

        return 'dm-v4-level-chip-display--' + key;

    }



    function syncRubricLevelChip($wrap) {

        var $sel = $wrap.find('.dm-v4-level-select');

        var $disp = $wrap.find('.dm-v4-level-chip-display');

        if (!$sel.length || !$disp.length) return;

        var label = $sel.find('option:selected').text().trim() || 'Select';

        $disp.attr('class', 'dm-v4-level-chip-display ' + rubricLevelDisplayClass(label));

        $disp.find('.dm-v4-level-chip__label').text(label);

    }



    function enhanceRubricLevelSelects() {

        $('#SkillTable .dm-v4-level-chip-wrap').each(function () {

            var $wrap = $(this);

            var $sel = $wrap.find('.dm-v4-level-select');

            if (!$sel.length) return;

            if (!$wrap.find('.dm-v4-level-chip-display').length) {

                $sel.before(

                    '<span class="dm-v4-level-chip-display" aria-hidden="true">' +

                    '<span class="dm-v4-level-chip__label"></span>' +

                    '<span class="dm-v4-level-chip__chev">' + DM_V4_LEVEL_CHEV + '</span></span>'

                );

            }

            syncRubricLevelChip($wrap);

        });

    }



    function enhanceRubricEditableUI() {

        $('#SkillTable tbody tr.dm-v4-rubric-row').each(function () {

            var $row = $(this);



            var $skillTd = $row.find('td[data-col="skill"]');

            var $skillInput = $skillTd.find('[id$="__SkillName"]').first();

            if ($skillInput.length) {

                $skillInput.addClass('dm-v4-rubric-skill-input');

                var $skillBtn = $skillTd.find('.dm-v4-editable-skill');

                if (!$skillBtn.length) {

                    $skillInput.after(buildEditableSkillHtml($skillInput.val()));

                } else {

                    syncEditableSkillButton($skillBtn, $skillInput.val());

                }

            }



            var $weightTd = $row.find('td[data-col="weight"]');

            var $weightInput = $weightTd.find('[id$="__Weightage"]').first();

            if ($weightInput.length) {

                $weightInput.addClass('dm-v4-rubric-field-input');

                var $weightPill = $weightTd.find('.dm-v4-field-pill--weight');

                if (!$weightPill.length) {

                    $weightInput.after(buildFieldPillHtml($weightInput.val(), 'weight'));

                } else {

                    syncFieldPill($weightPill, $weightInput.val(), 'weight');

                }

            }



            var $timeTd = $row.find('td[data-col="time"]');

            var $timeInput = $timeTd.find('[id$="__Timelimit"]').first();

            if ($timeInput.length) {

                $timeInput.addClass('dm-v4-rubric-field-input');

                var $timePill = $timeTd.find('.dm-v4-field-pill--time');

                if (!$timePill.length) {

                    $timeInput.after(buildFieldPillHtml($timeInput.val(), 'time'));

                } else {

                    syncFieldPill($timePill, $timeInput.val(), 'time');

                }

            }

        });

    }



    function initRubricV4() {

        $('#SkillTable tbody tr').addClass('dm-v4-rubric-row');

        $('#SkillTable tbody tr.dm-v4-rubric-row').each(function () {

            syncElimWrap($(this));

        });

        reindexRubricRows();

        updateRubricTotals();



        $(document).off('click.dmV4Details', '.dm-v4-rubric-details-btn').on('click.dmV4Details', '.dm-v4-rubric-details-btn', function (e) {

            e.preventDefault();

            openDrawer(parseInt($(this).attr('data-row-index'), 10) || 0);

        });



        $(document).off('click.dmV4Elim', '.dm-v4-elim-wrap [data-elim-label]').on('click.dmV4Elim', '.dm-v4-elim-wrap [data-elim-label]', function (e) {

            e.preventDefault();

            var $wrap = $(this).closest('.dm-v4-elim-wrap');

            var $row = $wrap.closest('tr');

            var $cb = $wrap.find('.dm-v4-elim-input');

            var yes = $(this).attr('data-elim-label') === 'yes';

            $cb.prop('checked', yes);

            syncElimWrap($row);

            updateRubricTotals();

        });



        $(document).off('change.dmV4Weight', '#SkillTable .dm-v4-rubric-weight, #SkillTable [id$="__Timelimit"], #SkillTable .dm-v4-elim-input')

            .on('change.dmV4Weight', '#SkillTable .dm-v4-rubric-weight, #SkillTable [id$="__Timelimit"], #SkillTable .dm-v4-elim-input', updateRubricTotals);



        $(document).off('change.dmV4Level', '#SkillTable .dm-v4-level-select')

            .on('change.dmV4Level', '#SkillTable .dm-v4-level-select', function () {

                syncRubricLevelChip($(this).closest('.dm-v4-level-chip-wrap'));

            });



        $('#dmV4DrawerClose, #dmV4DrawerHeadClose, #dmV4RubricOverlay').off('click.dmV4Drawer').on('click.dmV4Drawer', function () {

            closeDrawer(false);

        });

        $('#dmV4DrawerSave').off('click.dmV4Drawer').on('click.dmV4Drawer', function () {

            dmV4SaveRubricDrawerAndSubmit();

        });



        $(document).off('click.dmV4SkillEdit', '.dm-v4-editable-skill').on('click.dmV4SkillEdit', '.dm-v4-editable-skill', function (e) {

            e.preventDefault();

            var $btn = $(this);

            var $input = $btn.siblings('.dm-v4-rubric-skill-input, [id$="__SkillName"]').first();

            if ($input.length) startEditableSkillEdit($btn, $input);

        });



        $(document).off('click.dmV4FieldPill', '.dm-v4-field-pill').on('click.dmV4FieldPill', '.dm-v4-field-pill', function (e) {

            e.preventDefault();

            startFieldPillEdit($(this));

        });



        enhanceRubricLevelSelects();

        enhanceRubricEditableUI();

    }



    function isFreeChannelPanel() {
        return $('#dmV4IvPanel').attr('data-lock') !== '1';
    }

    function isClientShareView() {
        if (window.dmV4ClientShareView === true) return true;
        var $panel = $('#dmV4IvPanel');
        if ($panel.attr('data-client-share') === '1') return true;
        var $page = $('.dm-v4-page');
        return $page.attr('data-client-share') === '1';
    }

    function isSmeView() {
        if (window.dmV4SmeView === true) return true;
        var $panel = $('#dmV4IvPanel');
        if ($panel.attr('data-sme-view') === '1') return true;
        var $page = $('.dm-v4-page');
        return $page.attr('data-sme-view') === '1';
    }

    function isPanelCompactView() {
        return isClientShareView() || isSmeView();
    }

    function getClientShareTypeParam() {
        return (window.dmV4ClientShareType || 'ublc').toString();
    }

    function getIvCanEditClientVet() {
        var $panel = $('#dmV4IvPanel');
        return $panel.attr('data-can-edit-client-vet') === '1' || $panel.data('can-edit-client-vet') === 1;
    }

    function readRbVetFromRow($row) {
        if (!readRowDbActive($row)) return false;
        var raw = $row.attr('data-rb-vet');
        if (raw === '1') return true;
        if (raw === '0' && isFreeChannelPanel()
            && ($row.attr('data-is-available') === '1' || $row.attr('data-is-available') == null)
            && ($row.attr('data-client-action') || '').toLowerCase() !== 'rejected') {
            return true;
        }
        return false;
    }

    function readClientVetFromRow($row) {
        if (!readRowDbActive($row)) return false;
        var raw = $row.attr('data-client-vet');
        if (raw === '1') return true;
        var action = ($row.attr('data-client-action') || '').toLowerCase();
        if (action === 'rejected') return false;
        if (raw === '0' && isFreeChannelPanel() && readRbVetFromRow($row)
            && ($row.attr('data-is-available') === '1' || $row.attr('data-is-available') == null)) {
            return true;
        }
        return false;
    }

    function refreshIvBadges() {
        var $rows = $('.dm-v4-iv-row');
        var n = $rows.length;
        var rb = 0;
        var client = 0;
        var panel = 0;
        $rows.each(function () {
            var $row = $(this);
            if (!readRowDbActive($row)) return;
            var rbOn = readRbVetFromRow($row);
            var clientOn = readClientVetFromRow($row);
            if (rbOn) rb += 1;
            if (clientOn) client += 1;
            if (rbOn && clientOn) panel += 1;
        });
        $('#dmV4IvCountBadge').text(n + (n === 1 ? ' CANDIDATE' : ' CANDIDATES'));
        $('#dmV4RbVetBadge').text(rb + ' RB-VETTED');
        $('#dmV4ClientVetBadge').text(client + ' CLIENT-APPROVED');
        $('#dmV4OnPanelBadge').text(panel + ' ON PANEL');
        refreshIvTabCounts();
        applyIvTabFilter();
    }

    function readRowDbActive($row) {
        return ($row.attr('data-is-active') || '0') === '1';
    }

    function getIvTab() {
        var tab = ($('#dmV4IvPanel').attr('data-iv-tab') || 'active').toString();
        return tab === 'inactive' ? 'inactive' : 'active';
    }

    function setIvTab(tab) {
        tab = tab === 'inactive' ? 'inactive' : 'active';
        $('#dmV4IvPanel').attr('data-iv-tab', tab);
        $('#dmV4IvTabs .dm-v4-iv-tab').each(function () {
            var on = ($(this).attr('data-tab') || '') === tab;
            $(this).toggleClass('is-on', on).attr('aria-selected', on ? 'true' : 'false');
        });
        if (tab === 'inactive') {
            setActiveSubTab('all');
            $('#dmV4IvPanel .dm-v4-iv-row').removeClass('dm-v4-iv-row--subtab-hidden');
            refreshInactiveSubTabCounts();
            applyInactiveSubTabFilter();
        } else {
            setInactiveSubTab('all');
            $('#dmV4IvPanel .dm-v4-iv-row').removeClass('dm-v4-iv-row--subtab-hidden');
            refreshActiveSubTabCounts();
            applyActiveSubTabFilter();
        }
        applyIvTabFilter();
    }

    function refreshIvTabCounts() {
        var active = 0;
        var inactive = 0;
        $('#dmV4IvPanel .dm-v4-iv-row').each(function () {
            if (readRowDbActive($(this))) active += 1;
            else inactive += 1;
        });
        $('#dmV4IvTabActiveCount').text(active);
        $('#dmV4IvTabInactiveCount').text(inactive);
        refreshInactiveSubTabCounts();
        refreshActiveSubTabCounts();
    }

    function applyIvTabFilter() {
        var $panel = $('#dmV4IvPanel');
        if (!$panel.length || !$('#dmV4IvTabs').length) return;
        var tab = getIvTab();
        $panel.find('.dm-v4-iv-row').each(function () {
            var $row = $(this);
            var isActive = readRowDbActive($row);
            var show = tab === 'active' ? isActive : !isActive;
            $row.toggleClass('dm-v4-iv-row--tab-hidden', !show);
        });
        if (tab === 'inactive') applyInactiveSubTabFilter();
        else if (tab === 'active') applyActiveSubTabFilter();
        else updateIvTabEmptyMessage();
    }

    function initIvTabs() {
        if (!$('#dmV4IvTabs').length) return;
        $(document).off('click.dmV4IvTab', '#dmV4IvTabs .dm-v4-iv-tab').on('click.dmV4IvTab', '#dmV4IvTabs .dm-v4-iv-tab', function (e) {
            e.preventDefault();
            var tab = ($(this).attr('data-tab') || 'active').toString();
            if (tab === getIvTab()) return;
            setIvTab(tab);
        });
    }

    function ivStatusBadgeFromRow($row) {
        var isActive = readRowDbActive($row);
        var isAvailable = ($row.attr('data-is-available') || '0') === '1';
        var clientAction = ($row.attr('data-client-action') || '').toLowerCase();
        if (isActive) return { label: 'Active', tone: 'active' };
        if (isAvailable && clientAction !== 'rejected') return { label: 'Available', tone: 'available' };
        return { label: 'Inactive', tone: 'inactive' };
    }

    // ── Active sub-tabs ─────────────────────────────────────────────────────

    function rowHasBadQualityFlag($row) {
        if (!readRowDbActive($row)) return false;
        var verdict = ($row.attr('data-auto-qc-verdict') || '').trim();
        return verdict.length > 0;
    }

    function getActiveSubTab() {
        var val = ($('#dmV4IvActiveSubTabs').attr('data-active-subtab') || 'all').toString();
        return val || 'all';
    }

    function setActiveSubTab(subtab) {
        subtab = subtab || 'all';
        $('#dmV4IvActiveSubTabs').attr('data-active-subtab', subtab);
        $('#dmV4IvActiveSubTabs .dm-v4-iv-subtab').each(function () {
            var on = ($(this).attr('data-active-subtab') || '') === subtab;
            $(this).toggleClass('is-on', on).attr('aria-selected', on ? 'true' : 'false');
        });
        applyActiveSubTabFilter();
    }

    function refreshActiveSubTabCounts() {
        var counts = { all: 0, 'bad-quality': 0 };
        $('#dmV4IvPanel .dm-v4-iv-row').each(function () {
            var $row = $(this);
            if (!readRowDbActive($row)) return;
            counts.all += 1;
            if (rowHasBadQualityFlag($row)) counts['bad-quality'] += 1;
        });
        $('#dmV4ActiveSubTabAllCount').text(counts.all);
        $('#dmV4ActiveSubTabBadQualityCount').text(counts['bad-quality']);
    }

    function applyActiveSubTabFilter() {
        if (getIvTab() !== 'active') {
            $('#dmV4IvPanel .dm-v4-iv-row').removeClass('dm-v4-iv-row--subtab-hidden');
            return;
        }
        var subtab = getActiveSubTab();
        $('#dmV4IvPanel .dm-v4-iv-row').each(function () {
            var $row = $(this);
            if (!readRowDbActive($row)) return;
            var show = subtab === 'all' || (subtab === 'bad-quality' && rowHasBadQualityFlag($row));
            $row.toggleClass('dm-v4-iv-row--subtab-hidden', !show);
        });
        updateIvTabEmptyMessage();
    }

    function initActiveSubTabs() {
        if (!$('#dmV4IvActiveSubTabs').length) return;
        $(document).off('click.dmV4ActiveSubTab', '#dmV4IvActiveSubTabs .dm-v4-iv-subtab')
            .on('click.dmV4ActiveSubTab', '#dmV4IvActiveSubTabs .dm-v4-iv-subtab', function (e) {
                e.preventDefault();
                if (getIvTab() !== 'active') setIvTab('active');
                var subtab = ($(this).attr('data-active-subtab') || 'all').toString();
                if (subtab === getActiveSubTab()) return;
                setActiveSubTab(subtab);
            });
        refreshActiveSubTabCounts();
    }

    // ── Inactive sub-tabs ────────────────────────────────────────────────────

    /** Maps a data-subtab value to the set of data-inactive-reason-category values it shows. */
    function subtabMatchesCategory(subtab, category) {
        switch (subtab) {
            case 'all': return true;
            case 'pending': return category === 'PendingRbReview';
            case 'removed-rb': return category === 'RemovedByRb';
            case 'removed-client': return category === 'RemovedByClient';
            default: return true;
        }
    }

    function getInactiveSubTab() {
        var val = ($('#dmV4IvSubTabs').attr('data-inactive-subtab') || 'all').toString();
        return val || 'all';
    }

    function setInactiveSubTab(subtab) {
        subtab = subtab || 'all';
        $('#dmV4IvSubTabs').attr('data-inactive-subtab', subtab);
        $('#dmV4IvSubTabs .dm-v4-iv-subtab').each(function () {
            var on = ($(this).attr('data-subtab') || '') === subtab;
            $(this).toggleClass('is-on', on).attr('aria-selected', on ? 'true' : 'false');
        });
        applyInactiveSubTabFilter();
    }

    function getInactiveReasonCategoryFromRow($row) {
        var reason = inferInactiveReasonFromRow($row);
        if (!reason || !reason.category) {
            $row.removeAttr('data-inactive-reason-category');
            return '';
        }
        $row.attr('data-inactive-reason-category', reason.category);
        return reason.category;
    }

    function refreshInactiveSubTabCounts() {
        var counts = { all: 0, pending: 0, 'removed-rb': 0, 'removed-client': 0 };
        $('#dmV4IvPanel .dm-v4-iv-row').each(function () {
            var $row = $(this);
            if (readRowDbActive($row)) return;
            var cat = getInactiveReasonCategoryFromRow($row);
            counts.all += 1;
            if (cat === 'PendingRbReview') counts.pending += 1;
            if (cat === 'RemovedByRb') counts['removed-rb'] += 1;
            if (cat === 'RemovedByClient') counts['removed-client'] += 1;
        });
        $('#dmV4SubTabAllCount').text(counts.all);
        $('#dmV4SubTabPendingCount').text(counts.pending);
        $('#dmV4SubTabRemovedRbCount').text(counts['removed-rb']);
        $('#dmV4SubTabRemovedClientCount').text(counts['removed-client']);
    }

    function applyInactiveSubTabFilter() {
        if (getIvTab() !== 'inactive') {
            $('#dmV4IvPanel .dm-v4-iv-row').removeClass('dm-v4-iv-row--subtab-hidden');
            return;
        }
        var subtab = getInactiveSubTab();
        $('#dmV4IvPanel .dm-v4-iv-row').each(function () {
            var $row = $(this);
            if (readRowDbActive($row)) return;
            var cat = getInactiveReasonCategoryFromRow($row);
            var show = subtabMatchesCategory(subtab, cat);
            $row.toggleClass('dm-v4-iv-row--subtab-hidden', !show);
        });
        updateIvTabEmptyMessage();
    }

    function isInactiveRowVisibleInTab($row, mainTab) {
        if ($row.hasClass('dm-v4-iv-row--tab-hidden')) return false;
        if (readRowDbActive($row)) return mainTab === 'active' && !$row.hasClass('dm-v4-iv-row--subtab-hidden');
        if (mainTab !== 'inactive') return false;
        if ($row.hasClass('dm-v4-iv-row--subtab-hidden')) return false;
        return true;
    }

    function updateIvTabEmptyMessage() {
        var $empty = $('#dmV4IvTabEmpty');
        if (!$empty.length) return;
        var tab = getIvTab();
        var visible = 0;
        $('#dmV4IvPanel .dm-v4-iv-row').each(function () {
            if (isInactiveRowVisibleInTab($(this), tab)) visible += 1;
        });
        if (visible === 0) {
            if (tab === 'inactive' && getInactiveSubTab() !== 'all') {
                $empty.text('No interviewers in this category.').prop('hidden', false);
            } else if (tab === 'active' && getActiveSubTab() !== 'all') {
                $empty.text('No interviewers in this category.').prop('hidden', false);
            } else {
                $empty.text(tab === 'active' ? 'No active interviewers.' : 'No inactive interviewers.').prop('hidden', false);
            }
        } else {
            $empty.prop('hidden', true);
        }
    }

    function initInactiveSubTabs() {
        if (!$('#dmV4IvSubTabs').length) return;
        $(document).off('click.dmV4InactiveSubTab', '#dmV4IvSubTabs .dm-v4-iv-subtab')
            .on('click.dmV4InactiveSubTab', '#dmV4IvSubTabs .dm-v4-iv-subtab', function (e) {
                e.preventDefault();
                if (getIvTab() !== 'inactive') {
                    setIvTab('inactive');
                }
                var subtab = ($(this).attr('data-subtab') || 'all').toString();
                if (subtab === getInactiveSubTab()) return;
                setInactiveSubTab(subtab);
            });
        refreshInactiveSubTabCounts();
    }

    // ────────────────────────────────────────────────────────────────────────

    /** Updates status badge only — never mutates data-is-active (server truth for tab filter). */
    function updateIvRowStatus($row) {
        var status = ivStatusBadgeFromRow($row);
        var $badge = $row.find('[data-role="iv-status"]');
        if (!$badge.length) return;
        $badge.removeClass('dm-v4-iv-status--active dm-v4-iv-status--available dm-v4-iv-status--inactive')
            .addClass('dm-v4-iv-status--' + status.tone)
            .text(status.label);
    }

    function dmV4VetErrorText(res, fallback) {
        if (!res) return fallback || 'Unknown error.';
        var err = (res.error || '').toString().trim();
        var msg = (res.message || '').toString().trim();
        if (err && (!msg || msg === 'Forbidden' || msg === 'Unauthorized')) return err;
        return msg || err || fallback || 'Unknown error.';
    }

    function applyVetCascadeRowAttrs($row, opts) {
        if (typeof opts.rbVet !== 'undefined') {
            $row.attr('data-rb-vet', opts.rbVet ? '1' : '0');
            $row.attr('data-rb-vet-db', opts.rbVet ? '1' : '0');
            $row.attr('data-rb-approved', opts.rbVet ? '1' : '0');
        }
        if (typeof opts.clientVet !== 'undefined') {
            $row.attr('data-client-vet', opts.clientVet ? '1' : '0');
            $row.attr('data-client-approved', opts.clientVet ? '1' : '0');
        }
        if (typeof opts.clientAction !== 'undefined') {
            $row.attr('data-client-action', opts.clientAction);
            $row.attr('data-client-approved', opts.clientAction === 'Shortlisted' ? '1' : '0');
        }
        if (typeof opts.active !== 'undefined') {
            $row.attr('data-is-active', opts.active ? '1' : '0');
            if (opts.active) {
                $row.attr('data-ever-activated', '1');
                $row.attr('data-on-panel', '1');
            } else {
                $row.removeAttr('data-auto-qc-verdict data-auto-qc-interview-id data-auto-qc-removed-at');
                $row.find('.dm-v4-iv-active-bad-quality').remove();
            }
        }
        if (typeof opts.available !== 'undefined') {
            $row.attr('data-is-available', opts.available ? '1' : '0');
        }
    }

    function readRowEverActivated($row) {
        return ($row.attr('data-ever-activated') || '0') === '1';
    }

    function readHistoricalRbVetFromRow($row) {
        var token = ($row.attr('data-rb-vet-db') || '').toLowerCase();
        if (token === '1') return { yes: true, explicitNo: false, unset: false };
        if (token === '0') return { yes: false, explicitNo: true, unset: false };
        return { yes: false, explicitNo: false, unset: true };
    }

    function inactiveReasonFromCategory(category) {
        switch ((category || '').toString()) {
            case 'PendingRbReview':
                return { label: 'Pending RB Review', tone: 'info', category: 'PendingRbReview' };
            case 'AwaitingClientApproval':
                return { label: 'Awaiting client approval', tone: 'info', category: 'AwaitingClientApproval' };
            case 'RemovedByRb':
                return { label: 'Removed by RB', tone: 'warning', category: 'RemovedByRb' };
            case 'RemovedByClient':
                return { label: 'Removed by Client', tone: 'warning', category: 'RemovedByClient' };
            case 'Unavailable':
                return { label: 'Not available on channel', tone: 'muted', category: 'Unavailable' };
            case 'Unknown':
                return { label: 'Inactive — reason unknown', tone: 'unknown', category: 'Unknown' };
            case 'RemovedByRbAutoQcSuspicious':
                return { label: 'Removed by RB — Suspicious (Auto QC)', tone: 'warning', category: 'RemovedByRbAutoQcSuspicious', isAutoQc: true };
            case 'RemovedByRbAutoQcRedo':
                return { label: 'Removed by RB — Redo (Auto QC)', tone: 'warning', category: 'RemovedByRbAutoQcRedo', isAutoQc: true };
            default:
                return null;
        }
    }

    function isAutoQcCategory(category) {
        return category === 'RemovedByRbAutoQcSuspicious' || category === 'RemovedByRbAutoQcRedo';
    }

    function readRowOnPanel($row) {
        return ($row.attr('data-on-panel') || '0') === '1';
    }

    function inactiveReasonWithDetail(category, detail) {
        var base = inactiveReasonFromCategory(category);
        if (!base) return null;
        if (detail) base.label = base.label + ' — ' + detail;
        return base;
    }

    function inferInactiveReasonFromAuditRow($row) {
        if (($row.attr('data-inactive-audit-loaded') || '0') !== '1') return null;

        var evt = ($row.attr('data-inactive-audit-event') || '').trim();
        var detail = ($row.attr('data-inactive-audit-detail') || '').trim();

        if (evt === 'AutoQcRemoval') {
            var verdict = (detail || ($row.attr('data-auto-qc-verdict') || '')).toLowerCase();
            if (verdict === 'suspicious') return inactiveReasonFromCategory('RemovedByRbAutoQcSuspicious');
            if (verdict === 'redo') return inactiveReasonFromCategory('RemovedByRbAutoQcRedo');
            return inactiveReasonFromCategory('Unknown');
        }
        if (evt === 'ClientVetFlippedToNo') return inactiveReasonWithDetail('RemovedByClient', detail);
        if (evt === 'RbVetFlippedToNo') return inactiveReasonWithDetail('RemovedByRb', detail);

        // Match server TryResolveFromAuditLog: no removal event and never on active panel.
        if (!evt && !readRowOnPanel($row)) {
            return inactiveReasonFromCategory('PendingRbReview');
        }

        return null;
    }

    function inferInactiveReasonFromRow($row) {
        if (readRowDbActive($row)) return null;

        var fromAudit = inferInactiveReasonFromAuditRow($row);
        if (fromAudit) return fromAudit;

        var autoQcVerdict = ($row.attr('data-auto-qc-verdict') || '').toLowerCase();
        if (autoQcVerdict === 'suspicious') return inactiveReasonFromCategory('RemovedByRbAutoQcSuspicious');
        if (autoQcVerdict === 'redo') return inactiveReasonFromCategory('RemovedByRbAutoQcRedo');

        var channelLocked = isPolicyLockOn(getPolicyBar());
        var available = ($row.attr('data-is-available') || '0') === '1';
        var action = ($row.attr('data-client-action') || '').toLowerCase();
        var rejected = action === 'rejected';
        var rb = readHistoricalRbVetFromRow($row);
        var rbVetYes = rb.yes || ($row.attr('data-rb-approved') || '0') === '1';
        var rbVetExplicitNo = rb.explicitNo;
        var rbVetUnset = rb.unset;
        var clientApproved = ($row.attr('data-client-approved') || '0') === '1' || action === 'shortlisted';
        var onPanel = readRowOnPanel($row);

        if (!rbVetYes && !rbVetExplicitNo && channelLocked && available && !rejected && !onPanel) {
            return inactiveReasonFromCategory('PendingRbReview');
        }
        if (rbVetYes && !clientApproved && !rejected && !onPanel) {
            return inactiveReasonFromCategory('AwaitingClientApproval');
        }
        if (onPanel && rejected && rbVetYes) {
            return inactiveReasonFromCategory('RemovedByClient');
        }
        if (rejected && rbVetYes && !onPanel && clientApproved) {
            return inactiveReasonFromCategory('RemovedByClient');
        }
        if (rbVetExplicitNo || (rejected && !rbVetYes)) {
            return inactiveReasonFromCategory('RemovedByRb');
        }
        if (onPanel && rejected && rbVetUnset) {
            return inactiveReasonFromCategory('RemovedByRb');
        }
        if (onPanel && !rejected) {
            return inactiveReasonFromCategory('RemovedByRb');
        }
        if (rbVetYes && clientApproved && !rejected) {
            return inactiveReasonFromCategory('RemovedByRb');
        }
        if (!rbVetYes && !clientApproved && !onPanel && !available && !rejected) {
            return inactiveReasonFromCategory('Unavailable');
        }
        return inactiveReasonFromCategory('Unknown');
    }

    function syncInactiveReasonBadge($row) {
        var reason = inferInactiveReasonFromRow($row);
        if (!reason) {
            $row.removeAttr('data-inactive-reason-category');
            var $clearBadge = $row.find('.dm-v4-iv-inactive-reason');
            if ($clearBadge.length) {
                $clearBadge.text('').attr('title', '');
            }
            return;
        }
        if (reason.category) {
            $row.attr('data-inactive-reason-category', reason.category);
        }

        var $badge = $row.find('.dm-v4-iv-inactive-reason');
        if (!$badge.length) return;

        $badge
            .removeClass('dm-v4-iv-inactive-reason--info dm-v4-iv-inactive-reason--warning dm-v4-iv-inactive-reason--unknown dm-v4-iv-inactive-reason--muted dm-v4-iv-inactive-reason--auto-qc')
            .addClass('dm-v4-iv-inactive-reason--' + reason.tone);

        if (reason.isAutoQc) {
            $badge.addClass('dm-v4-iv-inactive-reason--auto-qc');
            var interviewId = $row.attr('data-auto-qc-interview-id') || $row.attr('data-inactive-audit-interview-id') || '';
            var verdictLabel = reason.category === 'RemovedByRbAutoQcSuspicious' ? 'suspicious' : 'redo';
            var removedAt = $row.attr('data-auto-qc-removed-at') || $row.attr('data-inactive-audit-changed-at') || '';
            var tooltipHtml = buildAutoQcTooltipHtml(interviewId, verdictLabel, removedAt);
            $badge.empty().append(document.createTextNode(reason.label)).append(tooltipHtml);
        } else {
            $badge.text(reason.label);
        }
        $badge.attr('title', reason.isAutoQc ? '' : reason.label);
    }

    function formatAutoQcTimestamp(iso) {
        if (!iso) return '';
        try {
            var d = new Date(iso);
            if (isNaN(d.getTime())) return iso;
            return d.toLocaleString();
        } catch (_) {
            return iso;
        }
    }

    function buildAutoQcTooltipHtml(interviewId, verdict, removedAtIso) {
        var $tip = $('<span class="dm-v4-autoqc-tooltip"></span>');
        $tip.append('<span class="dm-v4-autoqc-tooltip__label">Auto QC Removal</span>');
        if (interviewId) {
            $tip.append('<br>Verdict: <strong>' + $('<span>').text(verdict).html() + '</strong>');
            $tip.append('<br>Interview ID: <strong>' + $('<span>').text(interviewId).html() + '</strong>');
            if (removedAtIso) {
                $tip.append('<br>Removed: <strong>' + $('<span>').text(formatAutoQcTimestamp(removedAtIso)).html() + '</strong>');
            }
            var url = '/Interview/QCMergedAIDetails?interviewId=' + encodeURIComponent(interviewId);
            $tip.append('<br><a class="dm-v4-autoqc-tooltip__link" href="' + url + '" target="_blank" rel="noopener">View QC details →</a>');
        } else {
            $tip.append('<br>Verdict: <strong>' + $('<span>').text(verdict).html() + '</strong>');
            if (removedAtIso) {
                $tip.append('<br>Removed: <strong>' + $('<span>').text(formatAutoQcTimestamp(removedAtIso)).html() + '</strong>');
            }
            $tip.append('<br><em>No interview ID recorded</em>');
        }
        return $tip;
    }

    function syncVetButtons($row) {
        if (isClientShareView()) {
            var clientOn = readClientVetFromRow($row);
            var vetEnabled = $row.attr('data-client-vet-enabled') === '1';
            var $clientBtns = $row.find('[data-vet="client"] .dm-v4-vet-btn');
            var $clientToggle = $row.find('[data-vet="client"]');
            $clientBtns.removeClass('is-on');
            $clientBtns.filter('[data-vet-value="' + (clientOn ? '1' : '0') + '"]').addClass('is-on');
            var canEdit = getIvCanEditClientVet() && vetEnabled;
            $clientBtns.prop('disabled', !canEdit);
            $clientToggle.attr('title', !vetEnabled ? 'This interviewer is not ready for client review yet.' : '');
            $row.find('.dm-v4-iv-vet-sub--client').text(
                !vetEnabled
                    ? 'Awaiting Risebird review'
                    : (clientOn ? 'Approved for your panel' : 'Awaiting your approval')
            );
            syncInactiveReasonBadge($row);
            return;
        }
        var rbOn = readRbVetFromRow($row);
        var clientOn = readClientVetFromRow($row);
        var isActive = readRowDbActive($row);
        var status = ivStatusBadgeFromRow($row);
        $row.find('[data-vet="rb"] .dm-v4-vet-btn').removeClass('is-on');
        $row.find('[data-vet="rb"] .dm-v4-vet-btn[data-vet-value="' + (rbOn ? '1' : '0') + '"]').addClass('is-on');
        $row.find('[data-vet="client"] .dm-v4-vet-btn').removeClass('is-on');
        $row.find('[data-vet="client"] .dm-v4-vet-btn[data-vet-value="' + (clientOn ? '1' : '0') + '"]').addClass('is-on');
        var $rbSub = $row.find('.dm-v4-iv-cell--rb-vet .dm-v4-iv-vet-sub');
        if ($rbSub.length) {
            if (!isActive) {
                $rbSub.text(status.tone === 'available' ? 'Awaiting RB review' : 'Inactive on panel');
            } else {
                $rbSub.text(rbOn ? 'Risebird verified' : 'Awaiting RB review');
            }
        }
        var $clientBtns = $row.find('[data-vet="client"] .dm-v4-vet-btn');
        var $clientToggle = $row.find('[data-vet="client"]');
        if (!isActive) {
            $clientBtns.prop('disabled', true);
            $clientToggle.attr('title', status.tone === 'available' ? 'Needs RB Vet first.' : 'Inactive on panel');
            $row.find('.dm-v4-iv-vet-sub--client').text(
                status.tone === 'inactive' ? 'Inactive on panel' : 'Blocked — needs RB Vet first'
            );
        } else if (!rbOn) {
            $clientBtns.prop('disabled', true);
            $clientToggle.attr('title', 'Needs RB Vet first.');
            $row.find('.dm-v4-iv-vet-sub--client').text('Blocked — needs RB Vet first');
        } else if (!getIvCanEdit()) {
            $clientBtns.prop('disabled', true);
            $clientToggle.attr('title', 'You do not have permission to change Client Vet.');
            $row.find('.dm-v4-iv-vet-sub--client').text(clientOn ? 'Signed off by client' : 'Awaiting client sign-off');
        } else {
            $clientBtns.prop('disabled', false);
            $clientToggle.attr('title', '');
            $row.find('.dm-v4-iv-vet-sub--client').text(clientOn ? 'Signed off by client' : 'Awaiting client sign-off');
        }
        updateIvRowStatus($row);
        syncInactiveReasonBadge($row);
    }

    function getRubricSkillNames() {
        var el = document.getElementById('dmV4RubricSkillsJson');
        if (!el || !el.textContent) return [];
        try {
            var list = JSON.parse(el.textContent);
            return Array.isArray(list) ? list : [];
        } catch (e) {
            return [];
        }
    }

    function verdictToneFromRow($row) {
        var t = ($row.data('verdict-tone') || '').toString().toLowerCase();
        if (t === 'pass' || t === 'warn' || t === 'fail' || t === 'muted') return t;
        var label = ($row.data('verdict-label') || '').toString().toUpperCase();
        if (label === 'PASS') return 'pass';
        if (label === 'BLOCK') return 'fail';
        if (label === 'REVIEW') return 'warn';
        return 'muted';
    }

    function verdictHeroTitle(tone) {
        if (tone === 'pass') return 'PASS · Recommended for panel';
        if (tone === 'fail') return 'BLOCK · Do not assign';
        if (tone === 'warn') return 'REVIEW · Manual sign-off advised';
        return 'PENDING · AI screening not run';
    }

    function verdictFlagsTitle(tone) {
        if (tone === 'pass') return 'Why we recommend';
        if (tone === 'fail') return 'Why we block';
        if (tone === 'warn') return 'What needs review';
        return 'Screening notes';
    }

    function defaultVerdictBullets(tone) {
        if (tone === 'pass') {
            return ['Meets rubric and resume signals for this role.', 'No blocking flags from AI screening.'];
        }
        if (tone === 'fail') {
            return ['Below threshold — review before assigning interviews.', 'Check coded CV and skill claims before panel activation.'];
        }
        if (tone === 'warn') {
            return ['Manual sign-off advised before panel activation.', 'Review AI screening output and RB vet status.'];
        }
        return ['AI screening has not been run for this skill channel yet.'];
    }

    function parseVerdictBullets(text, tone) {
        if (!text) return defaultVerdictBullets(tone);
        var lines = text.split(/\n+/).map(function (s) { return s.trim(); }).filter(Boolean);
        var bullets = lines.filter(function (l) {
            return /^[-•*]\s/.test(l) || /^\d+\.\s/.test(l);
        }).map(function (l) {
            return l.replace(/^[-•*]\s*/, '').replace(/^\d+\.\s*/, '').trim();
        });
        if (bullets.length) return bullets.slice(0, 6);
        var sentences = text.replace(/\n/g, ' ').split(/[.!?]+\s+/).map(function (s) { return s.trim(); }).filter(function (s) {
            return s.length > 24;
        });
        if (sentences.length) return sentences.slice(0, 4);
        return [text.length > 400 ? text.substring(0, 400) + '…' : text];
    }

    function factorBarClass(score) {
        if (score >= 80) return 'dm-v4-vd-factor-bar__fill--ok';
        if (score >= 60) return 'dm-v4-vd-factor-bar__fill--mid';
        return 'dm-v4-vd-factor-bar__fill--low';
    }

    function renderVerdictFactors($container, tone, screenScore, clientVet) {
        var score = Math.max(0, parseInt(screenScore, 10) || 0);
        var rubricMatch = score > 0 ? Math.min(100, Math.max(0, score - 4)) : 0;
        var cal = tone === 'pass' ? 88 : tone === 'warn' ? 64 : tone === 'fail' ? 38 : 0;
        var client = clientVet ? 95 : tone === 'pass' ? 70 : 50;
        var factors = [
            { label: 'AI screening score', score: score, weight: 30 },
            { label: 'Skill match vs rubric', score: rubricMatch, weight: 30 },
            { label: 'Calibration with rubric', score: cal, weight: 20 },
            { label: 'Client compatibility', score: client, weight: 20 }
        ];
        var overall = Math.round(factors.reduce(function (s, f) {
            return s + (f.score * f.weight / 100);
        }, 0));
        var html = '';
        factors.forEach(function (f) {
            html += '<div class="dm-v4-vd-factor">' +
                '<div class="dm-v4-vd-factor__head">' +
                '<span>' + f.label + '</span>' +
                '<span class="dm-v4-mono">' + f.score + '<span class="dm-v4-vd-factor__wt"> · ' + f.weight + '%</span></span>' +
                '</div>' +
                '<div class="dm-v4-vd-factor-bar"><span class="dm-v4-vd-factor-bar__fill ' + factorBarClass(f.score) + '" style="width:' + Math.min(100, f.score) + '%"></span></div>' +
                '</div>';
        });
        $container.html(html);
        return overall;
    }

    function renderVerdictSnapshot($container, primarySkill, screenScore, totalIv) {
        var score = Math.max(0, parseInt(screenScore, 10) || 0);
        var match = score > 0 ? Math.min(100, Math.max(0, score - 4)) : 0;
        var mainSkill = (primarySkill || 'Primary skill').split(',')[0].trim() || 'Primary skill';
        var conducted = Math.max(0, parseInt(totalIv, 10) || 0);
        var rows = [
            { skill: mainSkill, done: conducted || 14, pass: Math.round(match * 0.92) || 0, avg: (7 + (match / 100) * 2).toFixed(1), dot: 'accent' },
            { skill: 'Adjacent skill', done: Math.max(0, Math.floor(conducted * 0.65)) || 9, pass: 78, avg: '7.4', dot: 'green' },
            { skill: 'Secondary skill', done: Math.max(0, Math.floor(conducted * 0.45)) || 6, pass: 82, avg: '8.1', dot: 'green' }
        ];
        var html = '';
        rows.forEach(function (r) {
            html += '<div class="dm-v4-vd-snapshot-row">' +
                '<div class="dm-v4-vd-snapshot-row__skill">' +
                '<span class="dm-v4-vd-snapshot-row__dot dm-v4-vd-snapshot-row__dot--' + r.dot + '"></span>' +
                '<span class="dm-v4-vd-snapshot-row__name">' + $('<span/>').text(r.skill).html() + '</span>' +
                '</div>' +
                '<div class="dm-v4-vd-snapshot-row__stat"><span class="dm-v4-vd-snapshot-row__stat-label">conducted</span>' +
                '<span class="dm-v4-vd-snapshot-row__stat-val dm-v4-mono">' + r.done + '</span></div>' +
                '<div class="dm-v4-vd-snapshot-row__stat"><span class="dm-v4-vd-snapshot-row__stat-label">pass %</span>' +
                '<span class="dm-v4-vd-snapshot-row__stat-val dm-v4-mono">' + r.pass + '%</span></div>' +
                '<div class="dm-v4-vd-snapshot-row__stat"><span class="dm-v4-vd-snapshot-row__stat-label">avg score</span>' +
                '<span class="dm-v4-vd-snapshot-row__stat-val dm-v4-mono">' + r.avg + '<span class="dm-v4-vd-snapshot-row__stat-dim">/10</span></span></div>' +
                '</div>';
        });
        $container.html(html);
    }

    var vdChipIconCheck = '<span class="dm-v4-vd-skill-chip__icon" aria-hidden="true"><svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.8" width="10" height="10"><path d="M3.5 8.5l3 3L13 5" stroke-linecap="round" stroke-linejoin="round"/></svg></span>';
    var vdChipIconX = '<span class="dm-v4-vd-skill-chip__icon" aria-hidden="true"><svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" width="10" height="10"><path d="M4 4l8 8M12 4l-8 8" stroke-linecap="round"/></svg></span>';

    function renderSkillCoverageChips($container, rubricSkills, primarySkill, screenScore) {
        if (!rubricSkills.length) {
            $container.html('<span class="dm-v4-vd-coverage-empty">No rubric skills on this channel.</span>');
            return;
        }
        var score = parseInt(screenScore, 10) || 0;
        var primary = (primarySkill || '').toLowerCase();
        var html = '';
        rubricSkills.slice(0, 6).forEach(function (name, i) {
            var covered = score >= 70 && (
                !primary ||
                name.toLowerCase().indexOf(primary.split(',')[0].trim()) >= 0 ||
                primary.indexOf(name.toLowerCase()) >= 0 ||
                (i + (name.length % 3)) % 3 !== 0
            );
            html += '<span class="dm-v4-vd-skill-chip' + (covered ? ' dm-v4-vd-skill-chip--ok' : '') + '">' +
                (covered ? vdChipIconCheck : vdChipIconX) +
                $('<span/>').text(name).html() +
                '</span>';
        });
        $container.html(html);
    }

    function openVerdictDrawer($row) {
        window._dmV4VerdictDrawerRow = $row;
        var name = $row.find('.dm-v4-iv-coded-label').first().text().trim();
        if (!name) {
            name = $row.find('.dm-v4-iv-code').first().text().replace(/\s+/g, ' ').replace(/›/g, '').trim();
        }
        var tone = verdictToneFromRow($row);
        var $full = $row.find('.dm-v4-iv-ai-reason-full');
        var reason = $full.length ? $full.text().trim() : $row.find('.dm-v4-iv-verdict-reason').text().trim();
        if (!reason) reason = 'AI screening not run for this skill channel yet.';

        var scoreText = $row.find('.dm-v4-iv-score').first().text().trim();
        if (!scoreText || scoreText === '—') scoreText = String($row.data('ai-score') || '0');
        if (scoreText === '—') scoreText = '0';
        var screenScore = parseInt(scoreText, 10) || 0;

        $('#dmV4VerdictDrawerTitle').text(name || 'Interviewer');
        if (isClientShareView()) {
            $('#dmV4VerdictDrawerEyebrow').text('AI verdict');
            $('#dmV4VerdictDrawerHeroTitle').hide();
            $('#dmV4VerdictDrawerFlags').hide().empty();
        } else {
            $('#dmV4VerdictDrawerEyebrow').text('AI verdict · ' + (tone === 'pass' ? 'PASS' : tone === 'fail' ? 'BLOCK' : tone === 'warn' ? 'REVIEW' : 'PENDING'));
            $('#dmV4VerdictDrawerHeroTitle').show();
        }
        $('.dm-v4-verdict-drawer-body').attr('class', 'dm-v4-drawer__body dm-v4-verdict-drawer-body dm-v4-verdict-drawer-body--' + tone);

        var $hero = $('#dmV4VerdictDrawerHero');
        $hero.attr('class', 'dm-v4-vd-hero dm-v4-vd-hero--' + tone);
        if (!isClientShareView()) {
            $('#dmV4VerdictDrawerHeroTitle').text(verdictHeroTitle(tone));
        }
        $('#dmV4VerdictDrawerReason').text(reason);

        var $badge = $row.find('.dm-v4-verdict').first().clone().removeAttr('id');
        $('#dmV4VerdictDrawerBadge').replaceWith($badge.attr('id', 'dmV4VerdictDrawerBadge'));

        $('#dmV4VerdictDrawerOverall').text(screenScore > 0 ? screenScore : '—');

        if (!isClientShareView()) {
            var bullets = parseVerdictBullets(reason, tone);
            var flagsHtml = '';
            bullets.forEach(function (b) {
                flagsHtml += '<li class="dm-v4-vd-flag-item">' +
                    '<span class="dm-v4-vd-flag-item__icon" aria-hidden="true"><i class="fa ' +
                    (tone === 'pass' ? 'fa-check' : tone === 'fail' ? 'fa-times' : 'fa-exclamation') +
                    '"></i></span><span>' + $('<span/>').text(b).html() + '</span></li>';
            });
            $('#dmV4VerdictDrawerFlags').html(flagsHtml).show();
        }

        dmV4UpdateScreenedAtCaption($row);
        dmV4SyncDrawerScreenBtnVisibility($row);
        if (!isClientShareView()) {
            dmV4SyncVerdictDrawerKyc($row);
        } else {
            $('#dmV4VerdictDrawerScreenBtn').closest('.dm-v4-vd-section--screen').hide();
            $('#dmV4VerdictDrawerKycSection').prop('hidden', true).hide();
            $('#dmV4VerdictDrawerScreenedAt').hide();
        }

        $('#dmV4VerdictOverlay, #dmV4VerdictDrawer').addClass('is-open').attr('aria-hidden', 'false');
    }

    function closeVerdictDrawer() {
        $('#dmV4VerdictOverlay, #dmV4VerdictDrawer').removeClass('is-open').attr('aria-hidden', 'true');
        $('#dmV4VerdictDrawerKycSection').prop('hidden', true).hide();
    }

    var vdVerdictIconPass = '<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.8" width="11" height="11"><path d="M3.5 8.5l3 3L13 5" stroke-linecap="round" stroke-linejoin="round"/></svg>';
    var vdVerdictIconWarn = '<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" width="11" height="11"><path d="M8 2l6.5 11.5h-13L8 2z" stroke-linejoin="round"/><path d="M8 6.5v3M8 11.5v.5" stroke-linecap="round"/></svg>';
    var vdVerdictIconFail = '<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" width="11" height="11"><path d="M4 4l8 8M12 4l-8 8" stroke-linecap="round"/></svg>';
    var vdVerdictIconMuted = '<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.4" width="11" height="11"><circle cx="8" cy="8" r="6"/><path d="M8 7v4M8 5v.5" stroke-linecap="round"/></svg>';

    function dmV4ResumeSummaryErrorMessage(xhr, data) {
        if (data && data.message) return String(data.message);
        if (xhr && xhr.responseJSON && xhr.responseJSON.message) return String(xhr.responseJSON.message);
        if (xhr && xhr.status === 0) return 'Network error — check your connection and retry.';
        if (xhr && xhr.status >= 500) return 'Server error (' + xhr.status + ') — please retry.';
        if (xhr && (xhr.status === 401 || xhr.status === 403)) return 'Not authorized to generate resume summary.';
        if (xhr && xhr.statusText) return xhr.statusText;
        return 'Could not generate resume summary.';
    }

    function dmV4ShowResumeSummaryError(context, xhr, data) {
        var msg = dmV4ResumeSummaryErrorMessage(xhr, data);
        console.error('[dm-v4] Resume summary failed:', context, { message: msg, xhr: xhr, data: data });
        if (typeof swal !== 'undefined') {
            swal.fire({
                title: 'Resume summary failed',
                type: 'error',
                text: msg
            });
        }
        if (typeof closeResumeSummaryDrawer === 'function') closeResumeSummaryDrawer();
        return msg;
    }

    function dmV4IsSummaryCodedCvLink(link) {
        if (!link) return false;
        var path = String(link).split('?')[0].toLowerCase();
        return path.indexOf('resumesummary_') >= 0 || path.endsWith('.json');
    }

    function dmV4SetResumeSummaryDrawerTitle(label) {
        var title = (label && String(label).trim()) ? String(label).trim() + ' — Resume Summary' : 'Resume Summary';
        $('#dmV4ResumeSummaryTitle').text(title);
    }

    function dmV4ShowResumeSummaryLoading(message) {
        if (typeof openResumeSummaryDrawer === 'function') openResumeSummaryDrawer();
        $('#id_candidateResumeSummaryBody').html(
            '<div class="dm-v4-summary-loading">' +
            '<div class="dm-v4-summary-loading__spinner" aria-hidden="true"></div>' +
            '<p class="dm-v4-summary-loading__text">' + (message || 'Loading resume summary…') + '</p>' +
            '<p class="dm-v4-summary-loading__hint">AI-powered summary</p></div>');
    }

    function dmV4OpenInterviewerResumeSummary(codedCvLink, resumeLink, cvLabel) {
        var cfg = getDmIvCvConfig();
        dmV4SetResumeSummaryDrawerTitle(cvLabel);
        if (codedCvLink && !dmV4IsSummaryCodedCvLink(codedCvLink)) {
            codedCvLink = '';
        }
        var clientShareData = {};
        if (isClientShareView()) {
            var $row = (window._dmV4CvSummaryRow && window._dmV4CvSummaryRow.length)
                ? window._dmV4CvSummaryRow
                : (window._dmV4VerdictDrawerRow && window._dmV4VerdictDrawerRow.length ? window._dmV4VerdictDrawerRow : $('.dm-v4-iv-row').first());
            var shareIrfId = parseInt($row.data('irf-id'), 10);
            if (shareIrfId) {
                clientShareData.irfId = shareIrfId;
                clientShareData.Type = getClientShareTypeParam();
            }
        }
        if (dmV4IsSummaryCodedCvLink(codedCvLink)) {
            dmV4ShowResumeSummaryLoading('Loading saved resume summary…');
            $.ajax({
                url: cfg.viewCodedSummaryUrl || '/IRF/GetInterviewerCodedCvSummary',
                type: 'POST',
                data: $.extend({ codedCvLink: codedCvLink }, clientShareData),
                success: function (result) {
                    if (result != null && String(result).trim()) {
                        $('#id_candidateResumeSummaryBody').html(result);
                    } else {
                        $('#id_candidateResumeSummaryBody').html(
                            '<p class="text-danger" style="padding:20px;">Could not load resume summary from coded CV.</p>');
                    }
                },
                error: function (xhr) {
                    console.error('[dm-v4] coded CV summary load failed', xhr);
                    $('#id_candidateResumeSummaryBody').html(
                        '<p class="text-danger" style="padding:20px;">Failed to load resume summary. Please try again.</p>');
                }
            });
            return;
        }
        if (resumeLink) {
            dmV4ShowResumeSummaryLoading('Generating resume summary from resume…');
            $.ajax({
                url: cfg.viewResumeSummaryUrl || '/IRF/GetInterviewerResumeSummary',
                type: 'POST',
                data: { resumeLink: resumeLink },
                success: function (result) {
                    if (result != null && String(result).trim()) {
                        $('#id_candidateResumeSummaryBody').html(result);
                    } else {
                        $('#id_candidateResumeSummaryBody').html(
                            '<p class="text-danger" style="padding:20px;">Could not generate resume summary.</p>');
                    }
                },
                error: function (xhr) {
                    console.error('[dm-v4] resume summary load failed', xhr);
                    $('#id_candidateResumeSummaryBody').html(
                        '<p class="text-danger" style="padding:20px;">Failed to load resume summary. Please try again.</p>');
                }
            });
            return;
        }
        dmV4ShowResumeSummaryLoading('No resume summary available.');
        $('#id_candidateResumeSummaryBody').html(
            '<p class="text-danger" style="padding:20px;">No coded resume summary is saved for this interviewer yet.</p>');
    }

    window.dmV4OpenInterviewerResumeSummary = dmV4OpenInterviewerResumeSummary;

    function getDmIvCvConfig() {
        var el = document.getElementById('dmV4IvCvConfig');
        if (!el || !el.textContent) return {};
        try {
            return JSON.parse(el.textContent);
        } catch (e) {
            return {};
        }
    }

    function formatIvFutureAssignedCount(count) {
        var n = parseInt(count, 10);
        if (isNaN(n) || n < 0) n = 0;
        return String(n);
    }

    function setIvFutureAssignedDisplay($row, count) {
        if (!$row || !$row.length) return;
        var text = formatIvFutureAssignedCount(count);
        $row.attr('data-future-assigned', text);
        $row.attr('data-assigned', text);
        $row.find('.dm-v4-iv-future-count').text(text);
        var $capAssigned = $row.find('.dm-v4-iv-cap-assigned');
        if ($capAssigned.length) {
            $capAssigned.text(text);
        }
        var $wrap = $row.find('.dm-v4-iv-capacity').first();
        if ($wrap.length) {
            $wrap.attr('data-assigned', text);
        }
    }

    function syncDmV4IvFutureAssignedFromRow($row) {
        if (!$row || !$row.length) return;
        var count = parseInt($row.attr('data-future-assigned'), 10);
        if (isNaN(count)) {
            count = parseInt($row.attr('data-assigned'), 10);
        }
        if (isNaN(count)) count = 0;
        setIvFutureAssignedDisplay($row, count);
    }

    function syncDmV4IvFutureAssignedAll() {
        $('#dmV4IvPanel .dm-v4-iv-row').each(function () {
            syncDmV4IvFutureAssignedFromRow($(this));
        });
    }

    function dmV4FormatAbsoluteTimestamp(iso) {
        if (!iso) return '';
        var d = new Date(iso);
        if (isNaN(d.getTime())) return '';
        return d.toLocaleString('en-GB', {
            day: 'numeric',
            month: 'short',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    function dmV4FormatRelativeTime(iso) {
        if (!iso) return null;
        var d = new Date(iso);
        if (isNaN(d.getTime())) return null;
        var sec = Math.floor((Date.now() - d.getTime()) / 1000);
        if (sec < 0) return 'just now';
        if (sec < 60) return 'just now';
        if (sec < 3600) {
            var mins = Math.floor(sec / 60);
            return mins <= 1 ? '1 min ago' : mins + ' min ago';
        }
        if (sec < 86400) {
            var hours = Math.floor(sec / 3600);
            return hours < 6 ? 'a few hours ago' : hours + 'h ago';
        }
        if (sec < 604800) return Math.floor(sec / 86400) + 'd ago';
        if (sec < 2592000) return Math.floor(sec / 604800) + 'w ago';
        if (sec < 31536000) return Math.floor(sec / 2592000) + 'mo ago';
        return Math.floor(sec / 31536000) + 'y ago';
    }

    function dmV4ApplyIvActivityMeta($row, opts) {
        if (!$row || !$row.length) return;
        opts = opts || {};
        var loginIso = opts.lastLoginAt != null ? String(opts.lastLoginAt) : String($row.data('last-login-at') || '');
        var interviewIso = opts.lastInterviewConductedAt != null
            ? String(opts.lastInterviewConductedAt) : String($row.data('last-interview-at') || '');
        if (opts.lastLoginAt != null) $row.attr('data-last-login-at', loginIso);
        if (opts.lastInterviewConductedAt != null) $row.attr('data-last-interview-at', interviewIso);

        var $meta = $row.find('.dm-v4-iv-meta').first();
        if (!$meta.length) return;

        var $loginLine = $meta.find('[data-meta="login"]');
        var $interviewLine = $meta.find('[data-meta="interview"]');

        if (loginIso) {
            var relLogin = dmV4FormatRelativeTime(loginIso);
            $loginLine.text('Last login: ' + (relLogin || '—'));
            $loginLine.attr('title', dmV4FormatAbsoluteTimestamp(loginIso));
        } else {
            $loginLine.text('Last login: Never logged in');
            $loginLine.removeAttr('title');
        }

        if (interviewIso) {
            var relIv = dmV4FormatRelativeTime(interviewIso);
            $interviewLine.text('Last interview: ' + (relIv || '—'));
            $interviewLine.attr('title', dmV4FormatAbsoluteTimestamp(interviewIso));
        } else {
            $interviewLine.text('Last interview: No interviews yet');
            $interviewLine.removeAttr('title');
        }
    }

    function dmV4InitIvActivityMetaAll() {
        $('#dmV4IvPanel .dm-v4-iv-row').each(function () {
            dmV4ApplyIvActivityMeta($(this));
        });
    }

    function refreshDmV4IvPanelMetrics() {
        if (isClientShareView()) {
            return $.Deferred().resolve().promise();
        }
        var cfg = getDmIvCvConfig();
        var irfId = parseInt($('#id').val(), 10) || parseInt($('#dmV4IvPanel').closest('form').find('[name="id"]').val(), 10);
        if (!cfg.panelMetricsUrl || !irfId) {
            syncDmV4IvFutureAssignedAll();
            dmV4InitIvActivityMetaAll();
            return $.Deferred().resolve().promise();
        }
        return $.getJSON(cfg.panelMetricsUrl, { irfId: irfId }).done(function (res) {
            if (!res || !res.ok || !res.interviewers) {
                syncDmV4IvFutureAssignedAll();
                dmV4InitIvActivityMetaAll();
                return;
            }
            res.interviewers.forEach(function (iv) {
                var userId = parseInt(iv.userId, 10);
                if (isNaN(userId)) return;
                var $row = $('#dmV4IvPanel .dm-v4-iv-row').filter(function () {
                    return parseInt($(this).data('user-id'), 10) === userId;
                }).first();
                if ($row.length) {
                    setIvFutureAssignedDisplay($row, iv.futureAssignedCount);
                    dmV4ApplyIvActivityMeta($row, {
                        lastLoginAt: iv.lastLoginAt || '',
                        lastInterviewConductedAt: iv.lastInterviewConductedAt || ''
                    });
                }
            });
        }).fail(function () {
            syncDmV4IvFutureAssignedAll();
            dmV4InitIvActivityMetaAll();
        });
    }

    function dmV4VerdictToneFromAi(aiVerdict) {
        if (!aiVerdict) return 'muted';
        var v = String(aiVerdict).toLowerCase();
        if (v.indexOf('conditional') >= 0 || v.indexOf('review') >= 0 || v.indexOf('warn') >= 0) return 'warn';
        if (v.indexOf('no') >= 0 || v.indexOf('fail') >= 0 || v.indexOf('block') >= 0 || v.indexOf('reject') >= 0) return 'fail';
        if (v.indexOf('yes') >= 0 || v.indexOf('pass') >= 0 || v.indexOf('recommend') >= 0) return 'pass';
        return 'muted';
    }

    function dmV4VerdictLabelFromTone(tone) {
        if (tone === 'pass') return 'Yes';
        if (tone === 'fail') return 'No';
        if (tone === 'warn') return 'Conditional Yes';
        return 'Pending';
    }

    function dmV4VerdictIconForTone(tone) {
        if (tone === 'pass') return vdVerdictIconPass;
        if (tone === 'fail') return vdVerdictIconFail;
        if (tone === 'warn') return vdVerdictIconWarn;
        return vdVerdictIconMuted;
    }

    function dmV4ReasonSnippet(text, maxLen) {
        if (!text) return '';
        var t = String(text).replace(/\s+/g, ' ').trim();
        if (t.length <= (maxLen || 140)) return t;
        return t.substring(0, maxLen || 140) + '…';
    }

    function dmV4MapVerdictLabelToRowTone(label) {
        var u = (label || '').toUpperCase();
        if (u === 'YES' || u === 'PASS') return 'pass';
        if (u === 'NO' || u === 'BLOCK') return 'fail';
        if (u === 'CONDITIONAL YES' || u === 'REVIEW') return 'warn';
        return 'muted';
    }

    function dmV4GetRowScreenBtn($row) {
        return $row.find('.dm-v4-iv-cell--verdict .dm-v4-iv-screen-btn');
    }

    function dmV4GetDrawerScreenBtn() {
        return $('#dmV4VerdictDrawerScreenBtn');
    }

    function dmV4GetLinkedScreenBtns($row) {
        var $btns = dmV4GetRowScreenBtn($row);
        if (window._dmV4VerdictDrawerRow && window._dmV4VerdictDrawerRow.is($row)) {
            $btns = $btns.add(dmV4GetDrawerScreenBtn());
        }
        return $btns;
    }

    function dmV4SetScreenBtnHtml($btn, icon, label) {
        $btn.attr('data-has-screening', label === 'Regenerate' ? '1' : '0');
        $btn.html('<i class="fa fa-' + icon + '" aria-hidden="true"></i> <span class="dm-v4-screen-btn-label">' +
            $('<span/>').text(label).html() + '</span>');
    }

    function dmV4RowHasScreening($row) {
        var $rowBtn = dmV4GetRowScreenBtn($row);
        var hasFlag = $rowBtn.length && $rowBtn.attr('data-has-screening') === '1';
        var verdict = ($row.data('ai-verdict') || '').toString().trim();
        var score = parseInt($row.data('ai-score'), 10) || 0;
        return hasFlag || score > 0 || !!verdict;
    }

    function dmV4FormatScreenedAt(iso) {
        if (!iso) return '';
        try {
            var d = new Date(iso);
            if (isNaN(d.getTime())) return '';
            var mins = Math.round((Date.now() - d.getTime()) / 60000);
            if (mins < 1) return 'just now';
            if (mins < 60) return mins + 'm ago';
            var hrs = Math.round(mins / 60);
            if (hrs < 48) return hrs + 'h ago';
            return d.toLocaleString();
        } catch (e) { return ''; }
    }

    function dmV4SyncScreenButtonLabels($row) {
        var has = dmV4RowHasScreening($row);
        var label = has ? 'Regenerate' : 'Screen Resume';
        var icon = has ? 'refresh' : 'search';
        dmV4GetRowScreenBtn($row).each(function () {
            dmV4SetScreenBtnHtml($(this), icon, label);
        });
        if (window._dmV4VerdictDrawerRow && window._dmV4VerdictDrawerRow.is($row)) {
            var $drawerBtn = dmV4GetDrawerScreenBtn();
            if ($drawerBtn.length && dmV4GetRowScreenBtn($row).length) {
                dmV4SetScreenBtnHtml($drawerBtn, icon, label);
            }
        }
    }

    function dmV4SyncDrawerScreenBtnVisibility($row) {
        var $drawerBtn = dmV4GetDrawerScreenBtn();
        if (!$drawerBtn.length) return;
        if (dmV4GetRowScreenBtn($row).length) {
            $drawerBtn.prop('hidden', false).show();
            dmV4SyncScreenButtonLabels($row);
        } else {
            $drawerBtn.prop('hidden', true).hide();
        }
    }

    function dmV4UpdateScreenedAtCaption($row, isoOverride) {
        var iso = isoOverride || ($row.data('screened-at') || '').toString();
        var rel = dmV4FormatScreenedAt(iso);
        var text = rel ? ('Last screened: ' + rel) : '';
        var $cap = $row.find('.dm-v4-iv-cell--verdict .dm-v4-iv-screened-at');
        if (text) {
            if (!$cap.length) {
                $cap = $('<div class="dm-v4-iv-screened-at dm-v4-muted"></div>');
                $row.find('.dm-v4-iv-cell--verdict').append($cap);
            }
            $cap.text(text).show();
        } else if ($cap.length) {
            $cap.hide();
        }
        var $drawerCap = $('#dmV4VerdictDrawerScreenedAt');
        if ($drawerCap.length) {
            if (text) $drawerCap.text(text).show();
            else $drawerCap.hide();
        }
    }

    var dmV4ScreeningInFlight = false;

    function dmV4SetScreeningBusy($row, busy) {
        var $screenBtns = dmV4GetLinkedScreenBtns($row);
        if (busy) {
            $screenBtns.each(function () {
                var $b = $(this);
                if (!$b.data('dm-v4-screen-orig-html')) {
                    $b.data('dm-v4-screen-orig-html', $b.html());
                }
                $b.prop('disabled', true).html(
                    '<i class="fa fa-spinner fa-spin" aria-hidden="true"></i> <span class="dm-v4-screen-btn-label">Screening…</span>'
                );
            });
            var $reason = $row.find('.dm-v4-iv-verdict-reason');
            if ($reason.length && !$reason.data('dm-v4-reason-orig')) {
                $reason.data('dm-v4-reason-orig', $reason.text());
            }
            if ($reason.length) {
                $reason.text('Generating AI reasoning…');
            }
            if (window._dmV4VerdictDrawerRow && window._dmV4VerdictDrawerRow.is($row)) {
                $('#dmV4VerdictDrawerReason').text('Generating AI reasoning…');
                $('#dmV4VerdictDrawerEyebrow').text('AI verdict · GENERATING');
            }
            $row.addClass('dm-v4-iv-row--screening');
        } else {
            $screenBtns.each(function () {
                var $b = $(this);
                $b.prop('disabled', false);
                $b.removeData('dm-v4-screen-orig-html');
            });
            var $reason = $row.find('.dm-v4-iv-verdict-reason');
            var origReason = $reason.data('dm-v4-reason-orig');
            if (origReason && $reason.length) {
                $reason.text(origReason);
            }
            $reason.removeData('dm-v4-reason-orig');
            $row.removeClass('dm-v4-iv-row--screening');
        }
    }

    function dmV4RunScreening($row) {
        if (dmV4ScreeningInFlight || !$row || !$row.length) return;
        var cfg = getDmIvCvConfig();
        if (!cfg.screenUrl) return;
        var isRedo = dmV4RowHasScreening($row);
        dmV4ScreeningInFlight = true;
        dmV4SetScreeningBusy($row, true);
        $.post(cfg.screenUrl, {
            jdTitle: $row.data('jd-title') || '',
            irfId: parseInt($row.data('irf-id'), 10) || parseInt($('#id').val(), 10),
            userId: parseInt($row.data('user-id'), 10),
            irfName: $row.data('irf-name') || '',
            isRedo: isRedo
        }).done(function (res) {
            if (res && res.ok) {
                dmV4ApplyScreeningResult($row, res);
            } else if (typeof swal !== 'undefined') {
                swal.fire('Screening', (res && res.message) || 'Failed.', 'error');
            }
        }).fail(function () {
            if (typeof swal !== 'undefined') swal.fire('Screening', 'Request failed.', 'error');
        }).always(function () {
            dmV4ScreeningInFlight = false;
            dmV4SetScreeningBusy($row, false);
            dmV4SyncScreenButtonLabels($row);
        });
    }

    function dmV4ApplyScreeningResult($row, res) {
        var tone = dmV4VerdictToneFromAi(res.aiVerdict);
        var label = dmV4VerdictLabelFromTone(tone);
        var score = parseInt(res.aiScore, 10);
        if (isNaN(score)) score = 0;
        var reasonFull = (res.reasoning || '').toString().trim();
        var reasonSnippet = dmV4ReasonSnippet(reasonFull, 140) || 'AI screening completed.';

        $row.attr('data-ai-verdict', res.aiVerdict || '');
        $row.attr('data-ai-score', score);
        $row.attr('data-verdict-tone', tone);
        $row.attr('data-verdict-label', label);

        var $verdictCell = $row.find('.dm-v4-iv-cell--verdict');
        var scoreDisplay = score > 0 || reasonFull ? String(score) : '—';
        var $fullHidden = $verdictCell.find('.dm-v4-iv-ai-reason-full');
        if (!$fullHidden.length) {
            $fullHidden = $('<div class="dm-v4-iv-ai-reason-full" hidden aria-hidden="true"></div>');
            $verdictCell.append($fullHidden);
        }
        $fullHidden.text(reasonFull);

        var verdictHtml =
            '<button type="button" class="dm-v4-iv-verdict-link dm-v4-iv-verdict-open" data-screen-url="' +
            ($row.find('.dm-v4-iv-verdict-open').first().data('screen-url') || '#') + '" title="View AI reasoning + screening score">' +
            '<span class="dm-v4-iv-verdict-main">' +
            '<span class="dm-v4-iv-verdict-top">' +
            '<span class="dm-v4-verdict dm-v4-verdict--' + tone + '"><span class="dm-v4-verdict__icon" aria-hidden="true">' +
            dmV4VerdictIconForTone(tone) + '</span>' + $('<span/>').text(label).html() + '</span>' +
            '<span class="dm-v4-iv-score-wrap"><span class="dm-v4-iv-score dm-v4-iv-score--' + tone + '">' +
            $('<span/>').text(scoreDisplay).html() + '</span><span class="dm-v4-iv-score-dim">/100</span></span>' +
            '</span>' +
            '<span class="dm-v4-iv-verdict-reason">' + $('<span/>').text(reasonSnippet).html() + '</span>' +
            '</span>' +
            '<span class="dm-v4-iv-why">Why <i class="fa fa-chevron-right" aria-hidden="true"></i></span>' +
            '</button>';
        $verdictCell.find('.dm-v4-iv-verdict-link').replaceWith(verdictHtml);

        if (res.screenedAt) {
            $row.attr('data-screened-at', res.screenedAt);
        }
        dmV4UpdateScreenedAtCaption($row, res.screenedAt);
        dmV4SyncScreenButtonLabels($row);
        if (window._dmV4VerdictDrawerRow && window._dmV4VerdictDrawerRow.is($row)) {
            dmV4SyncDrawerScreenBtnVisibility($row);
            $('#dmV4VerdictDrawerEyebrow').text('AI verdict · ' + (tone === 'pass' ? 'PASS' : tone === 'fail' ? 'BLOCK' : tone === 'warn' ? 'REVIEW' : 'PENDING'));
            $('#dmV4VerdictDrawerHeroTitle').text(verdictHeroTitle(tone));
            $('#dmV4VerdictDrawerReason').text(reasonFull || reasonSnippet);
            $('#dmV4VerdictDrawerOverall').text(score > 0 ? score : '—');
            var $hero = $('#dmV4VerdictDrawerHero');
            $hero.attr('class', 'dm-v4-vd-hero dm-v4-vd-hero--' + tone);
            var $badge = $row.find('.dm-v4-verdict').first().clone().removeAttr('id');
            $('#dmV4VerdictDrawerBadge').replaceWith($badge.attr('id', 'dmV4VerdictDrawerBadge'));
        }
    }

    function dmV4RowCvCode($row) {
        if (!$row || !$row.length) return 'CV';
        var code = ($row.attr('data-cv-code') || '').toString().trim();
        if (code) return code;
        var nick = $row.find('.dm-v4-iv-code').first().clone().children().remove().end().text().trim();
        if (nick && nick !== '—') return nick;
        return 'CV';
    }

    function dmV4ResolveRowIrfId($row) {
        var n = parseInt($row.attr('data-irf-id'), 10);
        if (!isNaN(n) && n > 0) return n;
        n = parseInt($('#id').val(), 10);
        if (!isNaN(n) && n > 0) return n;
        var $bar = getPolicyBar();
        n = parseInt($bar.attr('data-irf-id'), 10);
        if (!isNaN(n) && n > 0) return n;
        n = parseInt($bar.data('irf-id'), 10);
        return isNaN(n) ? 0 : n;
    }

    /** Apply coded CV from generate-save response (or refetch) without page reload. */
    function dmV4ApplyCvSaveResult($row, payload) {
        if (!$row || !$row.length || !payload) return;
        var codedCvLink = (payload.codedCvLink || '').toString().trim();
        if (!codedCvLink) return;
        var resumeLink = (payload.resumeLink || $row.attr('data-resume-link') || '').toString();
        var cvCode = (payload.cvCode || dmV4RowCvCode($row)).toString();
        var $stack = $row.find('.dm-v4-iv-cv-stack');
        dmV4RenderCvStack($row, {
            resumeLink: resumeLink,
            codedCvLink: codedCvLink,
            cvCode: cvCode,
            isResumeRedactAttempted: true,
            aiVerdict: ($row.attr('data-ai-verdict') || '').toString(),
            aiScore: parseInt($row.attr('data-ai-score'), 10) || 0,
            aiReasonFull: ($row.find('.dm-v4-iv-ai-reason-full').text() || '').trim()
        });
        $row.attr('data-coded-cv', codedCvLink);
        $row.attr('data-cv-code', cvCode);
        $row.attr('data-resume-link', resumeLink);
        if ($stack.length) {
            $stack.attr('data-coded-cv', codedCvLink);
            $stack.attr('data-resume-link', resumeLink);
        }
    }

    function dmV4RenderClientCvCell($row, state) {
        if (!isClientShareView()) return;
        var $cell = $row.find('.dm-v4-iv-cell--cv').first();
        if (!$cell.length) return;

        var codedCvLink = (state && state.codedCvLink != null ? state.codedCvLink : $row.attr('data-coded-cv') || '').toString().trim();
        var cvCode = (state && state.cvCode != null ? state.cvCode : $row.attr('data-cv-code') || '').toString().trim();
        var hasCoded = dmV4IsSummaryCodedCvLink(codedCvLink);

        $row.attr('data-coded-cv', hasCoded ? codedCvLink : '');
        $row.attr('data-cv-code', hasCoded ? (cvCode || 'CV') : '');

        if (hasCoded) {
            $cell.html(
                '<button type="button" class="dm-v4-iv-cv-link dm-v4-iv-cv-link--view" data-coded-cv="' +
                $('<span/>').text(codedCvLink).html() + '" data-cv-label="' + $('<span/>').text(cvCode || 'CV').html() +
                '" title="View coded resume summary">' + $('<span/>').text(cvCode || 'CV').html() + ' ↗</button>'
            );
        } else {
            $cell.html('<span class="dm-v4-muted">Not available</span>');
        }
    }

    function dmV4RenderCvStack($row, state, opts) {
        if (isClientShareView()) {
            dmV4RenderClientCvCell($row, state);
            return;
        }
        opts = opts || {};
        var canEdit = getIvCanEdit();
        var canUpload = getIvCanUploadResume();
        var $stack = $row.find('.dm-v4-iv-cv-stack');
        if (!$stack.length) return;

        var resumeLink = (state.resumeLink || '').toString().trim();
        var codedCvLink = (state.codedCvLink || '').toString().trim();
        var cvCode = (state.cvCode || 'CV').toString();
        var redactAttempted = !!state.isResumeRedactAttempted;
        var hasResume = dmV4HasResumeLink(resumeLink);
        var hasCoded = dmV4IsSummaryCodedCvLink(codedCvLink);
        var canScreen = hasResume || hasCoded;
        var hasScreening = (parseInt(state.aiScore, 10) || 0) > 0 ||
            !!(state.aiVerdict) || !!(state.aiReasonFull);

        $row.attr('data-resume-link', resumeLink);
        $row.attr('data-coded-cv', codedCvLink);
        $row.attr('data-cv-code', cvCode);
        $stack.attr('data-resume-link', resumeLink);
        $stack.attr('data-coded-cv', codedCvLink);

        var html = '';
        if (canUpload && !hasResume) {
            html += '<button type="button" class="dm-v4-iv-cv-btn dm-v4-iv-cv-btn--upload" title="Upload interviewer resume (PDF, DOC, or DOCX)">' +
                '<i class="fa fa-upload" aria-hidden="true"></i> Upload Resume</button>';
        } else if (hasResume) {
            html += '<a class="dm-v4-iv-cv-link dm-v4-iv-cv-link--resume" target="_blank" rel="noopener" href="' +
                $('<span/>').text(resumeLink).html().replace(/ /g, '%20') +
                '" title="Open interviewer resume">Resume ↗</a>';
            if (hasCoded) {
                html += '<button type="button" class="dm-v4-iv-cv-link dm-v4-iv-cv-link--view" data-coded-cv="' +
                    $('<span/>').text(codedCvLink).html() + '" data-resume-link="' + $('<span/>').text(resumeLink).html() +
                    '" data-cv-label="' + $('<span/>').text(cvCode).html() + '" title="View AI resume summary">Summary ↗</button>';
            } else if (canEdit) {
                html += '<button type="button" class="dm-v4-iv-cv-btn dm-v4-iv-cv-btn--summary" data-resume-link="' +
                    $('<span/>').text(resumeLink).html() + '" title="Generate AI resume summary and save as coded CV">' +
            
                    '<span class="dm-v4-iv-cv-btn__text"><span class="dm-v4-iv-cv-btn__title">Generate</span>' +
                    '<span class="dm-v4-iv-cv-btn__sub">Resume Summary</span></span></button>';
            }
        } else if (hasCoded) {
            html += '<button type="button" class="dm-v4-iv-cv-link dm-v4-iv-cv-link--view" data-coded-cv="' +
                $('<span/>').text(codedCvLink).html() + '" data-resume-link="' + $('<span/>').text(resumeLink).html() +
                '" data-cv-label="' + $('<span/>').text(cvCode).html() + '" title="View AI resume summary">' +
                $('<span/>').text(cvCode).html() + ' ↗</button>';
        } else if (redactAttempted && !hasCoded) {
            html += '<span class="dm-v4-muted">—</span>';
        } else if (!hasCoded) {
            html += '<span class="dm-v4-muted">No coded CV</span>';
        }
        $stack.html(html);
        if (!opts.skipVerdictSync) {
            dmV4EnsureVerdictScreenButton($row, {
                canScreen: canScreen,
                hasScreening: hasScreening
            });
        }
    }

    function dmV4EnsureVerdictScreenButton($row, opts) {
        opts = opts || {};
        var canEdit = getIvCanEdit();
        var canScreen = opts.canScreen;
        if (canScreen === undefined) {
            var resumeLink = ($row.data('resume-link') || '').toString();
            var codedCv = ($row.data('coded-cv') || '').toString();
            canScreen = !!(resumeLink || codedCv);
        }
        var hasScreening = opts.hasScreening;
        if (hasScreening === undefined) {
            hasScreening = dmV4RowHasScreening($row);
        }
        var $stack = $row.find('.dm-v4-iv-verdict-stack');
        if (!$stack.length) return;
        var $btn = dmV4GetRowScreenBtn($row);
        if (canEdit && canScreen) {
            if (!$btn.length) {
                $btn = $('<button type="button" class="dm-v4-iv-cv-btn dm-v4-iv-screen-btn" title="Run AI resume screening for this interviewer"></button>');
                $stack.append($btn);
            }
            dmV4SetScreenBtnHtml($btn, hasScreening ? 'refresh' : 'search', hasScreening ? 'Regenerate' : 'Screen Resume');
            $btn.show();
        } else if ($btn.length) {
            $btn.remove();
        }
        dmV4SyncDrawerScreenBtnVisibility($row);
    }

    function dmV4RefreshIvCvState($row, opts) {
        opts = opts || {};
        var cfg = getDmIvCvConfig();
        if (!cfg.cvStateUrl) return $.Deferred().reject().promise();
        var irfId = dmV4ResolveRowIrfId($row);
        var userId = parseInt($row.attr('data-user-id'), 10);
        if (!irfId || !userId) return $.Deferred().reject().promise();
        var requestData = { irfId: irfId, userId: userId };
        if (isClientShareView()) {
            requestData.Type = getClientShareTypeParam();
        }
        return $.getJSON(cfg.cvStateUrl, requestData).done(function (state) {
            if (state && state.ok) {
                if (isClientShareView()) {
                    dmV4RenderClientCvCell($row, state);
                    return;
                }
                var rowCoded = ($row.attr('data-coded-cv') || '').toString().trim();
                if (!state.codedCvLink && rowCoded && dmV4IsSummaryCodedCvLink(rowCoded)) {
                    state.codedCvLink = rowCoded;
                    state.cvCode = ($row.attr('data-cv-code') || dmV4RowCvCode($row)).toString();
                    state.isResumeRedactAttempted = true;
                }
                dmV4RenderCvStack($row, state, opts.cvOnly ? { skipVerdictSync: true } : undefined);
                if (!opts.cvOnly) {
                    if (state.futureAssignedCount !== undefined && state.futureAssignedCount !== null) {
                        setIvFutureAssignedDisplay($row, state.futureAssignedCount);
                    }
                    var hasScreening = (parseInt(state.aiScore, 10) || 0) > 0 ||
                        !!(state.aiVerdict) || !!(state.aiReasonFull);
                    dmV4EnsureVerdictScreenButton($row, {
                        canScreen: !!(state.resumeLink || state.codedCvLink),
                        hasScreening: hasScreening
                    });
                    if (state.aiVerdict || state.aiScore || state.aiReasonFull) {
                        dmV4ApplyScreeningResult($row, {
                            aiVerdict: state.aiVerdict,
                            aiScore: state.aiScore,
                            reasoning: state.aiReasonFull
                        });
                    }
                }
            }
        });
    }

    function dmV4ResetUploadResumeModal() {
        var filesInput = document.getElementsByName('ResumeFiles')[0];
        if (filesInput) filesInput.value = '';
        var $names = $('#filesList > #files-names');
        if ($names.length) $names.empty();
    }

    function patchDmV4UploadResume() {
        if (window._dmV4UploadResumePatched || typeof window.uploadResume !== 'function') return;
        window.uploadResume = function () {
            var pendingRow = window.dmV4PendingUploadRow;
            var formData = new FormData();
            var filesInput = document.getElementsByName('ResumeFiles')[0];
            if (!filesInput || !filesInput.files.length) {
                if (typeof swal !== 'undefined') swal.fire('Please select a PDF, DOC, or DOCX resume to upload.');
                return;
            }
            for (var i = 0; i < filesInput.files.length; i++) {
                var fileName = (filesInput.files[i].name || '').toLowerCase();
                if (!/\.(pdf|doc|docx)$/.test(fileName)) {
                    if (typeof swal !== 'undefined') swal.fire('Please select a PDF, DOC, or DOCX resume to upload.');
                    return;
                }
                formData.append('ResumeFiles', filesInput.files[i]);
            }
            formData.append('UserId', typeof interviewerUserId !== 'undefined' ? interviewerUserId : '');
            formData.append('InterviewerEmailId', typeof interviewerEmailId !== 'undefined' ? interviewerEmailId : '');
            if (pendingRow && pendingRow.length) {
                formData.append('IrfId', pendingRow.data('irf-id') || '');
            }
            var $btn = $('#btnUpload');
            $btn.prop('disabled', true);
            $.ajax({
                type: 'POST',
                url: '/IRF/UploadInterviewerResume',
                data: formData,
                contentType: false,
                enctype: 'multipart/form-data',
                processData: false,
                success: function (result) {
                    var msg = (result || '').toString();
                    var failed = /failed|unauthorized|please select|valid file/i.test(msg);
                    if (typeof swal !== 'undefined') {
                        swal.fire(msg, '', failed ? 'error' : 'success');
                    }
                    if (failed) return;
                    dmV4ResetUploadResumeModal();
                    if (typeof closeUploadResumeModal === 'function') closeUploadResumeModal();
                    if (pendingRow && pendingRow.length) {
                        dmV4RefreshIvCvState(pendingRow, { cvOnly: true });
                        window.dmV4PendingUploadRow = null;
                    } else {
                        location.reload(true);
                    }
                },
                error: function () {
                    if (typeof swal !== 'undefined') swal.fire('Upload failed.', '', 'error');
                },
                complete: function () {
                    $btn.prop('disabled', false);
                }
            });
        };
        window._dmV4UploadResumePatched = true;
    }

    function patchDmV4ResumeSummaryDrawerClose() {
        if (window._dmV4ResumeSummaryClosePatched) return;
        var original = window.closeResumeSummaryDrawer;
        if (typeof original !== 'function') return;
        window.closeResumeSummaryDrawer = function () {
            var sess = window.dmV4ResumeSummarySession;
            if (sess && sess.$row && sess.$row.length && sess.pendingPayload) {
                dmV4ApplyCvSaveResult(sess.$row, sess.pendingPayload);
            }
            window.dmV4ResumeSummarySession = { $row: null, pendingPayload: null };
            return original.apply(this, arguments);
        };
        window._dmV4ResumeSummaryClosePatched = true;
    }
    window.patchDmV4ResumeSummaryDrawerClose = patchDmV4ResumeSummaryDrawerClose;

    function getPolicyBar() {
        return $('#dmV4IvPolicyBar');
    }

    /** jQuery .data() coerces data-lock="1" to number 1 — never use === '1' alone. */
    function isPolicyLockOn($bar) {
        $bar = $bar && $bar.length ? $bar : getPolicyBar();
        if (!$bar.length) return false;
        var attrVal = $bar.attr('data-lock');
        if (attrVal === '1') return true;
        if (attrVal === '0') return false;
        var dataVal = $bar.data('lock');
        return dataVal === 1 || dataVal === '1' || dataVal === true;
    }

    function setPolicyLockOn($bar, lockOn) {
        $bar = $bar && $bar.length ? $bar : getPolicyBar();
        if (!$bar.length) return;
        var val = lockOn ? '1' : '0';
        $bar.attr('data-lock', val).data('lock', val);
        $('#dmV4IvPanel').attr('data-lock', val);
    }

    function setAssignmentPolicy($bar, policy) {
        $bar = $bar && $bar.length ? $bar : getPolicyBar();
        if (!$bar.length) return;
        var val = String(policy || '').toLowerCase() === 'limited' ? 'limited' : 'free';
        $bar.attr('data-policy', val).data('policy', val);
    }

    function getAssignmentPolicy() {
        var $bar = getPolicyBar();
        if (!$bar.length) return 'free';
        var attrVal = ($bar.attr('data-policy') || '').toString().toLowerCase();
        if (attrVal === 'limited' || attrVal === 'free') return attrVal;
        var dataVal = ($bar.data('policy') || '').toString().toLowerCase();
        return dataVal === 'limited' ? 'limited' : 'free';
    }

    function readRowAssignedCount($row) {
        var assigned = parseInt($row.attr('data-future-assigned'), 10);
        if (isNaN(assigned)) assigned = parseInt($row.attr('data-assigned'), 10);
        if (isNaN(assigned)) {
            assigned = parseInt($row.find('.dm-v4-iv-capacity').attr('data-assigned'), 10);
        }
        return isNaN(assigned) ? 0 : assigned;
    }

    /** Re-render every Interview Limit cell from current policy + row cap attrs (Free↔Limit). */
    function refreshInterviewLimitCellsForPolicy() {
        $('.dm-v4-iv-row').each(function () {
            var $row = $(this);
            var $wrap = $row.find('.dm-v4-iv-capacity');
            if (!$wrap.length) return;
            renderCapacityCell($wrap, null, readRowAssignedCount($row), isRowCapExplicit($row));
            updateIvRowStatus($row);
        });
    }

    function getIvCanEdit() {
        var $panel = $('#dmV4IvPanel');
        if ($panel.data('can-edit') === 1 || $panel.data('can-edit') === '1') return true;
        var $bar = getPolicyBar();
        return $bar.length && ($bar.data('can-edit') === 1 || $bar.data('can-edit') === '1');
    }

    /** Same roles as /IRF/UploadInterviewerResume and EditIRFInterviewers. */
    function getIvCanUploadResume() {
        var $panel = $('#dmV4IvPanel');
        if ($panel.length) {
            var panelVal = $panel.attr('data-can-upload-resume');
            if (panelVal === '1') return true;
            if (panelVal === '0') return false;
            if ($panel.data('can-upload-resume') === 1 || $panel.data('can-upload-resume') === '1') return true;
        }
        return false;
    }

    function dmV4HasResumeLink(link) {
        return !!(link && String(link).trim());
    }

    function getIvCanEditSurcharge() {
        var $panel = $('#dmV4IvPanel');
        if ($panel.data('can-edit-surcharge') === 1 || $panel.data('can-edit-surcharge') === '1') return true;
        return false;
    }

    function getDefaultCap() {
        return parseInt($('#dmV4DefaultCapVal').text(), 10) || 20;
    }

    function saveDmIvPolicy() {
        var $bar = getPolicyBar();
        if (!$bar.length || ($bar.data('can-edit') !== '1' && $bar.data('can-edit') !== 1)) return;
        $.post('/IRF/SaveDmIvPolicy', {
            irfId: parseInt($bar.data('irf-id'), 10),
            dmIvLockNew: isPolicyLockOn($bar),
            dmAssignmentPolicy: getAssignmentPolicy(),
            dmDefaultInterviewCap: getDefaultCap()
        }).fail(function () {
            if (typeof swal !== 'undefined') swal.fire('Failed to save policy settings.', '', 'error');
        });
    }

    function syncPolicyBarControls() {
        var $bar = getPolicyBar();
        if (!$bar.length) return;
        var lock = isPolicyLockOn($bar);
        var policy = getAssignmentPolicy();
        var limited = policy === 'limited';
        var canEdit = $bar.data('can-edit') === '1' || $bar.data('can-edit') === 1;
        var cap = parseInt($bar.data('default-cap'), 10);
        if (isNaN(cap) || cap < 1) cap = getDefaultCap();
        $bar.find('[data-segment="lock"] .dm-v4-segment-btn').removeClass('is-on')
            .filter('[data-value="' + (lock ? 'on' : 'off') + '"]').addClass('is-on');
        $bar.find('[data-segment="assign"] .dm-v4-segment-btn').removeClass('is-on')
            .filter('[data-value="' + policy + '"]').addClass('is-on');
        $('#dmV4DefaultCapVal').text(cap);
        $('#dmV4DefaultCapStepper').prop('hidden', !limited);
        $('#dmV4DefaultCapHint').prop('hidden', !limited);
        $('#dmV4ApplyCapAllBtn').prop('hidden', !limited);
        $('#dmV4DefaultCapUnlimited').prop('hidden', limited);
        $('#dmV4DefaultCapStepper .dm-v4-stepper-btn').prop('disabled', !canEdit || !limited);
        $('#dmV4ApplyCapAllBtn').prop('disabled', !canEdit || !limited);
        $('#dmV4DefaultCapCol')
            .toggleClass('dm-v4-iv-policy-col--cap-disabled', false)
            .toggleClass('dm-v4-iv-policy-col--cap-free', !limited);
    }

    function updatePolicySubtext() {
        var $bar = getPolicyBar();
        if (!$bar.length) return;
        var lock = isPolicyLockOn($bar);
        var limited = getAssignmentPolicy() === 'limited';
        $('#dmV4IvLockSub').text(lock
            ? 'New interviewers join as RB Vet pending. Client Vet auto-cascades to pending until Risebird clears them.'
            : 'New interviewers are auto-RB-vetted and auto-Client-approved. Override per row if needed.');
        $('#dmV4IvPolicySub').text(limited
            ? 'Enforce per-interviewer caps. Edit each interviewer’s limit inline in the table.'
            : 'Active interviewers can be assigned to any interview without further review.');
        $('#dmV4DefaultCapSub').text(limited
            ? 'Used for newly added interviewers. Existing caps stay until you bulk-apply.'
            : 'Not used while assignment policy is Free.');
        $('#dmV4IvPanel').attr('data-cap-enforced', limited ? '1' : '0');
        syncPolicyBarControls();
        refreshInterviewLimitCellsForPolicy();
        applyIvTabFilter();
        refreshIvTabCounts();
    }

    function isCapEnforced() {
        return $('#dmV4IvPanel').attr('data-cap-enforced') === '1';
    }

    function isChannelLocked() {
        return $('#dmV4IvPanel').attr('data-lock') === '1';
    }

    function getChannelDefaultCap() {
        var $bar = getPolicyBar();
        var n = parseInt($bar.data('default-cap'), 10);
        if (isNaN(n) || n < 1) n = 20;
        return n;
    }

    /** Parsed per-row cap when persisted; null = unset (not zero). */
    function readRowExplicitCapacity($row) {
        if ($row.attr('data-cap-configured') !== '1') return null;
        var raw = ($row.attr('data-capacity') || '').toString().trim();
        if (raw === '') return null;
        var n = parseInt(raw, 10);
        return isNaN(n) ? null : n;
    }

    /** True when TransInterviewer.InterviewCapacity is persisted (explicit override). */
    function isRowCapExplicit($row) {
        return readRowExplicitCapacity($row) !== null;
    }

    function isRowExplicitZeroCap($row) {
        var cap = readRowExplicitCapacity($row);
        return cap !== null && cap === 0;
    }

    /**
     * Unified cap decision tree — mirrors DmIvCapHelper.GetCapStateForRow (Option B).
     * Policy gates enforcement; stored caps are never cleared on Free↔Limit transitions.
     */
    function getCapStateForRow(policy, explicitCap, assigned, channelDefault) {
        var limited = policy === 'limited';
        assigned = parseInt(assigned, 10) || 0;
        channelDefault = parseInt(channelDefault, 10);
        if (isNaN(channelDefault) || channelDefault < 1) channelDefault = 20;

        if (!limited) {
            return {
                showUnlimited: true,
                isCapEnforced: false,
                isExplicitZero: false,
                usesDefault: false,
                effectiveCap: 0,
                isCapReached: false,
                assignmentAllowed: true
            };
        }

        if (explicitCap !== null && explicitCap === 0) {
            return {
                showUnlimited: false,
                isCapEnforced: true,
                isExplicitZero: true,
                usesDefault: false,
                effectiveCap: 0,
                isCapReached: true,
                assignmentAllowed: false
            };
        }

        var usesDefault = explicitCap === null;
        var effective = usesDefault ? channelDefault : explicitCap;
        var reached = assigned >= effective;
        return {
            showUnlimited: false,
            isCapEnforced: true,
            isExplicitZero: false,
            usesDefault: usesDefault,
            effectiveCap: effective,
            isCapReached: reached,
            assignmentAllowed: !reached
        };
    }

    function getCapStateForRowFromRow($row) {
        var assigned = parseInt($row.attr('data-assigned'), 10) || 0;
        return getCapStateForRow(
            getAssignmentPolicy(),
            readRowExplicitCapacity($row),
            assigned,
            getChannelDefaultCap()
        );
    }

    function rowUsesChannelDefault($row) {
        return getCapStateForRowFromRow($row).usesDefault;
    }

    function readRowDisplayCapacity($row) {
        var state = getCapStateForRowFromRow($row);
        if (state.showUnlimited) return null;
        return state.effectiveCap;
    }

    function shouldShowUnlimitedCap($row) {
        return getCapStateForRowFromRow($row).showUnlimited;
    }

    function capBarClass(assigned, capacity) {
        if (capacity <= 0) return 'ok';
        var pct = Math.round((assigned / Math.max(1, capacity)) * 100);
        if (assigned >= capacity) return 'full';
        if (pct >= 80) return 'near';
        return 'ok';
    }

    /* Reference dm-shared.jsx I.edit / I.plus */
    var DM_V4_SURCHARGE_MAX = 500;
    var DM_V4_EDIT_ICON = '<span class="dm-v4-money-edit-icon" aria-hidden="true">' +
        '<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.4" width="9" height="9">' +
        '<path d="M3 13l.7-3 6.6-6.6a1 1 0 011.4 0l1 1a1 1 0 010 1.4L6.1 12.3 3 13z" stroke-linejoin="round"/></svg></span>';

    var DM_V4_PLUS_ICON = '<span class="dm-v4-cap-set-btn__icon" aria-hidden="true">' +
        '<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" width="10" height="10">' +
        '<path d="M8 3.5v9M3.5 8h9" stroke-linecap="round"/></svg></span>';

    function buildCapPillHtml(capacity, usesDefault) {
        var defaultHint = usesDefault
            ? '<span class="dm-v4-cap-pill-default">(default)</span>'
            : '';
        return '<button type="button" class="dm-v4-cap-pill v4-cap-pill" data-cap="' + capacity + '" title="Click to set this interviewer\'s interview limit">' +
            '<span class="dm-v4-cap-pill-val">' + capacity + '</span>' + defaultHint + DM_V4_EDIT_ICON + '</button>';
    }

    function moneyDisplayValue(value, kind) {
        var v = Math.max(0, parseInt(value, 10) || 0);
        if (kind === 'rate' || kind === 'surcharge') {
            return v > 0 ? v.toLocaleString('en-IN') : '0';
        }
        return String(v);
    }

    function getSurchargeMax() {
        return DM_V4_SURCHARGE_MAX;
    }

    function validateSurchargeAmount(surcharge) {
        if (surcharge < 0) {
            return 'Surcharge cannot be negative.';
        }
        if (surcharge > DM_V4_SURCHARGE_MAX) {
            return 'Maximum surcharge is ₹500';
        }
        return null;
    }

    function clearSurchargeInlineError($row) {
        $row.find('.dm-v4-iv-cell--surcharge .dm-v4-surcharge-error').remove();
    }

    function showSurchargeInlineError($row, message) {
        var $cell = $row.find('.dm-v4-iv-cell--surcharge');
        clearSurchargeInlineError($row);
        $('<p class="dm-v4-surcharge-error" role="alert"></p>')
            .text(message || 'Maximum surcharge is ₹500')
            .appendTo($cell);
    }

    function buildMoneyPillHtml(value, kind, attrs) {
        attrs = attrs || {};
        var v = Math.max(0, parseInt(value, 10) || 0);
        var cls = 'dm-v4-money-pill v4-money-pill dm-v4-' + kind + '-pill';
        var display = moneyDisplayValue(v, kind);
        var inner = (kind === 'rate' || kind === 'surcharge')
            ? '<span class="dm-v4-money-unit">₹</span><span class="dm-v4-money-val">' + display + '</span>'
            : '<span class="dm-v4-money-val">' + display + '</span><span class="dm-v4-money-unit">%</span>';
        var title = kind === 'rate'
            ? 'Per-interview rate (INR)'
            : 'Surcharge amount in INR (max ₹500)';
        var data = ' data-value="' + v + '"';
        if (attrs.userId != null) data += ' data-user-id="' + attrs.userId + '"';
        if (attrs.irfId != null) data += ' data-irf-id="' + attrs.irfId + '"';
        return '<button type="button" class="' + cls + '"' + data + ' title="' + title + '">' + inner + DM_V4_EDIT_ICON + '</button>';
    }

    /**
     * Reference V4MoneyPill: click pill → inline number input → blur/Enter commits, Escape reverts.
     */
    function startMoneyPillEdit($pill) {
        if (!$pill.length || $pill.data('dm-v4-editing')) return;
        if ($pill.hasClass('dm-v4-rate-pill')) return;
        $pill.data('dm-v4-editing', true);

        var kind = 'surcharge';
        var $row = $pill.closest('.dm-v4-iv-row');
        clearSurchargeInlineError($row);
        var orig = parseInt($pill.attr('data-value'), 10);
        if (isNaN(orig)) orig = 0;
        var meta = {
            userId: $pill.data('userId') || $pill.data('user-id'),
            irfId: $pill.data('irfId') || $pill.data('irf-id')
        };

        var $input = $('<input type="number" class="dm-v4-inline-edit-input" min="0" max="' + DM_V4_SURCHARGE_MAX + '" />').val(orig);

        $pill.replaceWith($input);
        $input.focus().select();

        var finished = false;
        var finish = function (doCommit) {
            if (finished) return;
            finished = true;
            var n = doCommit ? Math.max(0, parseInt($input.val(), 10) || 0) : orig;
            if (doCommit) {
                var validationError = validateSurchargeAmount(n);
                if (validationError) {
                    showSurchargeInlineError($row, validationError);
                    doCommit = false;
                    n = orig;
                } else {
                    clearSurchargeInlineError($row);
                }
            }
            var attrs = { userId: meta.userId, irfId: meta.irfId };
            $input.replaceWith(buildMoneyPillHtml(n, kind, attrs));

            if (!doCommit) return;

            $row.find('input[name*="Surcharge"]').val(n);
            saveSurcharge($row, n);
        };

        $input.on('blur.dmV4MoneyEdit', function () {
            finish(true);
        });
        $input.on('keydown.dmV4MoneyEdit', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                finish(true);
            }
            if (e.key === 'Escape') {
                e.preventDefault();
                finish(false);
            }
        });
        /* Avoid accidental form submit while editing a pill */
        $input.on('click.dmV4MoneyEdit', function (e) {
            e.stopPropagation();
        });
    }

    function updateSurchargeExtra($row) {
        /* Surcharge is stored/displayed as INR; no derived percentage line. */
        var $cell = $row.find('.dm-v4-iv-cell--surcharge');
        if ($cell.length) {
            $cell.find('.dm-v4-surcharge-extra').remove();
        }
    }

    /**
     * Reference V4CapacityCell: click cap pill or Set limit → inline number input → blur/Enter saves.
     */
    function startCapInlineEdit($row, origVal) {
        if (!getIvCanEdit()) return;
        var $wrap = $row.find('.dm-v4-iv-capacity');
        if ($wrap.find('.dm-v4-inline-edit-input--cap').length) return;

        var assigned = parseInt($wrap.attr('data-assigned'), 10);
        if (isNaN(assigned)) assigned = parseInt($row.attr('data-assigned'), 10) || 0;
        var orig = origVal;
        if (orig == null || isNaN(orig)) {
            orig = readRowDisplayCapacity($row);
            if (orig == null || isNaN(orig)) orig = getChannelDefaultCap();
        }

        var $pill = $wrap.find('.dm-v4-cap-pill');
        var $setBtn = $wrap.find('.dm-v4-cap-set-btn');
        var $input = $('<input type="number" class="dm-v4-inline-edit-input dm-v4-inline-edit-input--cap dm-v4-mono" min="0" max="99" />').val(orig);
        var finished = false;
        var prevExplicit = isRowCapExplicit($row);
        var prevDisplay = readRowDisplayCapacity($row);
        var prevUsesDefault = rowUsesChannelDefault($row);

        var finish = function (doCommit) {
            if (finished) return;
            finished = true;
            $input.off('.dmV4CapEdit');
            var n = doCommit ? Math.max(0, Math.min(99, parseInt($input.val(), 10) || 0)) : orig;
            if (doCommit) {
                renderCapacityCell($wrap, n, assigned, true, false);
                saveCapacity($row, n, prevDisplay, prevExplicit, prevUsesDefault);
            } else {
                renderCapacityCell($wrap, prevDisplay, assigned, prevExplicit, prevUsesDefault);
            }
        };

        if ($pill.length) {
            $pill.replaceWith($input);
        } else if ($setBtn.length) {
            $setBtn.replaceWith(
                '<div class="dm-v4-cap-display dm-v4-cap-display--temp">' +
                '<span class="dm-v4-mono dm-v4-iv-cap-assigned">' + assigned + '</span>' +
                '<span class="dm-v4-iv-cap-sep">/</span></div>'
            );
            $wrap.find('.dm-v4-cap-display--temp').append($input).append('<span class="dm-v4-cap-iv-suffix">iv</span>');
        } else {
            return;
        }

        $input.focus().select();
        $input.on('blur.dmV4CapEdit', function () { finish(true); });
        $input.on('keydown.dmV4CapEdit', function (e) {
            if (e.key === 'Enter') { e.preventDefault(); finish(true); }
            if (e.key === 'Escape') { e.preventDefault(); finish(false); }
        });
    }

    function syncCapacityCellUi($wrap, displayCapacity, assigned, capExplicit) {
        var $row = $wrap.closest('.dm-v4-iv-row');
        if (typeof capExplicit === 'undefined') {
            capExplicit = isRowCapExplicit($row);
        }
        if (capExplicit) {
            displayCapacity = readRowExplicitCapacity($row);
            if (displayCapacity == null) {
                capExplicit = false;
            } else {
                $wrap.attr('data-capacity', displayCapacity).attr('data-cap-configured', '1').attr('data-cap-from-default', '0');
                $row.attr('data-capacity', displayCapacity).attr('data-cap-configured', '1').attr('data-cap-from-default', '0');
                $wrap.attr('data-cap-effective', displayCapacity);
                $row.attr('data-cap-effective', displayCapacity);
            }
        }
        if (!capExplicit) {
            $wrap.attr('data-capacity', '').attr('data-cap-configured', '0');
            $row.attr('data-capacity', '').attr('data-cap-configured', '0');
            if (displayCapacity != null && !isNaN(displayCapacity)) {
                $wrap.attr('data-cap-effective', displayCapacity);
                $row.attr('data-cap-effective', displayCapacity);
                $row.attr('data-cap-from-default', rowUsesChannelDefault($row) ? '1' : '0');
                $wrap.attr('data-cap-from-default', $row.attr('data-cap-from-default'));
            } else {
                $wrap.attr('data-cap-effective', '');
                $row.attr('data-cap-effective', '');
            }
        }
        $wrap.attr('data-assigned', assigned);
        $row.attr('data-assigned', assigned);

        if (!$wrap.find('.dm-v4-cap-display').length && !$wrap.find('.dm-v4-cap-set-btn').length && !$wrap.find('.dm-v4-cap-unlimited').length) {
            renderCapacityCell($wrap, displayCapacity, assigned, capExplicit);
            return;
        }

        renderCapacityCell($wrap, displayCapacity, assigned, capExplicit);
    }

    function renderCapacityCell($wrap, displayCapacity, assigned, capExplicit, usesDefaultOverride) {
        var $row = $wrap.closest('.dm-v4-iv-row');
        var canEdit = getIvCanEdit();
        assigned = parseInt(assigned, 10);
        if (isNaN(assigned)) assigned = parseInt($row.attr('data-assigned'), 10) || 0;

        var explicitCap = readRowExplicitCapacity($row);
        var state = getCapStateForRow(getAssignmentPolicy(), explicitCap, assigned, getChannelDefaultCap());
        if (typeof usesDefaultOverride === 'boolean') {
            state = $.extend({}, state, { usesDefault: usesDefaultOverride });
        }

        if (explicitCap !== null) {
            $wrap.attr('data-capacity', explicitCap).attr('data-cap-configured', '1').attr('data-cap-from-default', '0');
            $row.attr('data-capacity', explicitCap).attr('data-cap-configured', '1').attr('data-cap-from-default', '0');
            $wrap.attr('data-cap-effective', explicitCap);
            $row.attr('data-cap-effective', explicitCap);
        } else {
            $wrap.attr('data-capacity', '').attr('data-cap-configured', '0');
            $row.attr('data-capacity', '').attr('data-cap-configured', '0');
            if (!state.showUnlimited) {
                $wrap.attr('data-cap-effective', state.effectiveCap);
                $row.attr('data-cap-effective', state.effectiveCap);
            } else {
                $wrap.attr('data-cap-effective', '');
                $row.attr('data-cap-effective', '');
            }
            $row.attr('data-cap-from-default', state.usesDefault ? '1' : '0');
            $wrap.attr('data-cap-from-default', state.usesDefault ? '1' : '0');
        }
        $wrap.attr('data-assigned', assigned);
        $row.attr('data-assigned', assigned);

        if (state.showUnlimited) {
            $wrap.html('<span class="dm-v4-cap-unlimited" title="No interview limit configured">Unlimited</span>');
            $row.removeClass('dm-v4-iv-row--cap-full');
            return;
        }

        if (state.isExplicitZero) {
            if (canEdit) {
                $wrap.html('<button type="button" class="dm-v4-cap-set-btn" title="Interview limit is 0 — interviewer cannot be assigned">' +
                    DM_V4_PLUS_ICON + 'Set limit</button>' +
                    '<div class="dm-v4-cap-status dm-v4-cap-status--full">Cannot be assigned</div>');
            } else {
                $wrap.html('<span class="dm-v4-cap-unlimited dm-v4-cap-unlimited--blocked">0 · Cannot assign</span>');
            }
            $row.addClass('dm-v4-iv-row--cap-full');
            return;
        }

        var displayCap = state.effectiveCap;
        var usesDefault = state.usesDefault;
        var pct = displayCap > 0 ? Math.min(100, Math.round((assigned / Math.max(1, displayCap)) * 100)) : 0;
        var bar = capBarClass(assigned, displayCap);
        var full = state.isCapReached;
        var near = !full && pct >= 80;
        var html = '<div class="dm-v4-cap-display">';
        html += '<span class="dm-v4-mono dm-v4-iv-cap-assigned">' + assigned + '</span><span class="dm-v4-iv-cap-sep">/</span>';
        if (canEdit) {
            html += buildCapPillHtml(displayCap, usesDefault);
        } else {
            html += '<span class="dm-v4-mono dm-v4-cap-readonly">' + displayCap +
                (usesDefault ? ' <span class="dm-v4-cap-pill-default">(default)</span>' : '') + '</span>';
        }
        html += '</div>';
        if (displayCap > 0) {
            html += '<div class="dm-v4-cap-bar"><span class="dm-v4-cap-bar-fill dm-v4-cap-bar-fill--' + bar + '" style="width:' + pct + '%"></span></div>';
        }
        if (state.isCapEnforced && full) {
            html += '<div class="dm-v4-cap-status dm-v4-cap-status--full">FULL · CAPPED</div>';
        } else if (state.isCapEnforced && near) {
            html += '<div class="dm-v4-cap-status dm-v4-cap-status--near">NEAR LIMIT</div>';
        }
        $wrap.html(html);
        $row.toggleClass('dm-v4-iv-row--cap-full', full);
    }

    function saveCapacity($row, capacity, revertDisplay, revertExplicit, revertUsesDefault) {
        var transId = parseInt($row.data('trans-id'), 10);
        if (!transId) return;
        var $wrap = $row.find('.dm-v4-iv-capacity');
        var assigned = parseInt($row.data('assigned'), 10) || 0;

        function revertCapUi() {
            if (revertExplicit) {
                $row.attr('data-capacity', revertDisplay).attr('data-cap-configured', '1').attr('data-cap-from-default', '0');
            } else {
                $row.attr('data-capacity', '').attr('data-cap-configured', '0');
                $row.attr('data-cap-from-default', revertUsesDefault ? '1' : '0');
            }
            if (revertDisplay != null) {
                $row.attr('data-cap-effective', revertDisplay);
            }
            renderCapacityCell($wrap, revertDisplay, assigned, revertExplicit, revertUsesDefault);
        }

        $.post('/IRF/UpdateInterviewerCapacity', {
            transInterviewerId: transId,
            interviewCapacity: capacity
        }, function (res) {
            if (res && res.success) {
                var cap = res.interviewCapacity;
                $row.attr('data-capacity', cap).attr('data-cap-configured', '1').attr('data-cap-from-default', '0');
                $row.attr('data-cap-effective', cap);
                renderCapacityCell($wrap, cap, assigned, true, false);
            } else {
                revertCapUi();
                if (typeof swal !== 'undefined') {
                    swal.fire('Could not save interview limit', (res && res.message) || '', 'error');
                }
            }
        }).fail(function () {
            revertCapUi();
            if (typeof swal !== 'undefined') {
                swal.fire('Could not save interview limit', 'Request failed.', 'error');
            }
        });
    }

    function saveSurcharge($row, surcharge) {
        var $pill = $row.find('.dm-v4-surcharge-pill');
        var userId = $pill.data('userId') || $pill.data('user-id') || $row.data('user-id');
        var irfId = $pill.data('irfId') || $pill.data('irf-id') || $row.data('irf-id');
        clearSurchargeInlineError($row);
        $.post('/IRF/SaveSurcharge', {
            userId: userId,
            surcharge: surcharge,
            irfId: irfId
        }, function (res) {
            var ok = res && (res.success === true || res.success === 'true');
            if (!ok) {
                var msg = (res && res.message) ? res.message : 'Failed to save surcharge.';
                showSurchargeInlineError($row, msg);
                return;
            }
            clearSurchargeInlineError($row);
            var saved = res.surcharge != null ? res.surcharge : surcharge;
            $pill = $row.find('.dm-v4-surcharge-pill');
            if ($pill.length) {
                $pill.attr('data-value', saved);
                $pill.find('.dm-v4-money-val').text(moneyDisplayValue(saved, 'surcharge'));
            }
            $row.find('input[name*="Surcharge"]').val(saved);
        });
    }

    function clearBulkCapError() {
        var $err = $('#dmV4BulkCapError');
        if (!$err.length) return;
        $err.attr('hidden', 'hidden').text('');
    }

    function showBulkCapError(message) {
        var $err = $('#dmV4BulkCapError');
        if (!$err.length) return;
        $err.text(message || 'Could not apply cap. Please try again.').removeAttr('hidden');
    }

    function openBulkCapModal() {
        var cap = getDefaultCap();
        var n = $('.dm-v4-iv-row').length;
        var $modal = $('#dmV4BulkCapModal');
        if (!$modal.length) return;
        if (!$modal.parent().is('body')) {
            $modal.appendTo('body');
        }
        clearBulkCapError();
        $('#dmV4BulkCapTitle').text('Set every interviewer’s cap to ' + cap + '?');
        $('#dmV4BulkCapText').text(
            'This overrides individual interview limits across all ' + n + ' interviewer' + (n === 1 ? '' : 's') +
            '. Per-interviewer edits made afterwards will still stick.'
        );
        $('#dmV4BulkCapConfirm').prop('disabled', false).text('Confirm');
        $modal.addClass('is-open').attr('aria-hidden', 'false');
        document.body.classList.add('dm-v4-bulk-cap-open');
    }

    function closeBulkCapModal() {
        $('#dmV4BulkCapModal').removeClass('is-open').attr('aria-hidden', 'true');
        document.body.classList.remove('dm-v4-bulk-cap-open');
    }

    /** Upgrade legacy rate/surcharge/cap fields to reference V4MoneyPill / V4CapacityCell markup. */
    function enhanceIvMoneyPills() {
        $('#dmV4IvPanel .dm-v4-iv-row').each(function () {
            var $row = $(this);
            var $sIn = $row.find('.dm-v4-iv-surcharge-input');
            if ($sIn.length && !$row.find('.dm-v4-surcharge-pill').length) {
                var sv = parseInt($sIn.val(), 10) || 0;
                $sIn.hide();
                $sIn.after(buildMoneyPillHtml(sv, 'surcharge', {
                    userId: $sIn.data('userId') || $sIn.data('user-id') || $row.data('user-id'),
                    irfId: $sIn.data('irfId') || $sIn.data('irf-id') || $row.data('irf-id')
                }));
                updateSurchargeExtra($row);
            }
            var $surchargePill = $row.find('.dm-v4-surcharge-pill');
            if ($surchargePill.length && !$surchargePill.find('.dm-v4-money-edit-icon').length) {
                $surchargePill.append(DM_V4_EDIT_ICON);
            }
            if ($surchargePill.length) {
                $surchargePill.attr('title', 'Surcharge amount in INR (max ₹500)');
            }
            var $capPill = $row.find('.dm-v4-cap-pill');
            if ($capPill.length && !$capPill.find('.dm-v4-money-edit-icon').length) {
                $capPill.append(DM_V4_EDIT_ICON);
            }
            var $capInput = $row.find('.dm-v4-cap-input');
            if ($capInput.length) {
                var $wrap = $capInput.closest('.dm-v4-iv-capacity');
                var cap = parseInt($capInput.val(), 10) || readRowDisplayCapacity($row) || 0;
                var assigned = parseInt($wrap.data('assigned'), 10) || 0;
                renderCapacityCell($wrap, cap, assigned, true, false);
            }
        });
    }

    function dmV4GetRowVideoKycUrl($row) {
        if (!$row || !$row.length) return '';
        return ($row.attr('data-video-link') || $row.data('videoLink') || '').toString().trim();
    }

    /** Ensure playable KYC control whenever a video URL exists (not gated on approval). */
    function dmV4EnsureKycPlayButton($row) {
        if (!$row || !$row.length) return;
        var url = dmV4GetRowVideoKycUrl($row);
        if (!url) return;
        var $nameRow = $row.find('.dm-v4-iv-name-row').first();
        if (!$nameRow.length) return;
        var $btn = $nameRow.find('.dm-v4-iv-kyc--btn').first();
        if ($btn.length) {
            $btn.attr('data-video-url', url);
            return;
        }
        var $static = $nameRow.find('.dm-v4-iv-kyc').not('.dm-v4-iv-kyc--btn').first();
        if (!$static.length) return;
        var approved = $row.attr('data-kyc-approved') === '1';
        var title = $static.attr('title') || (approved ? 'Video KYC · Verified' : 'Video KYC · Pending approval — click to view');
        var $newBtn = $('<button type="button" class="dm-v4-iv-kyc dm-v4-iv-kyc--btn"></button>');
        $newBtn.addClass(approved ? 'dm-v4-iv-kyc--ok' : 'dm-v4-iv-kyc--warn');
        $newBtn.attr({ 'data-video-url': url, title: title, 'aria-label': title });
        $newBtn.html($static.html());
        $static.replaceWith($newBtn);
    }

    function dmV4SyncVerdictDrawerKyc($row) {
        var $section = $('#dmV4VerdictDrawerKycSection');
        var $btn = $('#dmV4VerdictDrawerKycBtn');
        if (!$section.length || !$btn.length) return;
        var url = dmV4GetRowVideoKycUrl($row);
        if (!url) {
            $section.prop('hidden', true).hide();
            $btn.removeAttr('data-video-url');
            return;
        }
        var approved = $row && $row.attr('data-kyc-approved') === '1';
        $btn.attr('data-video-url', url);
        $('#dmV4VerdictDrawerKycSub').text(approved
            ? 'Verified · click to play'
            : 'Pending approval · click to play');
        $section.prop('hidden', false).show();
    }

    function openDmV4VideoKyc(url) {
        if (!url) return;
        var $overlay = $('#dmV4VideoKycOverlay');
        var $modal = $('#dmV4VideoKycModal');
        var $video = $('#dmV4VideoKycPlayer');
        var $iframe = $('#dmV4VideoKycIframe');
        var low = String(url).toLowerCase();
        $video.hide();
        $iframe.hide().attr('src', '');
        if (low.indexOf('.mp4') >= 0 || low.indexOf('.webm') >= 0 || low.indexOf('.ogg') >= 0 || low.indexOf('.org') >= 0) {
            $video.find('source').attr('src', url);
            $video[0].load();
            $video.show();
        } else {
            $iframe.attr('src', url).show();
        }
        $overlay.addClass('is-open').attr('aria-hidden', 'false');
        $modal.addClass('is-open').attr('aria-hidden', 'false');
    }

    function closeDmV4VideoKyc() {
        var $video = $('#dmV4VideoKycPlayer');
        if ($video.length && $video[0].pause) {
            $video[0].pause();
        }
        $('#dmV4VideoKycIframe').attr('src', '').hide();
        $video.hide().find('source').attr('src', '');
        $('#dmV4VideoKycOverlay, #dmV4VideoKycModal').removeClass('is-open').attr('aria-hidden', 'true');
    }

    function initIvEditableFields() {
        enhanceIvMoneyPills();
        $('.dm-v4-iv-row').each(function () {
            var $row = $(this);
            var $wrap = $row.find('.dm-v4-iv-capacity');
            var assigned = parseInt($row.attr('data-assigned'), 10);
            if (isNaN(assigned)) assigned = parseInt($wrap.attr('data-assigned'), 10) || 0;
            syncCapacityCellUi($wrap, readRowDisplayCapacity($row), assigned, isRowCapExplicit($row));
            updateSurchargeExtra($row);
        });
    }

    function initAutoQcBadgeTooltips() {
        // Touch/click toggle for auto-QC badge tooltip (CSS :hover handles desktop).
        $(document).off('click.dmV4AutoQcBadge', '.dm-v4-iv-inactive-reason--auto-qc')
            .on('click.dmV4AutoQcBadge', '.dm-v4-iv-inactive-reason--auto-qc', function (e) {
                // Allow clicks on the link inside the tooltip to propagate normally.
                if ($(e.target).is('a') || $(e.target).closest('a').length) return;
                var $badge = $(this);
                var wasOpen = $badge.hasClass('is-open');
                // Close any other open tooltips.
                $('.dm-v4-iv-inactive-reason--auto-qc.is-open').not($badge).removeClass('is-open');
                $badge.toggleClass('is-open', !wasOpen);
                e.stopPropagation();
            });
        $(document).off('click.dmV4AutoQcBadgeOut').on('click.dmV4AutoQcBadgeOut', function () {
            $('.dm-v4-iv-inactive-reason--auto-qc.is-open').removeClass('is-open');
        });
    }

    function initInterviewersV4() {
        setPolicyLockOn(getPolicyBar(), isPolicyLockOn(getPolicyBar()));
        initIvTabs();
        initInactiveSubTabs();
        initActiveSubTabs();
        initAutoQcBadgeTooltips();
        refreshIvBadges();
        updatePolicySubtext();
        initIvEditableFields();
        patchDmV4UploadResume();
        patchDmV4ResumeSummaryDrawerClose();
        syncDmV4IvFutureAssignedAll();
        dmV4InitIvActivityMetaAll();
        refreshDmV4IvPanelMetrics();
        $('.dm-v4-iv-row').each(function () {
            var $row = $(this);
            syncVetButtons($row);
            if (!isPanelCompactView()) {
                dmV4SyncScreenButtonLabels($row);
                dmV4EnsureKycPlayButton($row);
            }
        });
        refreshInactiveSubTabCounts();

        $(document).off('click.dmV4IvInsightsGlobal', '.dm-v4-iv-insights-btn--global').on('click.dmV4IvInsightsGlobal', '.dm-v4-iv-insights-btn--global', function (e) {
            e.preventDefault();
            if (isSmeView()) return;
            var $btn = $(this);
            var $row = $btn.closest('.dm-v4-iv-row');
            if (isPanelCompactView()) {
                var userId = parseInt($btn.data('user-id') || $row.data('user-id'), 10);
                var irfId = parseInt($row.data('irf-id'), 10);
                var nickname = ($btn.data('nickname') || $row.find('.dm-v4-iv-code').first().text().replace(/\s+/g, ' ').replace(/›/g, '').trim() || '').toString();
                if (!userId || !irfId) return;
                $.get('/IRF/GetClientShareInterviewerInsights', {
                    irfId: irfId,
                    userId: userId,
                    Type: isClientShareView() ? getClientShareTypeParam() : undefined
                }, function (data) {
                    openInterviewerInsightsModel();
                    $('#divInsightsmodel').html(data);
                    $('#txtheaderInsights').text(nickname ? nickname + (isSmeView() ? ' (SME view)' : ' (Client view)') : 'Interviewer');
                    $('#hidlastlogin').text($('#LastLogin').val() || '');
                });
                return;
            }
            var email = ($(this).data('email') || $row.data('email') || '').toString();
            if (!email) return;
            if (typeof InsightsDetails === 'function') {
                InsightsDetails(email);
            }
        });

        $(document).off('click.dmV4IvChannelInsights', '.dm-v4-iv-channel-insights-btn').on('click.dmV4IvChannelInsights', '.dm-v4-iv-channel-insights-btn', function (e) {
            e.preventDefault();
            if (isSmeView()) return;
            var $btn = $(this);
            var $row = $btn.closest('.dm-v4-iv-row');
            if (isPanelCompactView()) {
                var userId = parseInt($btn.data('user-id') || $row.data('user-id'), 10);
                var irfId = parseInt($btn.data('irf-id') || $row.data('irf-id'), 10);
                var irfName = ($btn.data('irf-id-name') || $row.data('irf-name') || '').toString();
                var nickname = ($row.find('.dm-v4-iv-code').first().text().replace(/\s+/g, ' ').replace(/›/g, '').trim() || '').toString();
                if (!userId || !irfId) return;
                $.get('/IRF/GetClientShareInterviewerInsights', {
                    irfId: irfId,
                    userId: userId,
                    Type: isClientShareView() ? getClientShareTypeParam() : undefined
                }, function (data) {
                    openInterviewerInsightsModel();
                    $('#divInsightsmodel').html(data);
                    $('#txtheaderInsights').text(nickname ? nickname + ' · ' + irfName : irfName);
                    $('#hidlastlogin').text($('#LastLogin').val() || '');
                });
                return;
            }
            var email = ($btn.data('email') || $row.data('email') || '').toString();
            var irfName = ($btn.data('irf-id-name') || $row.data('irf-name') || '').toString();
            if (!email) return;
            if (typeof InsightsIRFDetails === 'function') {
                InsightsIRFDetails(email, irfName);
            } else if (typeof InsightsDetails === 'function') {
                InsightsDetails(email, irfName);
            }
        });

        $(document).off('click.dmV4IvCvView', '.dm-v4-iv-cv-link--view').on('click.dmV4IvCvView', '.dm-v4-iv-cv-link--view', function (e) {
            e.preventDefault();
            var $btn = $(this);
            var $row = $btn.closest('.dm-v4-iv-row');
            window._dmV4CvSummaryRow = $row;
            var codedCv = ($btn.data('coded-cv') || $btn.closest('.dm-v4-iv-cv-stack').data('coded-cv') || $btn.closest('.dm-v4-iv-row').data('coded-cv') || '').toString();
            var resumeLink = ($btn.data('resume-link') || $btn.closest('.dm-v4-iv-row').data('resume-link') || '').toString();
            var cvLabel = ($btn.data('cv-label') || $btn.text().replace(/\s*↗\s*$/, '').trim() || 'CV').toString();
            dmV4OpenInterviewerResumeSummary(codedCv, resumeLink, cvLabel);
        });

        $(document).off('click.dmV4IvCvUpload', '.dm-v4-iv-cv-btn--upload').on('click.dmV4IvCvUpload', '.dm-v4-iv-cv-btn--upload', function (e) {
            e.preventDefault();
            if (isClientShareView()) return;
            var $row = $(this).closest('.dm-v4-iv-row');
            window.dmV4PendingUploadRow = $row;
            var userId = parseInt($row.data('user-id'), 10);
            var email = ($row.data('email') || '').toString();
            if (typeof OpenUploadResumeModal === 'function') {
                OpenUploadResumeModal(userId, email);
            }
        });

        $(document).off('click.dmV4IvCvSummary', '.dm-v4-iv-cv-btn--summary').on('click.dmV4IvCvSummary', '.dm-v4-iv-cv-btn--summary', function (e) {
            e.preventDefault();
            if (isClientShareView()) return;
            var $btn = $(this);
            if ($btn.prop('disabled')) return;
            var $row = $btn.closest('.dm-v4-iv-row');
            var cfg = getDmIvCvConfig();
            var resumeLink = ($btn.data('resume-link') || $row.data('resume-link') || '').toString();
            if (!resumeLink) return;
            var userId = parseInt($row.data('user-id'), 10);
            var orig = $btn.html();
            $btn.prop('disabled', true).html('<i class="fa fa-spinner fa-spin" aria-hidden="true"></i> Generating…');
            var ivName = $row.find('.dm-v4-iv-code').first().clone().children().remove().end().text().trim();
            window.dmV4ResumeSummarySession = { $row: $row, pendingPayload: null };
            dmV4SetResumeSummaryDrawerTitle(ivName);
            dmV4ShowResumeSummaryLoading('Generating resume summary…');
            $.ajax({
                url: cfg.generateSummaryUrl || '/IRF/GenerateInterviewerResumeSummaryAndSave',
                type: 'POST',
                data: { userId: userId, resumeLink: resumeLink, irfId: parseInt($row.data('irf-id'), 10) || 0 },
                success: function (data, _textStatus, xhr) {
                    if (data && data.success) {
                        if (data.summaryHtml) {
                            $('#id_candidateResumeSummaryBody').html(data.summaryHtml);
                        } else if (typeof GetInterviewerResumeSummary === 'function') {
                            GetInterviewerResumeSummary(resumeLink);
                        }
                        var cvPayload = {
                            codedCvLink: (data.codedCvLink || '').toString(),
                            resumeLink: resumeLink,
                            cvCode: (data.cvCode || dmV4RowCvCode($row)).toString()
                        };
                        if (cvPayload.codedCvLink) {
                            dmV4ApplyCvSaveResult($row, cvPayload);
                            window.dmV4ResumeSummarySession = {
                                $row: $row,
                                pendingPayload: cvPayload
                            };
                        }
                        dmV4RefreshIvCvState($row);
                    } else {
                        window.dmV4ResumeSummarySession = { $row: null, pendingPayload: null };
                        dmV4ShowResumeSummaryError('generate-summary-response', xhr, data);
                    }
                },
                error: function (xhr, _textStatus, _errorThrown) {
                    window.dmV4ResumeSummarySession = { $row: null, pendingPayload: null };
                    dmV4ShowResumeSummaryError('generate-summary-request', xhr, xhr && xhr.responseJSON);
                },
                complete: function () {
                    $btn.prop('disabled', false).html(orig);
                }
            });
        });

        $(document).off('click.dmV4IvScreen', '.dm-v4-iv-screen-btn').on('click.dmV4IvScreen', '.dm-v4-iv-screen-btn', function (e) {
            e.preventDefault();
            e.stopPropagation();
            var $row = window._dmV4VerdictDrawerRow;
            if ($(this).is('#dmV4VerdictDrawerScreenBtn')) {
                if ($row && $row.length) dmV4RunScreening($row);
                return;
            }
            $row = $(this).closest('.dm-v4-iv-row');
            if ($row.length) dmV4RunScreening($row);
        });

        $(document).off('click.dmV4VerdictOpen', '.dm-v4-iv-verdict-open').on('click.dmV4VerdictOpen', '.dm-v4-iv-verdict-open', function (e) {
            e.preventDefault();
            var url = $(this).data('screen-url');
            if (e.shiftKey || e.ctrlKey || e.metaKey) {
                if (url) window.open(url, '_blank');
                return;
            }
            openVerdictDrawer($(this).closest('.dm-v4-iv-row'));
        });

        $('#dmV4VerdictDrawerClose, #dmV4VerdictDrawerHeadClose').off('click.dmV4Verdict').on('click.dmV4Verdict', closeVerdictDrawer);
        $('#dmV4VerdictOverlay').off('click.dmV4Verdict').on('click.dmV4Verdict', closeVerdictDrawer);

        $('#dmV4VerdictDrawerKycBtn').off('click.dmV4VerdictKyc').on('click.dmV4VerdictKyc', function (e) {
            e.preventDefault();
            var videoUrl = ($(this).attr('data-video-url') || $(this).data('videoUrl') || '').toString().trim();
            if (!videoUrl && window._dmV4VerdictDrawerRow && window._dmV4VerdictDrawerRow.length) {
                videoUrl = dmV4GetRowVideoKycUrl(window._dmV4VerdictDrawerRow);
            }
            if (videoUrl) {
                openDmV4VideoKyc(videoUrl);
            }
        });

        $(document).off('click.dmV4Vet', '.dm-v4-vet-btn:not(:disabled)').on('click.dmV4Vet', '.dm-v4-vet-btn:not(:disabled)', function () {
            var $btn = $(this);
            if ($btn.hasClass('is-on')) return;
            var vetType = $btn.closest('.dm-v4-vet-toggle').data('vet');
            if (vetType === 'rb' && !getIvCanEdit()) {
                if (typeof swal !== 'undefined') swal.fire('Cannot update RB Vet', 'You don\'t have permission to vet this interviewer.', 'error');
                return;
            }
            if (vetType === 'client' && !getIvCanEdit() && !getIvCanEditClientVet()) {
                if (typeof swal !== 'undefined') swal.fire('Cannot update Client Vet', 'You don\'t have permission to vet this interviewer.', 'error');
                return;
            }
            var $row = $btn.closest('.dm-v4-iv-row');
            var isYes = $btn.attr('data-vet-value') === '1';
            if (vetType === 'rb') {
                $.post('/IRF/UpdateInterviewerRbVet', {
                    transInterviewerId: parseInt($row.data('trans-id'), 10),
                    isVerified: isYes,
                    irfId: parseInt($row.data('irf-id'), 10),
                    userId: parseInt($row.data('user-id'), 10),
                    interviewerEmail: $row.data('email')
                }, function (res) {
                    if (res && res.success) {
                        applyVetCascadeRowAttrs($row, {
                            rbVet: isYes,
                            clientVet: isYes,
                            clientAction: isYes ? 'Shortlisted' : 'Rejected',
                            active: typeof res.active !== 'undefined' ? !!res.active : undefined,
                            available: typeof res.available !== 'undefined' ? !!res.available : undefined
                        });
                        syncVetButtons($row);
                        refreshIvBadges();
                    } else {
                        if (typeof swal !== 'undefined') swal.fire('RB Vet update failed', dmV4VetErrorText(res, 'Could not update RB Vet.'), 'error');
                    }
                }).fail(function (xhr) {
                    var res = xhr && xhr.responseJSON;
                    if (typeof swal !== 'undefined') swal.fire('RB Vet update failed', dmV4VetErrorText(res, 'Network or server error.'), 'error');
                });
            } else if (vetType === 'client') {
                var clientVetPayload = {
                    irfId: parseInt($row.data('irf-id'), 10),
                    interviewerEmail: ($row.attr('data-email') || $row.data('email') || '').toString(),
                    isApproved: isYes,
                    userId: parseInt($row.data('user-id'), 10)
                };
                if (isClientShareView()) {
                    clientVetPayload.Type = getClientShareTypeParam();
                }
                $.post('/IRF/UpdateInterviewerClientVet', clientVetPayload, function (res) {
                    if (res && res.success) {
                        applyVetCascadeRowAttrs($row, {
                            clientVet: isYes,
                            clientAction: isYes ? 'Shortlisted' : 'Rejected',
                            active: typeof res.active !== 'undefined' ? !!res.active : undefined,
                            available: typeof res.available !== 'undefined' ? !!res.available : undefined
                        });
                        syncVetButtons($row);
                        refreshIvBadges();
                    } else {
                        if (typeof swal !== 'undefined') swal.fire('Cannot update Client Vet', dmV4VetErrorText(res, 'Could not update Client Vet.'), 'error');
                    }
                }).fail(function (xhr) {
                    var res = xhr && xhr.responseJSON;
                    if (typeof swal !== 'undefined') swal.fire('Cannot update Client Vet', dmV4VetErrorText(res, 'Network or server error.'), 'error');
                });
            }
        });

        $('.dm-v4-iv-row').each(function () {
            updateSurchargeExtra($(this));
        });

        $(document).off('click.dmV4PolicySeg', '#dmV4IvPolicyBar .dm-v4-segment-btn:not(:disabled)').on('click.dmV4PolicySeg', '#dmV4IvPolicyBar .dm-v4-segment-btn:not(:disabled)', function () {
            var $btn = $(this);
            if ($btn.hasClass('is-on')) return;
            var $seg = $btn.closest('.dm-v4-segment');
            var $bar = getPolicyBar();
            $seg.find('.dm-v4-segment-btn').removeClass('is-on');
            $btn.addClass('is-on');
            if ($seg.data('segment') === 'lock') {
                setPolicyLockOn($bar, $btn.data('value') === 'on');
            } else if ($seg.data('segment') === 'assign') {
                setAssignmentPolicy($bar, $btn.attr('data-value') || $btn.data('value'));
            }
            updatePolicySubtext();
            saveDmIvPolicy();
        });

        $(document).off('click.dmV4CapStep', '#dmV4DefaultCapStepper .dm-v4-stepper-btn:not(:disabled)').on('click.dmV4CapStep', '#dmV4DefaultCapStepper .dm-v4-stepper-btn:not(:disabled)', function () {
            var step = parseInt($(this).data('step'), 10) || 0;
            var val = getDefaultCap();
            val = Math.max(1, Math.min(99, val + step));
            $('#dmV4DefaultCapVal').text(val);
            getPolicyBar().data('default-cap', val);
            saveDmIvPolicy();
        });

        $('#dmV4ApplyCapAllBtn').off('click.dmV4Bulk').on('click.dmV4Bulk', function (e) {
            e.preventDefault();
            openBulkCapModal();
        });
        $('#dmV4BulkCapCancel').off('click.dmV4Bulk').on('click.dmV4Bulk', closeBulkCapModal);
        $('#dmV4BulkCapModal').off('click.dmV4Bulk').on('click.dmV4Bulk', function (e) {
            if (e.target === this) closeBulkCapModal();
        });
        $('#dmV4BulkCapModal .dm-v4-bulk-cap-modal__card').off('click.dmV4BulkStop').on('click.dmV4BulkStop', function (e) {
            e.stopPropagation();
        });
        $(document).off('keydown.dmV4Bulk').on('keydown.dmV4Bulk', function (e) {
            if (e.key === 'Escape' && $('#dmV4BulkCapModal').hasClass('is-open')) {
                closeBulkCapModal();
            }
        });
        $('#dmV4BulkCapConfirm').off('click.dmV4Bulk').on('click.dmV4Bulk', function () {
            var $btn = $(this);
            if ($btn.prop('disabled')) return;
            var $bar = getPolicyBar();
            var cap = getDefaultCap();
            var irfId = parseInt($bar.data('irf-id'), 10) || parseInt($('#id').val(), 10);
            if (!irfId) {
                showBulkCapError('Missing skill channel id. Refresh the page and try again.');
                return;
            }
            var orig = $btn.text();
            clearBulkCapError();
            $btn.prop('disabled', true).text('Saving…');
            $.post('/IRF/ApplyDefaultInterviewCap', {
                irfId: irfId,
                defaultCap: cap
            }).done(function (res) {
                if (res && res.success) {
                    $('.dm-v4-iv-row').each(function () {
                        var $row = $(this);
                        var assigned = parseInt($row.data('assigned'), 10) || 0;
                        $row.attr('data-capacity', res.defaultCap).attr('data-cap-configured', '1').attr('data-cap-from-default', '0');
                        $row.attr('data-cap-effective', res.defaultCap);
                        renderCapacityCell($row.find('.dm-v4-iv-capacity'), res.defaultCap, assigned, true, false);
                    });
                    closeBulkCapModal();
                } else {
                    showBulkCapError((res && res.message) || 'Could not apply cap. Please try again.');
                }
            }).fail(function (xhr) {
                var msg = 'Request failed. Please try again.';
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    msg = xhr.responseJSON.message;
                }
                showBulkCapError(msg);
            }).always(function () {
                $btn.prop('disabled', false).text(orig);
            });
        });

        $(document).off('click.dmV4CapSet', '#dmV4IvPanel .dm-v4-cap-set-btn').on('click.dmV4CapSet', '#dmV4IvPanel .dm-v4-cap-set-btn', function (e) {
            e.preventDefault();
            e.stopPropagation();
            var $row = $(this).closest('.dm-v4-iv-row');
            var startVal = getDefaultCap();
            startCapInlineEdit($row, startVal);
        });

        $(document).off('click.dmV4CapPill', '#dmV4IvPanel .dm-v4-cap-pill').on('click.dmV4CapPill', '#dmV4IvPanel .dm-v4-cap-pill', function (e) {
            e.preventDefault();
            e.stopPropagation();
            var orig = parseInt($(this).attr('data-cap'), 10);
            if (isNaN(orig)) orig = 0;
            startCapInlineEdit($(this).closest('.dm-v4-iv-row'), orig);
        });

        /* Surcharge — inline edit on pill click (Per-IV rate is read-only on DM Approval) */
        $(document).off('click.dmV4SurchargePill', '#dmV4IvPanel .dm-v4-surcharge-pill').on('click.dmV4SurchargePill', '#dmV4IvPanel .dm-v4-surcharge-pill', function (e) {
            e.preventDefault();
            e.stopPropagation();
            if (!getIvCanEditSurcharge()) return;
            startMoneyPillEdit($(this));
        });

        $(document).off('click.dmV4KycPlay', '.dm-v4-iv-kyc--btn').on('click.dmV4KycPlay', '.dm-v4-iv-kyc--btn', function (e) {
            e.preventDefault();
            e.stopPropagation();
            var $btn = $(this);
            var $row = $btn.closest('.dm-v4-iv-row');
            var url = ($btn.attr('data-video-url') || $btn.data('videoUrl') || dmV4GetRowVideoKycUrl($row));
            if (url) {
                openDmV4VideoKyc(url);
            }
        });

        $(document).off('click.dmV4KycRequest', '.dm-v4-iv-kyc-request').on('click.dmV4KycRequest', '.dm-v4-iv-kyc-request', function (e) {
            e.preventDefault();
            e.stopPropagation();
            var $btn = $(this);
            var email = $btn.data('email');
            if (!email) return;
            var originalHtml = $btn.html();
            $btn.prop('disabled', true).text('Sending…');
            $.post('/IRF/RequestVideoKYC', {
                interviewerEmail: email,
                interviewerName: email.split('@')[0]
            }, function (res) {
                if (res && res.success) {
                    if (typeof swal !== 'undefined') swal.fire('Video KYC request sent to ' + email, '', 'success');
                } else {
                    if (typeof swal !== 'undefined') swal.fire('Failed to send Video KYC request.', '', 'error');
                }
            }).fail(function () {
                if (typeof swal !== 'undefined') swal.fire('Failed to send Video KYC request.', 'Network or server error.', 'error');
            }).always(function () {
                $btn.prop('disabled', false).html(originalHtml);
            });
        });

        $('#dmV4VideoKycClose, #dmV4VideoKycOverlay').off('click.dmV4KycModal').on('click.dmV4KycModal', closeDmV4VideoKyc);
        $('#dmV4VideoKycModal .dm-v4-video-kyc-modal__card').off('click.dmV4KycModalStop').on('click.dmV4KycModalStop', function (e) {
            e.stopPropagation();
        });
        $('#dmV4VideoKycModal').off('click.dmV4KycModal').on('click.dmV4KycModal', function (e) {
            if (e.target === this) closeDmV4VideoKyc();
        });
    }



    window.dmV4RefreshRubric = function () {

        reindexRubricRows();

        $('#SkillTable tbody tr').addClass('dm-v4-rubric-row');

        $('#SkillTable tbody tr.dm-v4-rubric-row').each(function () {

            syncElimWrap($(this));

        });

        updateRubricTotals();

        enhanceRubricLevelSelects();

        enhanceRubricEditableUI();

    };

    /**
     * Applies voice-revised rubric skills to the score rubric form without saving.
     * User must click Save score rubric to persist.
     */
    window.dmV4ApplyVoiceRevisedRubric = function (skills) {
        var $tbody = $('#SkillTable tbody');
        if (!$tbody.length) return false;

        var $templateRow = $tbody.find('tr.dm-v4-rubric-row').first();
        if (!$templateRow.length) return false;

        var templateHtml = $templateRow.prop('outerHTML');
        var rows = Array.isArray(skills) ? skills : [];
        $tbody.empty();

        if (!rows.length) {
            rows = [{
                skillName: '',
                levelId: 0,
                weightage: 100,
                timelimit: 10,
                isEliminationskill: false,
                concepts: '',
                natureofquestion: ''
            }];
        }

        rows.forEach(function (skill) {
            var $row = $(templateHtml);
            var levelId = skill.levelId || skill.LevelId || '';
            var skillName = skill.skillName || skill.SkillName || '';
            var weightage = skill.weightage != null ? skill.weightage : (skill.Weightage != null ? skill.Weightage : 10);
            var timelimit = skill.timelimit != null ? skill.timelimit : (skill.Timelimit != null ? skill.Timelimit : 10);
            var concepts = skill.concepts != null ? skill.concepts : (skill.Concepts || '');
            var expectations = skill.natureofquestion != null ? skill.natureofquestion : (skill.Natureofquestion || '');
            var elim = !!(skill.isEliminationskill || skill.IsEliminationskill);

            $row.find('.dm-v4-level-select').val(levelId ? String(levelId) : '');
            $row.find('[id$="__SkillName"]').val(skillName);
            $row.find('[id$="__Weightage"]').val(weightage);
            $row.find('[id$="__Timelimit"]').val(timelimit);
            $row.find('[id$="__Concepts"]').val(concepts);
            $row.find('[id$="__Natureofquestion"]').val(expectations);
            $row.find('.dm-v4-elim-input').prop('checked', elim);

            $tbody.append($row);
        });

        window.dmV4RefreshRubric();

        var skillNames = rows
            .map(function (s) { return (s.skillName || s.SkillName || '').trim(); })
            .filter(function (n) { return n.length > 0; });
        var $skillsJson = $('#dmV4RubricSkillsJson');
        if ($skillsJson.length) {
            $skillsJson.text(JSON.stringify(skillNames));
        }

        var $pill = $('#dmV4WeightPill');
        if ($pill.length) {
            $pill.addClass('dm-v4-weight-pill--pending');
        }

        $('#SkillCollection_WeightageError, #SkillCollection_WeightageErrorDown').text('');

        return true;
    };



    function formatVoiceTime(seconds) {
        if (!isFinite(seconds) || seconds < 0) return '0:00';
        var m = Math.floor(seconds / 60);
        var s = Math.floor(seconds % 60);
        return m + ':' + (s < 10 ? '0' : '') + s;
    }

    function initDmV4VoiceHero() {
        var audio = document.getElementById('dmV4VoiceAudio');
        var $playBtn = $('#dmV4VoicePlayBtn');
        var $time = $('#dmV4VoiceTime');
        var $wave = $('#dmV4VoiceWaveform');
        if (!audio || !$playBtn.length || !audio.getAttribute('src')) return;

        var $bars = $wave.find('.dm-v4-voice-waveform__bar');
        var $audio = $(audio);

        function syncWaveProgress() {
            if (!audio.duration) return;
            var ratio = audio.currentTime / audio.duration;
            var playedCount = Math.floor(ratio * $bars.length);
            $bars.each(function (i) {
                $(this).toggleClass('is-played', i < playedCount);
            });
        }

        function updateTimeLabel() {
            var cur = formatVoiceTime(audio.currentTime);
            var dur = audio.duration ? formatVoiceTime(audio.duration) : '—';
            $time.text(cur + ' / ' + dur);
            var $badge = $('#dmV4VoiceRecordedBadge');
            if ($badge.length && audio.duration) {
                $badge.text('RECORDED · ' + dur);
            }
        }

        $audio.off('.dmV4VoiceHero');
        $playBtn.off('click.dmV4VoiceHero');

        $audio.on('loadedmetadata.dmV4VoiceHero', function () {
            updateTimeLabel();
            syncWaveProgress();
        });
        $audio.on('timeupdate.dmV4VoiceHero', function () {
            updateTimeLabel();
            syncWaveProgress();
        });
        $audio.on('ended.dmV4VoiceHero', function () {
            $playBtn.removeClass('is-playing');
            $bars.removeClass('is-played');
            updateTimeLabel();
        });

        $playBtn.on('click.dmV4VoiceHero', function () {
            if (audio.paused) {
                audio.play();
                $playBtn.addClass('is-playing');
            } else {
                audio.pause();
                $playBtn.removeClass('is-playing');
            }
        });

        updateTimeLabel();
    }

    window.dmV4InitVoiceHero = initDmV4VoiceHero;

    window.applyDmV4Layout = function applyDmV4Layout() {

        moveLevelFormat();

        buildAdvancedNested();

        pinSection('hidden_fields_two');

        pinSection('hidden_fields_eight');

        $('.dm-v4-deprecated').remove();

        initRubricV4();

        initInterviewersV4();

        initDmV4Collapses();

        initAdvancedV4();

        initDmV4VoiceHero();

        if (typeof window.dmV4InitQuestions === 'function') {
            window.dmV4InitQuestions();
        }

    };

    window.dmV4RefreshEtiquette = loadEtiquetteFromServer;
    window.dmV4InitIvEditable = initIvEditableFields;

})(jQuery);


