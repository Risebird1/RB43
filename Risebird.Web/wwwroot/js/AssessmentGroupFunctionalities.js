

    var closeMember = document.getElementById("membersPopUp");
    closeMember.onclick = function () {
        var modal = document.getElementById("interviewerMemberModal");
        modal.style.display = "none";
    }
    function GetAssessmentGroupInterviewerMembers(id, agName, irfId = null, irfName = null) {
        $.ajax({
            url: '/IRF/GetAssessmentGroupInterviewerMembers',
            data: { 'Assessmentid': id, 'irfId' : irfId, 'irfName' : irfName },
            success: function (data) {
                var modal = document.getElementById("interviewerMemberModal");
                var modal = document.getElementById("interviewerMemberModal");
                modal.style.display = "block";
                $('#id_agMemberHeading').html('');
                $('#id_agMemberHeading').html('Interviewer Details for (' + agName + ')');
                $('#memberTableBody').html('');
                $('#memberTableBody').html(data);
            }
        });
    }
    