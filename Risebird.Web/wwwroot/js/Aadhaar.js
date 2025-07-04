var Buttonclick = 1;
//var isAadharVerified = false;
function openAadhaarpopup() {
    document.getElementById("divSubmit").style.display = "none";
    document.getElementById("divValidate").style.display = "inline-block";
    document.getElementById("divotpvalidation").style.display = "none";
    var modal = document.getElementById("Aadhaarpopup");
    modal.style.display = "block";
}
function closeAadhaarpopup() {
    var model = document.getElementById("Aadhaarpopup");
    model.style.display = "none";
    $("#aadhaarnumber").val('');
    $("#aadharimagecaptcha").attr("src", "");
}
function validateAadhaarnumber() {
    if (Buttonclick == 1) {
        showloader();
        $.ajax({
            type: "POST",
            url: "/Aadhaar/SendAadhaarNumber",
            data: { aadharnumber: $("#aadhaarnumber").val().trim() },
            success: function (data) {
                if (data.message.toLowerCase().includes("otp")) {
                    $("#aadhaaruuid").val(data.uuid);
                    document.getElementById("divotpvalidation").style.display = "block";
                    document.getElementById("divSubmit").style.display = "inline-block";
                    document.getElementById("divValidate").style.display = "none";
                }
                hideloader();
                alert(data.message);
                bindaadhaarnumber();
            }
        });
    }
}
function submitOtpnumber() {
    if (Buttonclick == 1) {
        showloader();
        $.ajax({
            type: "POST",
            url: "/Aadhaar/SubmitOTPNumber",
            data: { uuid: $("#aadhaaruuid").val(), otp: $("#txtAadhaarOtp").val().trim() },
            success: function (dataKYC) {
                openAadhaarKYCpopup();
                closeAadhaarpopup();
                hideloader();
                BindData(dataKYC);
                BindKYCData(dataKYC);
            }
        });
    }
}
function BindKYCData(data) {
    var html = '';
    html += '<table style="overflow:auto" width="100%">';
    html += '<tr><td rowspan="4"><img src="data:image/png;base64,' + data.doc_face + '" alt="Red dot" /></td><td>Name</td><td>' + data.name + '</td></tr>';
    html += '<tr><td>Address</td><td style="max-width:375px;white-space: break-spaces;word-break:break-word">' + data.address + '</td></tr>';
    html += '<tr><td>Gender</td><td>' + data.gender + '</td></tr>';
    html += '<tr><td>DOB</td><td>' + data.dob + '</td></tr>';
    html += '</table>';
    $("#divAadhaarKYC").empty().html(html);
    document.getElementById("btnSubmitDetails").disabled = false;
}
function openAadhaarKYCpopup() {
    var modal = document.getElementById("AadhaarKYCpopup");
    modal.style.display = "block";
}
function closeAadhaarKYCpopup() {
    var model = document.getElementById("AadhaarKYCpopup");
    model.style.display = "none";
}
function showloader() {
    $(".loader").show();
    Buttonclick = 0;
}
function hideloader() {
    $(".loader").hide();
    Buttonclick = 1;
}
function FetchAadhaarDetails(MCQID, candidateEmail) {
    $.ajax({
        type: "POST",
        url: "/Aadhaar/FetchAadhaarDetails",
        data: { MCQID: MCQID, candidateEmail: candidateEmail },
        success: function (data) {
            openAadhaarKYCpopup();
            if (data.type == 1) {
                BindCandidateKYCData(data);
            } else {
                BindAadhaarKYCData(data);
            }
        }
    });
}
function BindAadhaarKYCData(data) {
    var html = '';
    if (data.email != null) {
        if (data.passport_image != '' && data.passport_image != null) {
            html += '<table style="overflow:auto" width="100%">';
            html += '<tr><td rowspan="3"><img src="data:image/png;base64,' + data.doc_face + '" alt="Red dot" /></td><td>Name</td><td>' + data.name + '</td></tr>';
            html += '<tr><td>Address</td><td style="max-width:375px;white-space: break-spaces;word-break:break-word">' + data.address + '</td></tr>';
            html += '<tr><td>Gender</td><td>' + data.gender + '</td></tr>';
            html += '<tr><td rowspan="2"><img src="data:image/png;base64,' + data.passport_image + '" alt="Red dot" /></td><td>DOB</td><td>' + data.dob + '</td></tr>';
            html += '<tr><td>Face Matched</td><td>' + data.matchPercentage + '</td></tr>';
        } else {
            html += '<table style="overflow:auto" width="100%">';
            html += '<tr><td rowspan="4"><img src="data:image/png;base64,' + data.doc_face + '" alt="Red dot" /></td><td>Name</td><td>' + data.name + '</td></tr>';
            html += '<tr><td>Address</td><td style="max-width:375px;white-space: break-spaces;word-break:break-word">' + data.address + '</td></tr>';
            html += '<tr><td>Gender</td><td>' + data.gender + '</td></tr>';
            html += '<tr><td>DOB</td><td>' + data.dob + '</td></tr>';
        }
        html += '</table>';
    }
    if (data.email == null) {
        html = 'Aadhaar not yet verified for this candidate';
    }
    $("#divAadhaarKYC").empty().html(html);
}
function FetchInterviewAadhaarDetails(candidateEmail) {
    $.ajax({
        type: "POST",
        url: "/Aadhaar/FetchInterviewAadhaarDetails",
        data: { candidateEmail: candidateEmail },
        success: function (data) {
            openAadhaarKYCpopup();
            if (data.type == 1) {
                BindCandidateKYCData(data);
            } else {
                BindAadhaarKYCData(data);
            }
        }
    });
}
function BindCandidateKYCData(data) {
    var html = '';
    if (data.email != null) {
        html += '<table style="overflow:auto" width="100%">';
        html += '<tr><td rowspan="3"><img src="data:image/png;base64,' + data.doc_face + '" alt="Red dot" /></td><td>Name</td><td>' + data.name + '</td></tr>';
        html += '<tr><td>Address</td><td style="max-width:375px;white-space: break-spaces;word-break:break-word">' + data.address + '</td></tr>';
        html += '<tr><td>Gender</td><td>' + data.gender + '</td></tr>';
        html += '<tr><td rowspan="3"><img src="data:image/png;base64,' + data.passport_image + '" alt="Red dot" /></td><td>DOB</td><td>' + data.birthDate + '</td></tr>';
        html += '<tr><td>Country</td><td>' + data.nationality + '</td></tr>';
        html += '<tr><td>Face Matched</td><td>' + data.matchPercentage + '</td></tr>';
        html += '</table>';
    }
    if (data.email == null) {
        html = 'Not yet candidate verified for this candidate';
    }
    $("#divAadhaarKYC").empty().html(html);
}
