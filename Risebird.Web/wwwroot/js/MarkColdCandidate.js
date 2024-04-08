function MarkedColdCandidate(CandidateId, IRFId, CandidateEmail, InterviewId, irfIntId) {
    console.log(InterviewId, irfIntId)
    if (CandidateId != '') {
        $.ajax({
            url: '/Dashboard/MarkedColdCandidate',
            data: { 'CandidateId': CandidateId, 'IRFId': IRFId, 'CandidateEmail': CandidateEmail, 'InterviewId': InterviewId,'irfIntId':irfIntId },
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