(function () {
    var currentWeekend = 'This';
    var spotlightSaveTimer = null;

    function setActiveToggle(weekend) {
        $('.drive-weekend-toggle').removeClass('active');
        $('.drive-weekend-toggle[data-weekend="' + weekend + '"]').addClass('active');
    }

    function currentBucket() {
        var fromDom = $('#driveWeekendContainer .drive-spotlight-bar').data('weekend-bucket');
        if (fromDom) return fromDom;
        return currentWeekend === 'Next' ? 'Next' : 'This';
    }

    function saveRowActions($select) {
        if (!$select || !$select.length) {
            return;
        }

        $.post('/Dashboard/SaveRowActions', {
            refType: $select.data('reftype'),
            refId: $select.data('refid'),
            actions: ($select.val() || []).join(','),
            irfIntId: $select.data('irf-int-id') || 0,
            weekendBucket: $select.data('weekend-bucket') || currentBucket()
        });
    }

    window.initRowActions = function () {
        $('.row-actions').each(function () {
            var $el = $(this);
            if ($el.data('multiselect')) {
                $el.multiselect('destroy');
            }
        });

        if (!$.fn.multiselect) {
            return;
        }

        $('.row-actions').multiselect({
            includeSelectAllOption: true,
            selectedClass: 'active1',
            buttonWidth: '100%',
            maxHeight: 200,
            nonSelectedText: 'None selected',
            nSelectedText: ' selected',
            allSelectedText: 'All actions',
            onChange: function () {
                saveRowActions(this.$select);
            },
            onSelectAll: function () {
                saveRowActions(this.$select);
            },
            onDeselectAll: function () {
                saveRowActions(this.$select);
            }
        });
    };

    function syncSpotlightSelection(reload) {
        var $sel = $('#driveSpotlightIrf');
        if (!$sel.length) return;
        var ids = ($sel.val() || []).join(',');
        var bucket = currentBucket();
        $.post('/Dashboard/SyncWeekendDriveSpotlight', {
            weekendBucket: bucket,
            irfIds: ids
        }).done(function (res) {
            if (res && res.ok) {
                if (reload !== false) {
                    loadDriveWeekendData(currentWeekend);
                }
                return;
            }
            var msg = (res && res.message)
                ? res.message
                    : 'Spotlight save failed. Check DB permissions for WeekendDriveSpotlight.';
            if (window.swal && swal.fire) {
                swal.fire('Spotlight not saved', msg, 'error');
            } else {
                alert(msg);
            }
        }).fail(function () {
            if (window.swal && swal.fire) {
                swal.fire('Spotlight not saved', 'Request failed. Check you are signed in as Admin/HR.', 'error');
            }
        });
    }

    function upsertSpotlightIrf(irfId, remove) {
        return $.post('/Dashboard/UpsertWeekendDriveSpotlight', {
            irfId: irfId,
            weekendBucket: currentBucket(),
            remove: !!remove
        });
    }

    window.initDriveSpotlightPicker = function () {
        var $sel = $('#driveSpotlightIrf');
        if (!$sel.length) {
            return;
        }
        if (!$.fn.multiselect) {
            $sel.off('change.driveSpotlight').on('change.driveSpotlight', function () {
                clearTimeout(spotlightSaveTimer);
                spotlightSaveTimer = setTimeout(function () { syncSpotlightSelection(true); }, 400);
            });
            return;
        }
        if ($sel.data('multiselect')) {
            $sel.multiselect('destroy');
        }
        $sel.multiselect({
            includeSelectAllOption: true,
            enableFiltering: true,
            enableCaseInsensitiveFiltering: true,
            filterPlaceholder: 'Search IRFs…',
            selectedClass: 'active1',
            buttonWidth: '100%',
            maxHeight: 200,
            nonSelectedText: 'Select spotlight IRFs',
            nSelectedText: ' IRFs selected',
            allSelectedText: 'All IRFs',
            onChange: function () {
                clearTimeout(spotlightSaveTimer);
                spotlightSaveTimer = setTimeout(function () { syncSpotlightSelection(true); }, 400);
            },
            onSelectAll: function () {
                clearTimeout(spotlightSaveTimer);
                spotlightSaveTimer = setTimeout(function () { syncSpotlightSelection(true); }, 400);
            },
            onDeselectAll: function () {
                clearTimeout(spotlightSaveTimer);
                spotlightSaveTimer = setTimeout(function () { syncSpotlightSelection(true); }, 400);
            }
        });
    };

    window.recordDriveAction = function (actionRefId, actionKey) {
        var $sel = $('.row-actions[data-refid="' + actionRefId + '"]');
        if (!$sel.length) {
            return;
        }

        var vals = $sel.val() || [];
        if (vals.indexOf(actionKey) === -1) {
            vals.push(actionKey);
            $sel.val(vals);
            if ($sel.data('multiselect')) {
                $sel.multiselect('refresh');
            }
            saveRowActions($sel);
        }
    };

    window.plivoBatchCall = function (magId, scope, irfId, actionRefId) {
        if (!magId || magId <= 0) {
            swal.fire('No MAG mapped', 'This IRF has no assessment group — batch call is unavailable.', 'info');
            return;
        }

        var actionKey = scope === 'Campaign' ? 'campaignBatchCall' : 'magBatchCall';
        recordDriveAction(actionRefId, actionKey);

        $.get('/InterviewDemand/PlivoBatchCall', { magId: magId, scope: scope }, function (res) {
            if (res && res.success && res.url) {
                window.open(res.url, '_blank');
            } else if (res && res.urls && res.urls.length) {
                res.urls.forEach(function (u) { window.open(u, '_blank'); });
            } else {
                swal.fire(res && res.message ? res.message : 'Batch call route is not available for this MAG.');
            }
        }).fail(function () {
            swal.fire('Unable to start batch call.', 'Please try again.', 'error');
        });
    };

    window.sendMagEmail = function (magId, agName, actionRefId) {
        if (!magId || magId <= 0) {
            return;
        }
        recordDriveAction(actionRefId, 'magEmail');
        swal.fire({
            title: 'Send MAG application links?',
            text: 'Send MAG links for ' + (agName || 'this MAG') + ' to applicable interviewers.',
            type: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Yes, send',
            preConfirm: function () {
                return $.post('/IRF/SendMagApplicationLinks', { agId: magId, agName: agName });
            }
        }).then(function (result) {
            if (result.value) {
                swal.fire('Sent!', result.value || 'MAG emails triggered.', 'success');
            }
        });
    };

    window.openCampaignEmail = function (magId, encAgName, actionRefId) {
        if (!magId || magId <= 0) {
            return;
        }
        recordDriveAction(actionRefId, 'campaignEmail');
        window.open('/IRF/CreateCampaign?assessmentGroupId=' + magId +
            '&assessmentGroupName=' + encAgName +
            '&isAlreadyMasterAG=false&isAlreadySecondary=false', '_blank');
    };

    window.openStuckApplicants = function (magName, actionRefId) {
        recordDriveAction(actionRefId, 'processStuckApplicants');
        var query = 'funnelStage=InterestCollection&status=InProgress';
        if (magName) {
            query += '&magName=' + encodeURIComponent(magName);
        }
        window.open('/NaukriOnboarding?' + query, '_blank');
    };

    window.inviteFreshInterviewers = function (irfIntId, irfId, actionRefId) {
        recordDriveAction(actionRefId, 'inviteFresh');
        if (irfIntId > 0 && typeof CopyInterviewerApplicationLink === 'function') {
            CopyInterviewerApplicationLink(irfIntId, irfId);
        } else {
            window.open('/NaukriOnboarding', '_blank');
        }
    };

    window.sendOpenInterviewNotification = function (irfIntId, actionRefId) {
        if (!irfIntId || irfIntId <= 0) {
            return;
        }
        recordDriveAction(actionRefId, 'openInterviewNotification');
        if (typeof SendOpenInterview === 'function') {
            SendOpenInterview(irfIntId);
            return;
        }

        swal.fire({
            title: 'Send open interview notification?',
            type: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Yes, send',
            preConfirm: function () {
                return $.post('/Interviewer/SendOpenURLNotification', { irfId: irfIntId });
            }
        }).then(function (result) {
            if (result.value === 'Success') {
                swal.fire('Sent!', 'Open interview notification sent.', 'success');
            } else if (result.value) {
                swal.fire('Failed', result.value, 'error');
            }
        });
    };

    window.openDriveMembers = function (type, id, irfIntId, irfName, agName) {
        if (type === 'IRF') {
            getDriveIrfMembers(id, irfName);
            return;
        }

        if (type === 'MAG' && id > 0) {
            GetAssessmentGroupInterviewerMembers(id, agName || '', irfIntId, irfName);
        }
    };

    window.openDriveRelatedMagMembers = function (magId, irfIntId, irfName) {
        if (!magId || magId <= 0) {
            return;
        }

        $.ajax({
            url: '/InterviewDemand/GetRelatedMagInterviewerMembers',
            data: { magId: magId, irfIntId: irfIntId, irfName: irfName },
            success: function (data) {
                var modal = document.getElementById('interviewerMemberModal');
                if (!modal) {
                    return;
                }
                modal.style.display = 'block';
                $('#id_agMemberHeading').html('Related MAG interviewers for (' + (irfName || 'IRF') + ')');
                $('#memberTableBody').html(data);
            }
        });
    };

    window.getDriveIrfMembers = function (id, name) {
        $.ajax({
            url: '/IRF/GetIRFInterviewerMembers',
            data: { irfId: id },
            success: function (data) {
                var modal = document.getElementById('interviewerMemberModal');
                modal.style.display = 'block';
                $('#id_agMemberHeading').html('Interviewer Details for (' + (name || 'IRF') + ')');
                $('#memberTableBody').html(data);
            }
        });
    };

    var driveLoadXhr = null;

    window.loadDriveWeekendData = function (weekend) {
        currentWeekend = weekend || currentWeekend;
        setActiveToggle(currentWeekend);
        $('#driveWeekendLoader').show();
        // Keep previous content visible so tab switches don't look "blank forever"

        if (driveLoadXhr && driveLoadXhr.readyState !== 4) {
            driveLoadXhr.abort();
        }

        var dataUrl = window.driveWeekendDataUrl || '/InterviewDemand/DriveWeekendData';

        driveLoadXhr = $.ajax({
            url: dataUrl,
            data: { weekend: currentWeekend },
            timeout: 45000,
            success: function (html) {
                $('#driveWeekendContainer').html(html);
                if (typeof initDriveSpotlightPicker === 'function') {
                    initDriveSpotlightPicker();
                }
                if (typeof initRowActions === 'function') {
                    initRowActions();
                }
            },
            error: function (xhr, status) {
                if (status === 'abort') {
                    return;
                }
                var message = 'Unable to load weekend drive data.';
                if (status === 'timeout') {
                    message = 'Weekend drive is taking too long. Try again, or reduce DriveThreshold.';
                } else if (xhr && xhr.status === 401) {
                    message = 'Your session expired. Please sign in again.';
                } else if (xhr && xhr.status) {
                    message += ' (' + xhr.status + ')';
                }

                $('#driveWeekendContainer').html(
                    '<div class="drive-empty"><div><i class="fas fa-exclamation-circle"></i></div><p>' +
                    message +
                    '</p></div>'
                );
            },
            complete: function (xhr, status) {
                if (status !== 'abort') {
                    $('#driveWeekendLoader').hide();
                }
            }
        });
    };

    $(document).ready(function () {
        $(document).on('click', '.drive-weekend-toggle', function () {
            loadDriveWeekendData($(this).data('weekend'));
        });

        $(document).on('click', '.drive-add-spotlight', function () {
            var id = parseInt($(this).data('irf-int-id'), 10) || 0;
            if (!id) return;
            var $btn = $(this).prop('disabled', true);
            upsertSpotlightIrf(id, false).done(function (res) {
                if (res && res.ok) {
                    loadDriveWeekendData(currentWeekend);
                    return;
                }
                $btn.prop('disabled', false);
                var msg = (res && res.message)
                    ? res.message
                    : 'Spotlight save failed. Check DB permissions for WeekendDriveSpotlight.';
                if (window.swal && swal.fire) {
                    swal.fire('Spotlight not saved', msg, 'error');
                } else {
                    alert(msg);
                }
            }).fail(function () {
                $btn.prop('disabled', false);
                if (window.swal && swal.fire) {
                    swal.fire('Spotlight not saved', 'Request failed. Check you are signed in as Admin/HR.', 'error');
                }
            });
        });

        var closeMember = document.getElementById('membersPopUp');
        if (closeMember) {
            closeMember.onclick = function () {
                var modal = document.getElementById('interviewerMemberModal');
                if (modal) {
                    modal.style.display = 'none';
                }
            };
        }

        if (typeof initDriveSpotlightPicker === 'function') {
            initDriveSpotlightPicker();
        }
        if (typeof initRowActions === 'function') {
            initRowActions();
        }

        if ($('#driveWeekendContainer').children().length === 0) {
            loadDriveWeekendData('This');
        }
    });

    function demandToast(msg, type) {
        type = type || 'info';
        var host = document.getElementById('driveToastHost');
        if (!host) {
            host = document.createElement('div');
            host.id = 'driveToastHost';
            host.className = 'drive-toast-host';
            document.body.appendChild(host);
        }

        var meta = {
            success: { icon: 'fa-check', title: 'Done' },
            error: { icon: 'fa-exclamation', title: 'Couldn’t complete' },
            wait: { icon: 'fa-spinner fa-spin', title: 'Working' },
            info: { icon: 'fa-info', title: 'Notice' }
        }[type] || { icon: 'fa-info', title: 'Notice' };

        var el = document.createElement('div');
        el.className = 'drive-toast is-' + type;
        el.innerHTML =
            '<span class="drive-toast__icon"><i class="fas ' + meta.icon + '"></i></span>' +
            '<div class="drive-toast__body"><div class="drive-toast__title">' + meta.title + '</div>' +
            '<div class="drive-toast__msg"></div></div>' +
            '<button type="button" class="drive-toast__close" aria-label="Dismiss">&times;</button>';
        el.querySelector('.drive-toast__msg').textContent = msg || '';
        host.appendChild(el);

        var remove = function () {
            if (!el.parentNode) return;
            el.classList.add('is-out');
            setTimeout(function () { if (el.parentNode) el.parentNode.removeChild(el); }, 180);
        };
        el.querySelector('.drive-toast__close').onclick = remove;
        if (type !== 'wait') setTimeout(remove, 2800);
        return { close: remove, el: el };
    }

    function generateCrossMagLink(magId) {
        magId = parseInt(magId || '0', 10);
        if (!magId) {
            demandToast('Select a target MAG.', 'info');
            return;
        }
        $.post('/InterviewDemand/GenerateMAGApplicationLink', { targetMagId: magId })
            .done(function (res) {
                if (res && (res.ok || res.Ok) && (res.url || res.Url)) {
                    window.prompt('Cross-MAG entry link (standing, does not expire). Interviewers from other MAGs can enter this MAG:', res.url || res.Url);
                    demandToast('Link ready — copy it from the prompt.', 'success');
                } else {
                    demandToast((res && (res.message || res.Message)) || 'Could not generate link.', 'error');
                }
            })
            .fail(function () { demandToast('Could not generate link.', 'error'); });
    }

    function jsonFlag(res, key) {
        if (!res) return undefined;
        if (res[key] !== undefined) return res[key];
        var pascal = key.charAt(0).toUpperCase() + key.slice(1);
        return res[pascal];
    }

    function isJsonOk(res) {
        return jsonFlag(res, 'ok') === true || jsonFlag(res, 'success') === true;
    }

    function spotlightTableHtml() {
        return '<div class="drive-table-wrap"><table class="table drive-table drive-table-volume js-spotlight-table">' +
            '<thead><tr><th>IRF / Skill</th><th>MAG</th><th>Volume</th><th>Unassigned</th><th>Outreach</th></tr></thead>' +
            '<tbody></tbody></table></div>';
    }

    function ensureSpotlightTable() {
        var $table = $('#driveSpotlightBlock .js-spotlight-table');
        if ($table.length) return $table;
        $('#driveSpotlightEmpty').remove();
        $('#driveSpotlightBlock').html(spotlightTableHtml());
        return $('#driveSpotlightBlock .js-spotlight-table');
    }

    function setBlockCount(id, delta) {
        var $el = $(id);
        if (!$el.length) return;
        var n = parseInt($el.text() || '0', 10) + delta;
        if (n < 0) n = 0;
        $el.text(n);
    }

    function setPinButton($row, pinned, irfId) {
        var $toolbar = $row.find('.drive-actions-toolbar');
        $toolbar.find('.js-pin-priority, .js-unpin-priority').remove();
        if (pinned) {
            $toolbar.append(
                '<button type="button" class="drive-action-btn js-unpin-priority" title="Unpin (soft)" data-irf="' + irfId + '" data-irf-id="' + irfId + '">' +
                '<i class="fas fa-times"></i></button>'
            );
        } else {
            $toolbar.append(
                '<button type="button" class="drive-action-btn js-pin-priority" title="Mark high priority" data-irf="' + irfId + '" data-irf-id="' + irfId + '">' +
                '<i class="fas fa-thumb-tack"></i></button>'
            );
        }
    }

    function moveRowToSpotlight($row) {
        if (!$row || !$row.length) return;
        if ($('#driveSpotlightBlock tr[data-irf="' + $row.attr('data-irf') + '"]').length) {
            $row.remove();
            return;
        }
        var irfId = $row.attr('data-irf');
        var $fromVolume = $row.closest('#driveVolumeBlock').length > 0;
        $row.find('.drive-pin-meta').remove();
        $row.find('td:first').append('<div class="drive-pin-meta">Pinned just now</div>');
        setPinButton($row, true, irfId);
        ensureSpotlightTable().find('tbody').prepend($row);
        setBlockCount('#driveSpotlightCount', 1);
        if ($fromVolume) {
            setBlockCount('#driveVolumeCount', -1);
            if (!$('#driveVolumeBlock tbody tr').length) {
                $('#driveVolumeBlock').html('<div class="drive-empty drive-empty-compact" id="driveVolumeEmpty"><p>No IRFs with future scheduled volume.</p></div>');
            }
        }
    }

    function insertSpotlightRow(irfId, label, magName, magId) {
        if ($('#driveSpotlightBlock tr[data-irf="' + irfId + '"]').length) return;
        magId = parseInt(magId || '0', 10);
        var magCell = magName ? magName : '—';
        var linkBtn = magId > 0
            ? '<button type="button" class="drive-action-btn js-cross-mag-link" title="Cross-MAG entry link" data-mag="' + magId + '"><i class="fas fa-link"></i></button>'
            : '';
        var $row = $(
            '<tr data-irf="' + irfId + '" data-mag="' + magId + '">' +
            '<td><div class="drive-irf-id"></div><div class="drive-pin-meta">Pinned just now</div></td>' +
            '<td></td>' +
            '<td><span class="drive-badge drive-badge-total">—</span></td>' +
            '<td><span class="drive-unassigned-ok">—</span></td>' +
            '<td><div class="drive-actions-toolbar">' + linkBtn +
            '<button type="button" class="drive-action-btn js-unpin-priority" title="Unpin (soft)" data-irf="' + irfId + '" data-irf-id="' + irfId + '"><i class="fas fa-times"></i></button>' +
            '</div></td></tr>'
        );
        $row.find('.drive-irf-id').text(label || ('#' + irfId));
        $row.find('td').eq(1).text(magCell);
        ensureSpotlightTable().find('tbody').prepend($row);
        setBlockCount('#driveSpotlightCount', 1);
        var $vol = $('#driveVolumeBlock tr[data-irf="' + irfId + '"]');
        if ($vol.length) {
            $vol.remove();
            setBlockCount('#driveVolumeCount', -1);
        }
    }

    function pinHighPriority(irfId, ctx) {
        irfId = parseInt(irfId || '0', 10);
        ctx = ctx || {};
        if (!irfId) {
            demandToast('Select an IRF first.', 'info');
            return;
        }
        if (ctx.$row && ctx.$row.length) {
            moveRowToSpotlight(ctx.$row);
        } else {
            insertSpotlightRow(irfId, ctx.label, ctx.magName, ctx.magId);
        }
        var wait = demandToast('Marking as high priority…', 'wait');
        $.ajax({
            url: '/InterviewDemand/PinHighPriority',
            type: 'POST',
            data: { irfId: irfId },
            timeout: 15000
        }).done(function (res) {
            if (wait && wait.close) wait.close();
            if (isJsonOk(res)) {
                demandToast('Pinned to spotlight. Visible to everyone on this page.', 'success');
            } else {
                demandToast(jsonFlag(res, 'message') || 'Could not pin.', 'error');
            }
        }).fail(function () {
            if (wait && wait.close) wait.close();
            demandToast('Could not pin. Try again.', 'error');
        });
    }

    $(document).on('click', '.js-cross-mag-link', function () {
        if (this.disabled) return;
        generateCrossMagLink($(this).data('mag'));
    });

    $(document).on('click', '.js-pin-priority', function (e) {
        e.preventDefault();
        var $btn = $(this).closest('.js-pin-priority');
        pinHighPriority($btn.attr('data-irf') || $btn.data('irf'), { $row: $btn.closest('tr') });
    });

    $(document).on('click', '.js-unpin-priority', function (e) {
        e.preventDefault();
        e.stopPropagation();
        var $btn = $(this).closest('.js-unpin-priority');
        if ($btn.prop('disabled') || $btn.data('busy')) return;
        var irfId = parseInt($btn.attr('data-irf') || $btn.attr('data-irf-id') || $btn.data('irf') || '0', 10);
        if (!irfId) {
            demandToast('Missing IRF id — cannot unpin.', 'error');
            return;
        }

        var $row = $btn.closest('tr');
        $btn.data('busy', true).prop('disabled', true);
        var wait = demandToast('Removing from spotlight…', 'wait');

        $.ajax({
            url: '/InterviewDemand/UnpinHighPriority',
            type: 'POST',
            data: { irfId: irfId },
            timeout: 15000
        }).done(function (res) {
            if (wait && wait.close) wait.close();
            if (isJsonOk(res)) {
                $row.find('.drive-pin-meta').remove();
                setPinButton($row, false, irfId);
                var $volTable = $('#driveVolumeBlock .js-volume-table');
                if ($volTable.length) {
                    $volTable.find('tbody').prepend($row);
                } else {
                    $row.fadeOut(120, function () { $(this).remove(); });
                }
                setBlockCount('#driveSpotlightCount', -1);
                if ($volTable.length) setBlockCount('#driveVolumeCount', 1);
                if (!$('#driveSpotlightBlock tbody tr').length) {
                    $('#driveSpotlightBlock').html('<div class="drive-empty drive-empty-compact" id="driveSpotlightEmpty"><p>No high-priority IRFs. Use Mark high priority to pin one (self or other MAG).</p></div>');
                }
                demandToast('Removed from spotlight.', 'success');
            } else {
                $btn.data('busy', false).prop('disabled', false);
                demandToast(jsonFlag(res, 'message') || 'Could not unpin.', 'error');
            }
        }).fail(function (xhr, status) {
            if (wait && wait.close) wait.close();
            $btn.data('busy', false).prop('disabled', false);
            demandToast(status === 'timeout' ? 'Unpin timed out. Try again.' : 'Could not unpin.', 'error');
        });
    });

    function openDrivePinModal() { $('#drivePinModal').addClass('is-open'); }
    function closeDrivePinModal() { $('#drivePinModal').removeClass('is-open'); }

    $(document).on('click', '#driveOpenPin', function () { openDrivePinModal(); });
    $(document).on('click', '#drivePinCancel', function () { closeDrivePinModal(); });
    $(document).on('click', '#drivePinModal', function (e) {
        if (e.target === this) closeDrivePinModal();
    });

    var pinIrfCache = [];

    function fillDrivePinIrfSelect(q) {
        q = (q || '').toLowerCase();
        var $irf = $('#drivePinIrf').empty().append('<option value="">Select IRF</option>');
        pinIrfCache.forEach(function (x) {
            var label = x.label || x.irfId || ('#' + x.id);
            if (q && label.toLowerCase().indexOf(q) < 0) return;
            $irf.append($('<option/>').val(x.id).text(label));
        });
    }

    $(document).on('change', '#drivePinMag', function () {
        var magId = $(this).val();
        var $irf = $('#drivePinIrf').html('<option value="">Loading…</option>').prop('disabled', true);
        $('#drivePinIrfSearch').val('').prop('disabled', true);
        pinIrfCache = [];
        if (!magId) {
            $irf.html('<option value="">Select IRF</option>');
            return;
        }
        $.getJSON('/InterviewDemand/GetIRFsByMAG', { magId: magId })
            .done(function (res) {
                pinIrfCache = (res && res.data) ? res.data : [];
                fillDrivePinIrfSelect('');
                $irf.prop('disabled', false);
                $('#drivePinIrfSearch').prop('disabled', false);
            })
            .fail(function () {
                $irf.html('<option value="">Failed to load</option>');
                demandToast('Could not load IRFs for MAG', 'error');
            });
    });

    $(document).on('keyup', '#drivePinIrfSearch', function () {
        fillDrivePinIrfSelect($(this).val());
    });

    $(document).on('click', '#drivePinConfirm', function () {
        var $irf = $('#drivePinIrf');
        var irfId = $irf.val();
        var label = $.trim($irf.find('option:selected').text() || '');
        var magId = $('#drivePinMag').val();
        var magName = $.trim($('#drivePinMag option:selected').text() || '');
        closeDrivePinModal();
        pinHighPriority(irfId, { label: label, magName: magName, magId: magId });
    });
})();
