function openStats(statsProfile, userId, userEmail, statType, irfId = null) {
    var url = '';
    if (statsProfile == 'interviewer') {
        url = "/Interviewer/InterviewerStatsDetails"
    }
    else if (statsProfile == 'staffingAgency') {
        url = "/Client/";
    }
    $.ajax({
        type: "Get",
        url: url,
        data: { userId: userId, statType: statType, irfId: irfId },
        success: function (data) {
            var modal = document.getElementById("StatsDetailsModel");
            $("#idDetailsFor").html(userEmail);
            $("#idStatsType").html(statType);
            modal.style.display = "block";
            $("#divStatsBody").empty().html(data);
        }
    });
}

function closeModel() {
    var model = document.getElementById("StatsDetailsModel");
    model.style.display = "none";
}