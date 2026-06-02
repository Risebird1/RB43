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
        $(document).off('change.dmV4FormatCb', '.dm-v4-lf-format-cb').on('change.dmV4FormatCb', '.dm-v4-lf-format-cb', function () {
            syncLevelFormatCheckboxes();
        });
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

        var $etiquetteCard = $(
            '<div class="dm-v4-advanced-card dm-v4-advanced-card--etiquette">' +
            '<div class="dm-v4-advanced-card__head"><span class="dm-v4-advanced-card__title">Interviewer etiquette</span>' +
            '<span class="dm-v4-badge dm-v4-badge--green dm-v4-badge--xs">DEFAULTS ON</span></div>' +
            '<div class="dm-v4-advanced-card__body">' +
            '<div class="dm-v4-etiquette-label">Dress code</div>' +
            '<label class="dm-v4-etiquette-option is-on" data-etiquette="dress"><span class="dm-v4-etiquette-radio"></span><span><strong>Strict formal</strong><span class="dm-v4-etiquette-sub">Business attire · neutral background</span></span></label>' +
            '<label class="dm-v4-etiquette-option" data-etiquette="dress"><span class="dm-v4-etiquette-radio"></span><span><strong>Interviewer\'s choice — match candidate</strong><span class="dm-v4-etiquette-sub">Mirror role\'s expected level</span></span></label>' +
            '<ul class="dm-v4-etiquette-list"></ul></div></div>'
        );
        var $etiquetteList = $etiquetteCard.find('.dm-v4-etiquette-list');
        $etiquetteList.append(
            $('<li data-etiquette="rule"></li>').append('<span>Camera-on with neutral background required</span>').append(buildDmToggleButton(true))
        );
        $etiquetteList.append(
            $('<li data-etiquette="rule"></li>').append('<span>No eating, walking or smoking during interview</span>').append(buildDmToggleButton(true))
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
            $card.find('.dm-v4-etiquette-option[data-etiquette="dress"]').removeClass('is-on');
            $(this).addClass('is-on');
        });
        $(document).off('click.dmV4EtiquetteRule', '.dm-v4-etiquette-list .dm-v4-dm-toggle--btn').on('click.dmV4EtiquetteRule', '.dm-v4-etiquette-list .dm-v4-dm-toggle--btn', function () {
            var on = !$(this).hasClass('is-on');
            $(this).toggleClass('is-on', on).attr('aria-checked', on ? 'true' : 'false');
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

            $pill.toggleClass('dm-v4-weight-pill--ok', weight === 100);

            $pill.toggleClass('dm-v4-weight-pill--warn', weight !== 100);

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



        $('#dmV4DrawerClose, #dmV4RubricOverlay').off('click.dmV4Drawer').on('click.dmV4Drawer', function () {

            closeDrawer(false);

        });

        $('#dmV4DrawerDone').off('click.dmV4Drawer').on('click.dmV4Drawer', function () {

            closeDrawer(true);

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

    function readRbVetFromRow($row) {
        var raw = $row.attr('data-rb-vet');
        if ($row.attr('data-is-active') !== '1') return false;
        if (raw === '1') return true;
        if (raw === '0' && isFreeChannelPanel()
            && ($row.attr('data-is-available') === '1' || $row.attr('data-is-available') == null)
            && ($row.attr('data-client-action') || '').toLowerCase() !== 'rejected') {
            return true;
        }
        return false;
    }

    function readClientVetFromRow($row) {
        if ($row.attr('data-is-active') !== '1') return false;
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
    }

    function ivStatusFromRow($row) {
        var rbOn = readRbVetFromRow($row);
        var clientOn = readClientVetFromRow($row);
        var clientAction = ($row.attr('data-client-action') || '').toLowerCase();
        var active = rbOn && clientOn;
        if (active) return { label: 'Active', tone: 'active' };
        if (!rbOn && clientAction !== 'rejected') return { label: 'Available', tone: 'available' };
        return { label: 'Inactive', tone: 'inactive' };
    }

    function updateIvRowStatus($row) {
        if ($('#dmV4IvPanel').attr('data-lock') !== '1') return;
        var status = ivStatusFromRow($row);
        $row.attr('data-is-active', status.tone === 'active' ? '1' : '0');
        var $badge = $row.find('[data-role="iv-status"]');
        if (!$badge.length) return;
        $badge.removeClass('dm-v4-iv-status--active dm-v4-iv-status--available dm-v4-iv-status--inactive')
            .addClass('dm-v4-iv-status--' + status.tone)
            .text(status.label);
    }

    function normalizeVetRowAttrs($row) {
        var rbOn = readRbVetFromRow($row);
        var clientOn = readClientVetFromRow($row);
        $row.attr('data-rb-vet', rbOn ? '1' : '0');
        $row.attr('data-client-vet', clientOn ? '1' : '0');
    }

    function syncVetButtons($row) {
        var rbOn = readRbVetFromRow($row);
        var clientOn = readClientVetFromRow($row);
        $row.find('[data-vet="rb"] .dm-v4-vet-btn').removeClass('is-on');
        $row.find('[data-vet="rb"] .dm-v4-vet-btn[data-vet-value="' + (rbOn ? '1' : '0') + '"]').addClass('is-on');
        $row.find('[data-vet="client"] .dm-v4-vet-btn').removeClass('is-on');
        $row.find('[data-vet="client"] .dm-v4-vet-btn[data-vet-value="' + (clientOn ? '1' : '0') + '"]').addClass('is-on');
        var $clientBtns = $row.find('[data-vet="client"] .dm-v4-vet-btn');
        var $clientToggle = $row.find('[data-vet="client"]');
        if (!rbOn) {
            $clientBtns.prop('disabled', true);
            $clientToggle.attr('title', 'Needs RB Vet first.');
            $row.find('.dm-v4-iv-vet-sub--client').text('Blocked — needs RB Vet first');
        } else {
            $clientBtns.prop('disabled', false);
            $clientToggle.attr('title', '');
            $row.find('.dm-v4-iv-vet-sub--client').text(clientOn ? 'Signed off by client' : 'Awaiting client sign-off');
        }
        updateIvRowStatus($row);
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
        var name = $row.find('.dm-v4-iv-code').first().text().replace(/\s+/g, ' ').replace(/›/g, '').trim();
        var tone = verdictToneFromRow($row);
        var $full = $row.find('.dm-v4-iv-ai-reason-full');
        var reason = $full.length ? $full.text().trim() : $row.find('.dm-v4-iv-verdict-reason').text().trim();
        if (!reason) reason = 'AI screening not run for this skill channel yet.';

        var scoreText = $row.find('.dm-v4-iv-score').first().text().trim();
        if (!scoreText || scoreText === '—') scoreText = String($row.data('ai-score') || '0');
        if (scoreText === '—') scoreText = '0';
        var screenScore = parseInt(scoreText, 10) || 0;

        $('#dmV4VerdictDrawerTitle').text(name || 'Interviewer');
        $('#dmV4VerdictDrawerEyebrow').text('AI verdict · ' + (tone === 'pass' ? 'PASS' : tone === 'fail' ? 'BLOCK' : tone === 'warn' ? 'REVIEW' : 'PENDING'));
        $('.dm-v4-verdict-drawer-body').attr('class', 'dm-v4-drawer__body dm-v4-verdict-drawer-body dm-v4-verdict-drawer-body--' + tone);

        var $hero = $('#dmV4VerdictDrawerHero');
        $hero.attr('class', 'dm-v4-vd-hero dm-v4-vd-hero--' + tone);
        $('#dmV4VerdictDrawerHeroTitle').text(verdictHeroTitle(tone));
        $('#dmV4VerdictDrawerReason').text(reason);

        var $badge = $row.find('.dm-v4-verdict').first().clone().removeAttr('id');
        $('#dmV4VerdictDrawerBadge').replaceWith($badge.attr('id', 'dmV4VerdictDrawerBadge'));

        $('#dmV4VerdictDrawerOverall').text(screenScore > 0 ? screenScore : '—');

        var bullets = parseVerdictBullets(reason, tone);
        var flagsHtml = '';
        bullets.forEach(function (b) {
            flagsHtml += '<li class="dm-v4-vd-flag-item">' +
                '<span class="dm-v4-vd-flag-item__icon" aria-hidden="true"><i class="fa ' +
                (tone === 'pass' ? 'fa-check' : tone === 'fail' ? 'fa-times' : 'fa-exclamation') +
                '"></i></span><span>' + $('<span/>').text(b).html() + '</span></li>';
        });
        $('#dmV4VerdictDrawerFlags').html(flagsHtml);

        dmV4UpdateScreenedAtCaption($row);
        dmV4SyncDrawerScreenBtnVisibility($row);

        $('#dmV4VerdictOverlay, #dmV4VerdictDrawer').addClass('is-open').attr('aria-hidden', 'false');
    }

    function closeVerdictDrawer() {
        $('#dmV4VerdictOverlay, #dmV4VerdictDrawer').removeClass('is-open').attr('aria-hidden', 'true');
    }

    var vdVerdictIconPass = '<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.8" width="11" height="11"><path d="M3.5 8.5l3 3L13 5" stroke-linecap="round" stroke-linejoin="round"/></svg>';
    var vdVerdictIconWarn = '<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" width="11" height="11"><path d="M8 2l6.5 11.5h-13L8 2z" stroke-linejoin="round"/><path d="M8 6.5v3M8 11.5v.5" stroke-linecap="round"/></svg>';
    var vdVerdictIconFail = '<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" width="11" height="11"><path d="M4 4l8 8M12 4l-8 8" stroke-linecap="round"/></svg>';
    var vdVerdictIconMuted = '<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.4" width="11" height="11"><circle cx="8" cy="8" r="6"/><path d="M8 7v4M8 5v.5" stroke-linecap="round"/></svg>';

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

    function refreshDmV4IvPanelMetrics() {
        var cfg = getDmIvCvConfig();
        var irfId = parseInt($('#id').val(), 10) || parseInt($('#dmV4IvPanel').closest('form').find('[name="id"]').val(), 10);
        if (!cfg.panelMetricsUrl || !irfId) {
            syncDmV4IvFutureAssignedAll();
            return $.Deferred().resolve().promise();
        }
        return $.getJSON(cfg.panelMetricsUrl, { irfId: irfId }).done(function (res) {
            if (!res || !res.ok || !res.interviewers) {
                syncDmV4IvFutureAssignedAll();
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
                }
            });
        }).fail(function () {
            syncDmV4IvFutureAssignedAll();
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

    function dmV4RenderCvStack($row, state) {
        var canEdit = getIvCanEdit();
        var $stack = $row.find('.dm-v4-iv-cv-stack');
        if (!$stack.length) return;

        var resumeLink = (state.resumeLink || '').toString();
        var codedCvLink = (state.codedCvLink || '').toString();
        var cvCode = (state.cvCode || 'CV').toString();
        var redactAttempted = !!state.isResumeRedactAttempted;
        var hasResume = !!resumeLink;
        var hasCoded = !!codedCvLink;
        var canScreen = hasResume || hasCoded;
        var hasScreening = (parseInt(state.aiScore, 10) || 0) > 0 ||
            !!(state.aiVerdict) || !!(state.aiReasonFull);

        $row.attr('data-resume-link', resumeLink);
        $row.attr('data-coded-cv', codedCvLink);

        var html = '';
        if (hasCoded) {
            html += '<a href="' + codedCvLink + '" target="_blank" rel="noopener" class="dm-v4-iv-cv-link">' +
                $('<span/>').text(cvCode).html() + ' ↗</a>';
        } else if (canEdit && hasResume && !redactAttempted) {
            html += '<button type="button" class="dm-v4-iv-cv-btn dm-v4-iv-cv-btn--convert" data-resume-link="' +
                $('<span/>').text(resumeLink).html() + '" title="Resume on file — convert to coded CV">' +
                '<i class="fa fa-magic" aria-hidden="true"></i> Convert to Coded CV</button>';
        } else if (!canEdit && !hasCoded) {
            html += '<span class="dm-v4-muted">No coded CV</span>';
        } else if (redactAttempted && !hasCoded) {
            html += '<span class="dm-v4-muted">—</span>';
        }
        if (canEdit && !hasResume) {
            html += '<button type="button" class="dm-v4-iv-cv-btn dm-v4-iv-cv-btn--upload" title="Upload interviewer resume (PDF)">' +
                '<i class="fa fa-upload" aria-hidden="true"></i> Upload Resume</button>';
        }
        $stack.html(html);
        dmV4EnsureVerdictScreenButton($row, {
            canScreen: canScreen,
            hasScreening: hasScreening
        });
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

    function dmV4RefreshIvCvState($row) {
        var cfg = getDmIvCvConfig();
        if (!cfg.cvStateUrl) return $.Deferred().reject().promise();
        var irfId = parseInt($row.data('irf-id'), 10);
        var userId = parseInt($row.data('user-id'), 10);
        return $.getJSON(cfg.cvStateUrl, { irfId: irfId, userId: userId }).done(function (state) {
            if (state && state.ok) {
                dmV4RenderCvStack($row, state);
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
        });
    }

    function patchDmV4UploadResume() {
        if (window._dmV4UploadResumePatched || typeof window.uploadResume !== 'function') return;
        var originalUpload = window.uploadResume;
        window.uploadResume = function () {
            var pendingRow = window.dmV4PendingUploadRow;
            var formData = new FormData();
            var filesInput = document.getElementsByName('ResumeFiles')[0];
            if (!filesInput || !filesInput.files.length) {
                if (typeof swal !== 'undefined') swal.fire('Please select a PDF resume to upload.');
                return;
            }
            for (var i = 0; i < filesInput.files.length; i++) {
                formData.append('ResumeFiles', filesInput.files[i]);
            }
            formData.append('UserId', typeof interviewerUserId !== 'undefined' ? interviewerUserId : '');
            formData.append('InterviewerEmailId', typeof interviewerEmailId !== 'undefined' ? interviewerEmailId : '');
            $.ajax({
                type: 'POST',
                url: '/IRF/UploadInterviewerResume',
                data: formData,
                contentType: false,
                enctype: 'multipart/form-data',
                processData: false,
                success: function (result) {
                    if (typeof swal !== 'undefined') swal.fire(result);
                    if (typeof closeUploadResumeModal === 'function') closeUploadResumeModal();
                    if (pendingRow && pendingRow.length) {
                        dmV4RefreshIvCvState(pendingRow);
                        window.dmV4PendingUploadRow = null;
                    } else {
                        location.reload(true);
                    }
                },
                error: function () {
                    if (typeof swal !== 'undefined') swal.fire('Upload failed.', '', 'error');
                }
            });
        };
        window._dmV4UploadResumePatched = true;
    }

    function getPolicyBar() {
        return $('#dmV4IvPolicyBar');
    }

    function getIvCanEdit() {
        var $panel = $('#dmV4IvPanel');
        if ($panel.data('can-edit') === 1 || $panel.data('can-edit') === '1') return true;
        var $bar = getPolicyBar();
        return $bar.length && ($bar.data('can-edit') === 1 || $bar.data('can-edit') === '1');
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
            dmIvLockNew: $bar.data('lock') === '1',
            dmAssignmentPolicy: $bar.data('policy') || 'free',
            dmDefaultInterviewCap: getDefaultCap()
        }).fail(function () {
            if (typeof swal !== 'undefined') swal.fire('Failed to save policy settings.', '', 'error');
        });
    }

    function syncPolicyBarControls() {
        var $bar = getPolicyBar();
        if (!$bar.length) return;
        var lock = $bar.data('lock') === '1';
        var policy = $bar.data('policy') === 'limited' ? 'limited' : 'free';
        var cap = parseInt($bar.data('default-cap'), 10);
        if (isNaN(cap) || cap < 1) cap = getDefaultCap();
        $bar.find('[data-segment="lock"] .dm-v4-segment-btn').removeClass('is-on')
            .filter('[data-value="' + (lock ? 'on' : 'off') + '"]').addClass('is-on');
        $bar.find('[data-segment="assign"] .dm-v4-segment-btn').removeClass('is-on')
            .filter('[data-value="' + policy + '"]').addClass('is-on');
        $('#dmV4DefaultCapVal').text(cap);
    }

    function updatePolicySubtext() {
        var $bar = getPolicyBar();
        if (!$bar.length) return;
        var lock = $bar.data('lock') === '1';
        var limited = $bar.data('policy') === 'limited';
        $('#dmV4IvLockSub').text(lock
            ? 'New interviewers join as RB Vet pending. Client Vet auto-cascades to pending until Risebird clears them.'
            : 'New interviewers are auto-RB-vetted and auto-Client-approved. Override per row if needed.');
        $('#dmV4IvPolicySub').text(limited
            ? 'Enforce per-interviewer caps. Edit each interviewer’s limit inline in the table.'
            : 'Active interviewers can be assigned to any interview without further review.');
        $('#dmV4IvPanel').attr('data-cap-enforced', limited ? '1' : '0');
        syncPolicyBarControls();
        $('.dm-v4-iv-row').each(function () {
            var $row = $(this);
            var $wrap = $row.find('.dm-v4-iv-capacity');
            var assigned = parseInt($row.attr('data-assigned'), 10) || parseInt($wrap.attr('data-assigned'), 10) || 0;
            syncCapacityCellUi($wrap, readRowCapacity($row), assigned, isRowCapConfigured($row));
        });
    }

    function isCapEnforced() {
        return $('#dmV4IvPanel').attr('data-cap-enforced') === '1';
    }

    function isRowCapConfigured($row) {
        return $row.attr('data-cap-configured') === '1';
    }

    function readRowCapacity($row) {
        if (!isRowCapConfigured($row)) return null;
        var n = parseInt($row.attr('data-capacity'), 10);
        return isNaN(n) ? 0 : n;
    }

    function shouldShowUnlimitedCap($row) {
        if (!isCapEnforced()) return true;
        return !isRowCapConfigured($row);
    }

    function capBarClass(assigned, capacity) {
        if (capacity <= 0) return 'ok';
        var pct = Math.round((assigned / Math.max(1, capacity)) * 100);
        if (assigned >= capacity) return 'full';
        if (pct >= 80) return 'near';
        return 'ok';
    }

    /* Reference dm-shared.jsx I.edit / I.plus */
    var DM_V4_EDIT_ICON = '<span class="dm-v4-money-edit-icon" aria-hidden="true">' +
        '<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.4" width="9" height="9">' +
        '<path d="M3 13l.7-3 6.6-6.6a1 1 0 011.4 0l1 1a1 1 0 010 1.4L6.1 12.3 3 13z" stroke-linejoin="round"/></svg></span>';

    var DM_V4_PLUS_ICON = '<span class="dm-v4-cap-set-btn__icon" aria-hidden="true">' +
        '<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" width="10" height="10">' +
        '<path d="M8 3.5v9M3.5 8h9" stroke-linecap="round"/></svg></span>';

    function buildCapPillHtml(capacity) {
        return '<button type="button" class="dm-v4-cap-pill v4-cap-pill" data-cap="' + capacity + '" title="Click to set this interviewer\'s interview limit">' +
            '<span class="dm-v4-cap-pill-val">' + capacity + '</span>' + DM_V4_EDIT_ICON + '</button>';
    }

    function moneyDisplayValue(value, kind) {
        var v = Math.max(0, parseInt(value, 10) || 0);
        if (kind === 'rate' || kind === 'surcharge') {
            return v > 0 ? v.toLocaleString('en-IN') : '0';
        }
        return String(v);
    }

    function getSurchargeMax($row) {
        return Math.max(0, parseInt($row.data('rate'), 10) || 0);
    }

    function validateSurchargeAmount(surcharge, maxRate) {
        if (surcharge < 0) {
            return 'Surcharge cannot be negative.';
        }
        if (maxRate <= 0 && surcharge > 0) {
            return 'Set a per-interview rate before applying a surcharge.';
        }
        if (surcharge > maxRate) {
            return 'Surcharge cannot exceed 100% of the interview cost (max ₹' + maxRate.toLocaleString('en-IN') + ').';
        }
        return null;
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
            : 'Surcharge amount in INR (max = base interview rate)';
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
        $pill.data('dm-v4-editing', true);

        var kind = $pill.hasClass('dm-v4-rate-pill') ? 'rate' : 'surcharge';
        var $row = $pill.closest('.dm-v4-iv-row');
        var orig = parseInt($pill.attr('data-value'), 10);
        if (isNaN(orig)) orig = 0;
        var meta = {
            userId: $pill.data('userId') || $pill.data('user-id'),
            irfId: $pill.data('irfId') || $pill.data('irf-id')
        };

        var $input = $('<input type="number" class="dm-v4-inline-edit-input" min="0" />').val(orig);
        if (kind === 'surcharge') {
            var maxSurcharge = getSurchargeMax($row);
            if (maxSurcharge > 0) {
                $input.attr('max', maxSurcharge);
            }
        }

        $pill.replaceWith($input);
        $input.focus().select();

        var finished = false;
        var finish = function (doCommit) {
            if (finished) return;
            finished = true;
            var n = doCommit ? Math.max(0, parseInt($input.val(), 10) || 0) : orig;
            if (kind === 'surcharge' && doCommit) {
                var maxRate = getSurchargeMax($row);
                var validationError = validateSurchargeAmount(n, maxRate);
                if (validationError) {
                    if (typeof swal !== 'undefined') {
                        swal.fire('Invalid surcharge', validationError, 'error');
                    } else {
                        alert(validationError);
                    }
                    doCommit = false;
                    n = orig;
                }
            }
            var attrs = kind === 'surcharge' ? { userId: meta.userId, irfId: meta.irfId } : {};
            $input.replaceWith(buildMoneyPillHtml(n, kind, attrs));

            if (!doCommit) return;

            if (kind === 'rate') {
                $row.attr('data-rate', n);
                $row.find('input[name*="PerSuccessRate"]').val(n);
                saveRate($row, n);
            } else {
                $row.find('input[name*="Surcharge"]').val(n);
                saveSurcharge($row, n);
            }
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
            orig = parseInt($wrap.attr('data-capacity'), 10);
            if (isNaN(orig)) orig = parseInt($row.attr('data-capacity'), 10) || 0;
        }

        var $pill = $wrap.find('.dm-v4-cap-pill');
        var $setBtn = $wrap.find('.dm-v4-cap-set-btn');
        var $input = $('<input type="number" class="dm-v4-inline-edit-input dm-v4-inline-edit-input--cap dm-v4-mono" min="0" max="99" />').val(orig);
        var finished = false;

        var finish = function (doCommit) {
            if (finished) return;
            finished = true;
            $input.off('.dmV4CapEdit');
            var n = doCommit ? Math.max(0, Math.min(99, parseInt($input.val(), 10) || 0)) : orig;
            /* Reference V4CapacityCell: restore pill immediately, then persist */
            renderCapacityCell($wrap, n, assigned, true);
            if (doCommit) {
                saveCapacity($row, n);
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

    function syncCapacityCellUi($wrap, capacity, assigned, capConfigured) {
        var $row = $wrap.closest('.dm-v4-iv-row');
        if (typeof capConfigured === 'undefined') {
            capConfigured = capacity != null;
        }
        if (capConfigured) {
            $wrap.attr('data-capacity', capacity).attr('data-cap-configured', '1');
            $row.attr('data-capacity', capacity).attr('data-cap-configured', '1');
        } else {
            $wrap.attr('data-capacity', '').attr('data-cap-configured', '0');
            $row.attr('data-capacity', '').attr('data-cap-configured', '0');
        }
        $wrap.attr('data-assigned', assigned);
        $row.attr('data-assigned', assigned);

        if (!$wrap.find('.dm-v4-cap-display').length && !$wrap.find('.dm-v4-cap-set-btn').length && !$wrap.find('.dm-v4-cap-unlimited').length) {
            renderCapacityCell($wrap, capacity, assigned, capConfigured);
            return;
        }

        renderCapacityCell($wrap, capacity, assigned, capConfigured);
    }

    function renderCapacityCell($wrap, capacity, assigned, capConfigured) {
        var $row = $wrap.closest('.dm-v4-iv-row');
        var canEdit = getIvCanEdit();
        if (typeof capConfigured === 'undefined') {
            capConfigured = isRowCapConfigured($row);
            if (!capConfigured && capacity != null && capacity !== '') {
                capConfigured = true;
            }
        }
        if (capConfigured) {
            capacity = parseInt(capacity, 10);
            if (isNaN(capacity)) capacity = 0;
            $wrap.attr('data-capacity', capacity).attr('data-cap-configured', '1');
            $row.attr('data-capacity', capacity).attr('data-cap-configured', '1');
        } else {
            $wrap.attr('data-capacity', '').attr('data-cap-configured', '0');
            $row.attr('data-capacity', '').attr('data-cap-configured', '0');
        }
        $wrap.attr('data-assigned', assigned);
        $row.attr('data-assigned', assigned);

        if (shouldShowUnlimitedCap($row)) {
            $wrap.html('<span class="dm-v4-cap-unlimited" title="No interview limit configured">Unlimited</span>');
            $row.removeClass('dm-v4-iv-row--cap-full');
            return;
        }

        if (capConfigured && capacity === 0) {
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

        if (!capConfigured && canEdit) {
            $wrap.html('<button type="button" class="dm-v4-cap-set-btn" title="Set interview limit">' + DM_V4_PLUS_ICON + 'Set limit</button>');
            $row.removeClass('dm-v4-iv-row--cap-full');
            return;
        }

        var pct = capacity > 0 ? Math.min(100, Math.round((assigned / Math.max(1, capacity)) * 100)) : 0;
        var bar = capBarClass(assigned, capacity);
        var full = capacity > 0 && assigned >= capacity;
        var near = !full && pct >= 80;
        var html = '<div class="dm-v4-cap-display">';
        html += '<span class="dm-v4-mono dm-v4-iv-cap-assigned">' + assigned + '</span><span class="dm-v4-iv-cap-sep">/</span>';
        if (canEdit) {
            html += buildCapPillHtml(capacity);
        } else {
            html += '<span class="dm-v4-mono dm-v4-cap-readonly">' + capacity + '</span>';
        }
        html += '</div>';
        if (capacity > 0) {
            html += '<div class="dm-v4-cap-bar"><span class="dm-v4-cap-bar-fill dm-v4-cap-bar-fill--' + bar + '" style="width:' + pct + '%"></span></div>';
        }
        if (isCapEnforced() && full) {
            html += '<div class="dm-v4-cap-status dm-v4-cap-status--full">FULL · CAPPED</div>';
        } else if (isCapEnforced() && near) {
            html += '<div class="dm-v4-cap-status dm-v4-cap-status--near">NEAR LIMIT</div>';
        }
        $wrap.html(html);
        $row.toggleClass('dm-v4-iv-row--cap-full', full);
    }

    function saveCapacity($row, capacity) {
        $.post('/IRF/UpdateInterviewerCapacity', {
            transInterviewerId: parseInt($row.data('trans-id'), 10),
            interviewCapacity: capacity
        }, function (res) {
            if (res && res.success) {
                var assigned = parseInt($row.data('assigned'), 10) || 0;
                var cap = res.interviewCapacity;
                var configured = res.capConfigured !== false;
                renderCapacityCell($row.find('.dm-v4-iv-capacity'), cap, assigned, configured);
            }
        });
    }

    function saveRate($row, rate) {
        $.post('/IRF/UpdateInterviewerPerSuccessRate', {
            transInterviewerId: parseInt($row.data('trans-id'), 10),
            perSuccessRate: rate
        }, function (res) {
            if (res && res.success) {
                $row.attr('data-rate', res.perSuccessRate);
                var $pill = $row.find('.dm-v4-rate-pill');
                if ($pill.length) {
                    $pill.attr('data-value', res.perSuccessRate);
                    $pill.find('.dm-v4-money-val').text(moneyDisplayValue(res.perSuccessRate, 'rate'));
                }
                var $surchargePill = $row.find('.dm-v4-surcharge-pill');
                if ($surchargePill.length) {
                    $surchargePill.attr('title', 'Surcharge amount in INR (max ₹' + moneyDisplayValue(res.perSuccessRate, 'rate') + ')');
                }
                updateSurchargeExtra($row);
            }
        });
    }

    function saveSurcharge($row, surcharge) {
        var $pill = $row.find('.dm-v4-surcharge-pill');
        var userId = $pill.data('userId') || $pill.data('user-id') || $row.data('user-id');
        var irfId = $pill.data('irfId') || $pill.data('irf-id') || $row.data('irf-id');
        $.post('/IRF/SaveSurcharge', {
            userId: userId,
            surcharge: surcharge,
            irfId: irfId
        }, function (res) {
            var ok = res && (res.success === true || res.success === 'true');
            if (!ok) {
                var msg = (res && res.message) ? res.message : 'Failed to save surcharge.';
                if (typeof swal !== 'undefined') {
                    swal.fire('Surcharge not saved', msg, 'error');
                } else {
                    alert(msg);
                }
                return;
            }
            var saved = res.surcharge != null ? res.surcharge : surcharge;
            $pill = $row.find('.dm-v4-surcharge-pill');
            if ($pill.length) {
                $pill.attr('data-value', saved);
                $pill.find('.dm-v4-money-val').text(moneyDisplayValue(saved, 'surcharge'));
            }
            $row.find('input[name*="Surcharge"]').val(saved);
        });
    }

    function openBulkCapModal() {
        var cap = getDefaultCap();
        var n = $('.dm-v4-iv-row').length;
        $('#dmV4BulkCapTitle').text('Set every interviewer’s cap to ' + cap + '?');
        $('#dmV4BulkCapText').text(
            'This overrides individual interview limits across all ' + n + ' interviewer' + (n === 1 ? '' : 's') +
            '. Per-interviewer edits made afterwards will still stick.'
        );
        $('#dmV4BulkCapOverlay, #dmV4BulkCapModal').addClass('is-open').attr('aria-hidden', 'false');
    }

    function closeBulkCapModal() {
        $('#dmV4BulkCapOverlay, #dmV4BulkCapModal').removeClass('is-open').attr('aria-hidden', 'true');
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
            var $rateBtn = $row.find('.dm-v4-rate-pill');
            if ($rateBtn.length && !$rateBtn.find('.dm-v4-money-edit-icon').length) {
                var rv = parseInt($rateBtn.data('value'), 10) || 0;
                $rateBtn.replaceWith(buildMoneyPillHtml(rv, 'rate', {}));
            }
            var $capPill = $row.find('.dm-v4-cap-pill');
            if ($capPill.length && !$capPill.find('.dm-v4-money-edit-icon').length) {
                $capPill.append(DM_V4_EDIT_ICON);
            }
            var $capInput = $row.find('.dm-v4-cap-input');
            if ($capInput.length) {
                var $wrap = $capInput.closest('.dm-v4-iv-capacity');
                var cap = parseInt($capInput.val(), 10) || parseInt($wrap.data('capacity'), 10) || 0;
                var assigned = parseInt($wrap.data('assigned'), 10) || 0;
                renderCapacityCell($wrap, cap, assigned);
            }
        });
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
            var cap = parseInt($row.attr('data-capacity'), 10);
            if (isNaN(cap)) cap = parseInt($wrap.attr('data-capacity'), 10) || 0;
            var assigned = parseInt($row.attr('data-assigned'), 10);
            if (isNaN(assigned)) assigned = parseInt($wrap.attr('data-assigned'), 10) || 0;
            syncCapacityCellUi($wrap, cap, assigned);
            updateSurchargeExtra($row);
        });
    }

    function initInterviewersV4() {
        refreshIvBadges();
        updatePolicySubtext();
        initIvEditableFields();
        patchDmV4UploadResume();
        syncDmV4IvFutureAssignedAll();
        refreshDmV4IvPanelMetrics();
        $('.dm-v4-iv-row').each(function () {
            var $row = $(this);
            normalizeVetRowAttrs($row);
            syncVetButtons($row);
            dmV4SyncScreenButtonLabels($row);
        });

        $(document).off('click.dmV4IvCvUpload', '.dm-v4-iv-cv-btn--upload').on('click.dmV4IvCvUpload', '.dm-v4-iv-cv-btn--upload', function (e) {
            e.preventDefault();
            var $row = $(this).closest('.dm-v4-iv-row');
            window.dmV4PendingUploadRow = $row;
            var userId = parseInt($row.data('user-id'), 10);
            var email = ($row.data('email') || '').toString();
            if (typeof OpenUploadResumeModal === 'function') {
                OpenUploadResumeModal(userId, email);
            }
        });

        $(document).off('click.dmV4IvCvConvert', '.dm-v4-iv-cv-btn--convert').on('click.dmV4IvCvConvert', '.dm-v4-iv-cv-btn--convert', function (e) {
            e.preventDefault();
            var $btn = $(this);
            if ($btn.prop('disabled')) return;
            var $row = $btn.closest('.dm-v4-iv-row');
            var cfg = getDmIvCvConfig();
            var resumeLink = ($btn.data('resume-link') || $row.data('resume-link') || '').toString();
            if (!resumeLink) return;
            var orig = $btn.html();
            $btn.prop('disabled', true).html('<i class="fa fa-spinner fa-spin" aria-hidden="true"></i> Converting…');
            $.ajax({
                url: cfg.convertUrl || '/Interviewer/InterviewerResumeupdate',
                data: {
                    userid: parseInt($row.data('user-id'), 10),
                    irfid: parseInt($row.data('irf-id'), 10),
                    resumeLink: resumeLink
                },
                success: function (data) {
                    if (data && data !== '' && data !== 'failed') {
                        if (typeof swal !== 'undefined') {
                            swal.fire({ title: 'Coded CV ready', type: 'success', text: 'Resume converted for this interviewer.' });
                        }
                        dmV4RefreshIvCvState($row);
                    } else if (typeof swal !== 'undefined') {
                        swal.fire({ title: 'Conversion failed', type: 'error', text: 'Could not convert resume to coded CV.' });
                    }
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
            var videoUrl = $(this).data('video-url');
            var reviewUrl = $(this).data('review-url');
            if (videoUrl && typeof openDmV4VideoKyc === 'function') {
                openDmV4VideoKyc(videoUrl);
            } else if (reviewUrl) {
                window.open(reviewUrl, '_blank', 'noopener');
            }
        });

        $(document).off('click.dmV4Vet', '.dm-v4-vet-btn:not(:disabled)').on('click.dmV4Vet', '.dm-v4-vet-btn:not(:disabled)', function () {
            var $btn = $(this);
            if ($btn.hasClass('is-on')) return;
            var $row = $btn.closest('.dm-v4-iv-row');
            var isYes = $btn.attr('data-vet-value') === '1';
            var vetType = $btn.closest('.dm-v4-vet-toggle').data('vet');
            if (vetType === 'rb') {
                $.post('/IRF/UpdateInterviewerRbVet', {
                    transInterviewerId: parseInt($row.data('trans-id'), 10),
                    isVerified: isYes,
                    irfId: parseInt($row.data('irf-id'), 10),
                    userId: parseInt($row.data('user-id'), 10),
                    interviewerEmail: $row.data('email')
                }, function (res) {
                    if (res && res.success) {
                        $row.attr('data-rb-vet', isYes ? '1' : '0');
                        if (!isYes) {
                            $row.attr('data-client-vet', '0');
                            $row.attr('data-client-action', 'Rejected');
                        } else {
                            $row.attr('data-client-vet', '1');
                            $row.attr('data-client-action', 'Shortlisted');
                        }
                        if (typeof res.active !== 'undefined') {
                            $row.attr('data-is-active', res.active ? '1' : '0');
                        }
                        syncVetButtons($row);
                        refreshIvBadges();
                    } else {
                        if (typeof swal !== 'undefined') swal.fire('RB Vet update failed.', (res && res.error) || '', 'error');
                    }
                }).fail(function () {
                    if (typeof swal !== 'undefined') swal.fire('RB Vet update failed.', 'Network or server error.', 'error');
                });
            } else if (vetType === 'client') {
                $.post('/IRF/UpdateInterviewerClientVet', {
                    irfId: parseInt($row.data('irf-id'), 10),
                    interviewerEmail: $row.data('email'),
                    isApproved: isYes,
                    userId: parseInt($row.data('user-id'), 10)
                }, function (res) {
                    if (res && res.success) {
                        $row.attr('data-client-vet', isYes ? '1' : '0');
                        $row.attr('data-client-action', isYes ? 'Shortlisted' : 'Rejected');
                        if (typeof res.active !== 'undefined') {
                            $row.attr('data-is-active', res.active ? '1' : '0');
                        }
                        syncVetButtons($row);
                        refreshIvBadges();
                    } else {
                        if (typeof swal !== 'undefined') swal.fire('Client Vet update failed.', (res && res.error) || '', 'error');
                    }
                }).fail(function () {
                    if (typeof swal !== 'undefined') swal.fire('Client Vet update failed.', 'Network or server error.', 'error');
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
                $bar.data('lock', $btn.data('value') === 'on' ? '1' : '0');
            } else if ($seg.data('segment') === 'assign') {
                $bar.data('policy', $btn.data('value'));
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
        $('#dmV4BulkCapCancel, #dmV4BulkCapOverlay').off('click.dmV4Bulk').on('click.dmV4Bulk', closeBulkCapModal);
        $('#dmV4BulkCapModal').off('click.dmV4Bulk').on('click.dmV4Bulk', function (e) {
            if (e.target === this) closeBulkCapModal();
        });
        $('#dmV4BulkCapModal .dm-v4-bulk-cap-modal__card').off('click.dmV4BulkStop').on('click.dmV4BulkStop', function (e) {
            e.stopPropagation();
        });
        $('#dmV4BulkCapConfirm').off('click.dmV4Bulk').on('click.dmV4Bulk', function () {
            var $bar = getPolicyBar();
            var cap = getDefaultCap();
            $.post('/IRF/ApplyDefaultInterviewCap', {
                irfId: parseInt($bar.data('irf-id'), 10),
                defaultCap: cap
            }, function (res) {
                if (res && res.success) {
                    $('.dm-v4-iv-row').each(function () {
                        var assigned = parseInt($(this).data('assigned'), 10) || 0;
                        renderCapacityCell($(this).find('.dm-v4-iv-capacity'), res.defaultCap, assigned, true);
                    });
                    closeBulkCapModal();
                }
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

        /* Rate & surcharge — reference V4MoneyPill: whole pill click opens inline edit */
        $(document).off('click.dmV4MoneyPill', '#dmV4IvPanel .dm-v4-rate-pill').on('click.dmV4MoneyPill', '#dmV4IvPanel .dm-v4-rate-pill', function (e) {
            e.preventDefault();
            e.stopPropagation();
            if (!getIvCanEdit()) return;
            startMoneyPillEdit($(this));
        });

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
            var url = ($btn.attr('data-video-url') || $btn.data('videoUrl') || $row.attr('data-video-link') || $row.data('videoLink') || '').toString().trim();
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
        if (!audio || !$playBtn.length) return;

        var $bars = $wave.find('.dm-v4-voice-waveform__bar');
        var playedCount = 0;

        function syncWaveProgress() {
            if (!audio.duration) return;
            var ratio = audio.currentTime / audio.duration;
            playedCount = Math.floor(ratio * $bars.length);
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

        audio.addEventListener('loadedmetadata', function () {
            updateTimeLabel();
            syncWaveProgress();
        });
        audio.addEventListener('timeupdate', function () {
            updateTimeLabel();
            syncWaveProgress();
        });
        audio.addEventListener('ended', function () {
            $playBtn.removeClass('is-playing');
            playedCount = 0;
            $bars.removeClass('is-played');
            updateTimeLabel();
        });

        $playBtn.on('click', function () {
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

    window.dmV4InitIvEditable = initIvEditableFields;

})(jQuery);


