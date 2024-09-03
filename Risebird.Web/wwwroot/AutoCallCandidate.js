function AutoCallCandidate(phoneNumber, irfId, candidateId, candidateName) {

    swal.fire({
        title: 'Call ' + candidateName + '?',
        text: "A system generated schedule call will be placed to the candidate.",
        type: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes!',
        preConfirm: function () {
            return new Promise(function (resolve, reject) {
                $.ajax({
                    url: '/Candidate/AutoCallCandidate',
                    data: { 'phoneNumber': phoneNumber, 'irfId': irfId, 'candidateId': candidateId },
                    success: function (result) {
                        if (result == true) {
                            resolve(result)

                        }
                        else {
                            swal.fire("Error calling!", "Please try again", "error");

                        }

                    },
                    error: function () {
                        swal.fire("Error calling!", "Please try again", "error");
                    }
                })
            })
        },
        allowOutsideClick: false
    }).then(function (result) {
        if (result.dismiss !== 'cancel') {
            swal.fire({
                title: 'Call placed!',
                type: 'success',
                html: '<p>Successfully completed the schedule call.</p>',
                showCancelButton: false,
                confirmButtonColor: '#3085d6',
                confirmButtonText: 'Close!',
                allowOutsideClick: false
            })
            //.then(function () {
            //    window.location = "/Interviewer/Index";
            //})
        }
        else if (result.dismiss === 'cancel') {
            swal.fire(
                'Cancelled!',
                '',
                'error'
            )
        }
    })
}
