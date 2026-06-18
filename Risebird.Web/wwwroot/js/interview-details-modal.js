/**
 * Shared Interview Details modal (Payment SLA + Completed Interviews Evidence column).
 */
(function (window, $) {
    'use strict';

    var _paymentSlaDetailInterviewId = '';

    function pickVmProp(obj, camel, pascal) {
        if (!obj) return undefined;
        if (obj[camel] !== undefined && obj[camel] !== null) return obj[camel];
        if (obj[pascal] !== undefined && obj[pascal] !== null) return obj[pascal];
        return undefined;
    }

    function getFetchUrl() {
        return window.InterviewDetailsModalConfig && window.InterviewDetailsModalConfig.fetchUrl
            ? window.InterviewDetailsModalConfig.fetchUrl
            : '/Interview/FetchMoreDetailsInterview';
    }

    window.openPaymentSlaInterviewDetailModal = function (interviewId) {
        _paymentSlaDetailInterviewId = String(interviewId);
        var modal = document.getElementById('paymentSlaInterviewDetailModal');
        if (!modal) return;

        $('#psaDetailInterviewDuration').text('Loading…');
        $('#psaDetailDeficitDuration').text('Loading…');
        $('#psaDetailCandidateRating').empty();
        $('#psaDetailInterviewerJoin').empty();
        $('#psaDetailInterviewerLateJoin').empty();
        $('#psaDetailMeetingStatus').text('Loading…');
        $('#psaDetailInterviewStatus').text('Loading…');
        modal.style.display = 'block';

        $.ajax({
            type: 'GET',
            url: getFetchUrl(),
            data: { interviewId: interviewId },
            traditional: true,
            success: function (rows) {
                if (!rows || !rows.length) {
                    $('#psaDetailInterviewDuration').text('');
                    $('#psaDetailDeficitDuration').text('');
                    $('#psaDetailCandidateRating').html('<span style="font-size:12px;color:#64748b;">No details found.</span>');
                    $('#psaDetailInterviewerJoin').empty();
                    $('#psaDetailInterviewerLateJoin').empty();
                    $('#psaDetailMeetingStatus').text('');
                    $('#psaDetailInterviewStatus').text('');
                    return;
                }
                var row = rows[0];

                var dur = pickVmProp(row, 'interviewDuration', 'InterviewDuration');
                var deficit = pickVmProp(row, 'deficit', 'Deficit');
                var rating = parseInt(pickVmProp(row, 'rating', 'Rating'), 10);
                var joinedRaw = pickVmProp(row, 'joinedon', 'Joinedon');
                var lateVal = pickVmProp(row, 'interviewerLateJoin', 'InterviewerLateJoin');
                var late = parseInt(lateVal, 10) !== 0;
                var video = pickVmProp(row, 'isVideoRequired', 'IsVideoRequired') === true
                    || pickVmProp(row, 'isVideoRequired', 'IsVideoRequired') === 'true'
                    || pickVmProp(row, 'isVideoRequired', 'IsVideoRequired') === 1;

                var interviewDateStr = pickVmProp(row, 'interviewDate', 'InterviewDate');
                var confirmation = String(pickVmProp(row, 'confirmationStatus', 'ConfirmationStatus') || '').trim();
                var meetingDesc = String(pickVmProp(row, 'meetingStatusStatusDesc', 'MeetingStatusStatusDesc') || '');
                var interviewStatDesc = String(pickVmProp(row, 'intervieweStatusStatDesc', 'IntervieweStatusStatDesc') || '');

                var meetingStatus = '';
                var idt = interviewDateStr ? new Date(interviewDateStr) : null;
                if (idt && !isNaN(idt.getTime()) && idt > new Date() && confirmation.toLowerCase() !== 'cancelled') {
                    meetingStatus = 'Scheduled';
                } else if (meetingDesc && meetingDesc !== '0') {
                    meetingStatus = meetingDesc;
                }

                var interviewStatus = (interviewStatDesc && interviewStatDesc !== '0') ? interviewStatDesc : '';

                $('#psaDetailInterviewDuration').text((dur !== undefined && dur !== null && dur !== '') ? (dur + ' (mins)') : '');
                $('#psaDetailDeficitDuration').text((deficit !== undefined && deficit !== null && deficit !== '') ? (deficit + ' (mins)') : '');

                var elRat = document.getElementById('psaDetailCandidateRating');
                if (elRat) {
                    elRat.innerHTML = '';
                    if (!isNaN(rating) && rating !== 0) {
                        var lbl = document.createElement('label');
                        lbl.style.cssText = 'color:#42a5f5;font-size:15px;cursor:pointer;margin:0;';
                        lbl.textContent = rating + ' ';
                        var span = document.createElement('span');
                        span.style.cssText = 'color:grey;text-transform:lowercase;';
                        span.textContent = 'out of 5';
                        lbl.appendChild(span);
                        if (typeof window.PaymentComplianceDetails === 'function') {
                            lbl.addEventListener('click', function () {
                                window.PaymentComplianceDetails(_paymentSlaDetailInterviewId, 'Candidate');
                            });
                        }
                        elRat.appendChild(lbl);
                    } else {
                        var na = document.createElement('span');
                        na.style.cssText = 'font-size:15px;color:grey';
                        na.textContent = 'NA';
                        elRat.appendChild(na);
                    }
                }

                var elJoin = document.getElementById('psaDetailInterviewerJoin');
                if (elJoin) {
                    elJoin.innerHTML = '';
                    if (video) {
                        var wrap = document.createElement('div');
                        wrap.style.cssText = 'position:relative;z-index:10;width:fit-content;margin-bottom:6px;';
                        var inner = document.createElement('div');
                        inner.className = 'absCenter';
                        inner.innerHTML = '<button class="infoButton" style="top:8px;left:-16px;"><div class="infoButton-btn" style="width:15px;height:15px;"><span class="infoButton-btn-text" style="position:relative;font-size:13px;width:10px;height:21px;left:-7px;top:0px;">i</span></div><div class="infoButton-container" style="bottom:calc(-100% + 35px);"><div class="infoButton-container-message" style="position:relative;top:-14px;padding:4px 7px;width:auto;"><b style="color:red">Interviewer video on</b> is mandatory by client, else 100% payment deduction</div></div></button>';
                        wrap.appendChild(inner);
                        elJoin.appendChild(wrap);
                    }
                    var joinedText = '';
                    if (joinedRaw) {
                        var jd = new Date(joinedRaw);
                        joinedText = isNaN(jd.getTime()) ? String(joinedRaw) : jd.toLocaleString();
                    }
                    var div = document.createElement('div');
                    div.textContent = joinedText;
                    elJoin.appendChild(div);
                }

                var elLate = document.getElementById('psaDetailInterviewerLateJoin');
                if (elLate) {
                    elLate.innerHTML = '';
                    if (late) {
                        var y = document.createElement('span');
                        y.style.color = 'green';
                        y.textContent = 'Yes';
                        elLate.appendChild(y);
                    } else {
                        var n = document.createElement('span');
                        n.style.cssText = 'color:#000;font-weight:400';
                        n.textContent = 'No';
                        elLate.appendChild(n);
                    }
                }

                $('#psaDetailMeetingStatus').text(meetingStatus);
                $('#psaDetailInterviewStatus').text(interviewStatus);
            },
            error: function () {
                $('#psaDetailInterviewDuration').text('');
                $('#psaDetailDeficitDuration').text('');
                $('#psaDetailCandidateRating').html('<span style="font-size:12px;color:#b91c1c;">Unable to load details.</span>');
                $('#psaDetailInterviewerJoin').empty();
                $('#psaDetailInterviewerLateJoin').empty();
                $('#psaDetailMeetingStatus').text('');
                $('#psaDetailInterviewStatus').text('');
            }
        });
    };

    window.closePaymentSlaInterviewDetailModal = function () {
        var m = document.getElementById('paymentSlaInterviewDetailModal');
        if (m) m.style.display = 'none';
    };
})(window, jQuery);
