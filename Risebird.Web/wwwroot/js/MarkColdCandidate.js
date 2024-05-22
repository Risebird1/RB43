function MarkedColdCandidate(CandidateId, IRFId, CandidateEmail, InterviewId, irfIntId) {
    if (CandidateId != '') {
        Swal.fire({
            title: 'Are you sure?',
            text: "You want to mark cold candidate "+ " (" + CandidateEmail + ") " + " for skill channel " + " (" + IRFId + ")", 
            width: '50%', // You can use 'px' or '%' for the width value
            showCancelButton: true,
            cancelButtonText: 'Cancel',
            allowOutsideClick: false,
            preConfirm: function ()
            {
                return new Promise(function (resolve, reject) {
                    $.ajax({
                        url: '/Dashboard/MarkedColdCandidate',
                        data: { 'CandidateId': CandidateId, 'IRFId': IRFId, 'CandidateEmail': CandidateEmail, 'InterviewId': InterviewId, 'irfIntId': irfIntId },
                        type: "POST",
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
                        html: '<p>Cold candidate mark.</p>',
                        showCancelButton: false,
                        confirmButtonColor: '#3085d6',
                        confirmButtonText: 'Close!',
                        allowOutsideClick: false
                    }).then(function () {                      
                        location.reload(true);
                    })

                }
                else if (result.dismiss === 'cancel') {
                    swal.fire(
                        'Aborted!'
                        
                    )
                }

            })

    }
    else {
        swal.fire("Please Marked cold candidate.");
    }
}


function MarkedColdCandidateForClient(CandidateId, IRFId, CandidateEmail, InterviewId, irfIntId,clientEmail,urlType) {
    if (CandidateId != '') {
       
        Swal.fire({
            title: 'Are you sure?',
            text: "You want to mark cold candidate " + " (" + CandidateEmail + ") " + " for skill channel " + " (" + IRFId + ")",
            width: '50%', // You can use 'px' or '%' for the width value
            showCancelButton: true,
            cancelButtonText: 'Cancel',
            allowOutsideClick: false,
            preConfirm: function () {
                return new Promise(function (resolve, reject) {
                    $.ajax({
                        url: '/Dashboard/MarkedColdCandidate',
                        data: { 'CandidateId': CandidateId, 'IRFId': IRFId, 'CandidateEmail': CandidateEmail, 'InterviewId': InterviewId, 'irfIntId': irfIntId },
                        type: "POST",
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
                        html: '<p>Cold candidate mark.</p>',
                        showCancelButton: false,
                        confirmButtonColor: '#3085d6',
                        confirmButtonText: 'Close!',
                        allowOutsideClick: false
                    }).then(function () {
                        if (urlType == 1) {
                            PotentialColdCandidateByClient(clientEmail);
                        }
                        else if (urlType == 2) {
                            RescheduleMeetingByClient(clientEmail);
                        }
                        else {
                        }
                        
                    })

                }
                else if (result.dismiss === 'cancel') {
                    swal.fire(
                        'Aborted!'

                    )
                }

            })

    }
    else {
        swal.fire("Please Marked cold candidate.");
    }
}

function UndoMarkedColdCandidate(CandidateId, IRFId, CandidateEmail, irfIntId, scheduleUniqueid, InterviewId,ClientEmail,CurrentType) {
    if (CandidateId != '')
    {
    swal.fire({
        title: 'Are you sure?',
        text: "You want to undo the 'Cold Status' that is currently marked for" + " ("+ CandidateEmail +") " + " for skill channel " +" ("+ IRFId +") ",
        type: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes, Undo!',
        cancelButtonText: "Don't",
        preConfirm: function () {
            return new Promise(function (resolve, reject)
            {
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
        .then(function (result)
        {
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

                        ReschuduleMeeting(InterviewId, scheduleUniqueid, IRFId, CandidateEmail, ClientEmail, CurrentType);
                    //location.reload(true);
                })
                
            }
            else if (result.dismiss === 'cancel') {
                swal.fire(
                    'Aborted!'                  
                )
            }
           
        })


    }

    function ReschuduleMeeting(interviewid, uniqueid, irfid, candidateEmailid, ClientEmail, CurrentType) {
        swal.fire({
            title: 'Are you sure?',
            text: 'Do you want to schedule this candidate?',
            type: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Yes, Schedule!',
            preConfirm: function () {
                return new Promise(function (resolve, reject) {
                    $.ajax({
                        url: '/InterviewScheduler/GenerateCandidatescheuleLink',
                        method: 'POST',
                        data: {
                            'CandidateEmailid': candidateEmailid,
                            'interviewid': interviewid,
                            'uniqueid': uniqueid,
                            'Irfid': irfid,
                            'schedulertype': 2
                        },
                        success: function (result) {
                            window.open(result, '_blank');
                            resolve(result)
                        },
                        error: function () {
                            swal.fire("Error scheduling!", "Please try again", "error");
                        }
                    })
                })
            },
            allowOutsideClick: false
        }).then(function (result) {
            if (result.dismiss !== 'cancel') {
                if (CurrentType == 1) {
                    ColdCandidateByClient(ClientEmail);
                }
                else {
                    window.location.reload(); // Reloads the page in the current tab

                }
            } else {
                swal.fire(
                    'Cancelled',
                    'schedule cancelled! :)',
                    'error'
                );
                if (CurrentType == 1) {
                    ColdCandidateByClient(ClientEmail);
                }
                else {
                    window.location.reload(); // Reloads the page in the current tab

                }
            }
        });
    }


}