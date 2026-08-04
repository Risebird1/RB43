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
})();
