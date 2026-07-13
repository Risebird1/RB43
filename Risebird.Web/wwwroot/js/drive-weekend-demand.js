(function () {
    var currentWeekend = 'This';

    function setActiveToggle(weekend) {
        $('.drive-weekend-toggle').removeClass('active');
        $('.drive-weekend-toggle[data-weekend="' + weekend + '"]').addClass('active');
    }

    function saveRowActions($select) {
        if (!$select || !$select.length) {
            return;
        }

        $.post('/Dashboard/SaveRowActions', {
            refType: $select.data('reftype'),
            refId: $select.data('refid'),
            actions: ($select.val() || []).join(',')
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
            maxHeight: 220,
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

    window.loadDriveWeekendData = function (weekend) {
        currentWeekend = weekend || currentWeekend;
        setActiveToggle(currentWeekend);
        $('#driveWeekendLoader').show();
        $('#driveWeekendContainer').empty();

        var dataUrl = window.driveWeekendDataUrl || '/InterviewDemand/DriveWeekendData';

        $.ajax({
            url: dataUrl,
            data: { weekend: currentWeekend },
            success: function (html) {
                $('#driveWeekendContainer').html(html);
                if (typeof initRowActions === 'function') {
                    initRowActions();
                }
            },
            error: function (xhr) {
                var message = 'Unable to load weekend drive data.';
                if (xhr && xhr.status === 401) {
                    message = 'Your session expired. Please sign in again.';
                } else if (xhr && xhr.responseText) {
                    message += ' (' + xhr.status + ')';
                }

                $('#driveWeekendContainer').html(
                    '<div class="drive-empty"><div><i class="fas fa-exclamation-circle"></i></div><p>' +
                    message +
                    '</p><p class="text-muted" style="font-size:11px;margin-top:6px;">Check that usp_DriveWeekendInterviews is deployed, or refresh after rebuilding the app.</p></div>'
                );
            },
            complete: function () {
                $('#driveWeekendLoader').hide();
            }
        });
    };

    $(document).ready(function () {
        $(document).on('click', '.drive-weekend-toggle', function () {
            loadDriveWeekendData($(this).data('weekend'));
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

        if ($('#driveWeekendContainer').children().length === 0) {
            loadDriveWeekendData('This');
        }
    });
})();
