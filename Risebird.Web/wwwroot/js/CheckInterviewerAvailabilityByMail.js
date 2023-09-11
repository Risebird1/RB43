function CheckIntervieweravailbility(id, Name, assessmentGroupId) {
        swal.fire({
            title: 'Are you sure?',
            text: "Do you want to send the email to check the interviewer availability!",
            type: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Yes, go ahead',
            preConfirm: function () {
                return new Promise(function (resolve, reject) {
                    $.ajax({
                        url: '/IRF/CheckIntervieweravailbility',
                        data: { 'irfId': id, 'IRFName': Name, 'assessmentGroupId' : assessmentGroupId },
                        success: function (data) {
                            swal.fire(data);
                        }
                    });
                })
            },
            allowOutsideClick: false
        })

    }