function MarkedColdCandidate(CandidateId, IRFId, CandidateEmail, InterviewId, irfIntId) {
    if (CandidateId != '') {
        $.ajax({
            url: '/Dashboard/MarkedColdCandidate',
            data: { 'CandidateId': CandidateId, 'IRFId': IRFId, 'CandidateEmail': CandidateEmail, 'InterviewId': InterviewId, 'irfIntId': irfIntId },
            type: "POST",
            success: function (data) {
                Swal.fire({
                    title: '',
                    text: data, // Use the data variable here for the message
                    width: '50%', // You can use 'px' or '%' for the width value
                }).then(function () {
                    location.reload(true);
                });
            }
        });
    }
    else {
        swal.fire("Please Marked cold candidate.");
    }
}


function UndoMarkedColdCandidate(CandidateId, IRFId, CandidateEmail, irfIntId, scheduleUniqueid, InterviewId) {
    if (CandidateId != '') {


        swal.fire({
            title: 'Are you sure?',
            text: "You want to undo the 'Cold Status' that is currently marked for" + " (" + CandidateEmail + ") " + " for skill channel " + " (" + IRFId + ") ",
            type: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Yes, Undo!',
            cancelButtonText: "Don't",
            preConfirm: function () {
                return new Promise(function (resolve, reject) {
                    $.ajax({
                        url: '/Dashboard/UndoMarkedColdCandidate',
                        type: 'POST',
                        data: { 'CandidateId': CandidateId, 'IRFId': IRFId, 'CandidateEmail': CandidateEmail, 'irfIntId': irfIntId },
                        success: function (data) {
                            if (data != null && data.toLowerCase().includes('success')) {
                                resolve(data);
                            }
                            else {
                                swal.fire("Error cancelling!", "Please try again", "error");

                            }

                        }
                    });

                })
            },
            allowOutsideClick: false
        })
            .then(function (result) {
                if (result.dismiss !== 'cancel') {
                    swal.fire({
                        title: 'Cancelled!',
                        type: 'success',
                        html: '<p>Undo cold candidate.</p>',
                        showCancelButton: false,
                        confirmButtonColor: '#3085d6',
                        confirmButtonText: 'Close!',
                        allowOutsideClick: false
                    }).then(function () {

                        ReschuduleMeeting(InterviewId, scheduleUniqueid, CandidateEmail);
                    })

                }
                else if (result.dismiss === 'cancel') {
                    swal.fire(
                        'Aborted!',
                        'Meeting remains confirmed! :)',
                        'error'
                    )
                }
            })


    }

    function ReschuduleMeeting(interviewid, uniqueid, candidateEmailid) {
        swal.fire({
            title: 'Are you sure?',
            text: 'Do you really want to cancel this meeting and reschedule?',
            type: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Yes, Reschedule!',
            preConfirm: function () {
                return new Promise(function (resolve, reject) {
                    $.ajax({
                        url: '/InterviewScheduler/GenerateCandidatescheuleLink',
                        method: 'POST',
                        data: {
                            'CandidateEmailid': candidateEmailid,
                            'interviewid': interviewid,
                            'uniqueid': uniqueid,
                            'schedulertype': 2
                        },
                        success: function (result) {
                            window.open(result, '_blank');
                            resolve(result)
                        },
                        error: function () {
                            swal.fire("Error rescheduling!", "Please try again", "error");
                        }
                    })
                })
            },
            allowOutsideClick: false
        }).then(function (result) {
            if (result.dismiss !== 'cancel') {
                //location.reload(true);
            }
            else if (result.dismiss === 'cancel') {
                swal.fire(
                    'Cancelled',
                    'reschedule cancelled! :)',
                    'error'
                )
            }
        })
    }


}