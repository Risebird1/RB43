var actionbuttonclick = 1;
function captureInterviewerRelatedclick(type, buttonaction, interviewid, interviewerid) {
    var currenturl = window.location.href;
    if (actionbuttonclick == 1) {
        actionbuttonclick = 0;
        $.ajax({
            url: '/ActivityTracker/CaptureInterviewerRelatedPageClick',
            method: 'POST',
            async: true,
            data: {
                'type': type,
                'currenturl': currenturl,
                'buttonaction': buttonaction,
                'interviewid': interviewid,
                'interviewerid': interviewerid
            },
            success: function (result) {
                actionbuttonclick = 1;
            }
        });
    }
}