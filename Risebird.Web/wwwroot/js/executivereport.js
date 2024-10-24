function GenerateExecutiveReport(interviewid,irfid){
     $.ajax({
            type: "POST",
            url: "/Candidate/GenerateExecutiveReportLink",
            data: { 'interviewid': interviewid, 'irfid': irfid },
            success: function (result) {
                debugger;
                window.open(result , '_blank');
            }
     });
}