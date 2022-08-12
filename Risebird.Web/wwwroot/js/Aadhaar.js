function openAadhaarpopup() {
    document.getElementById("divSubmit").style.display = "none";
    document.getElementById("divValidate").style.display = "inline-block";
    document.getElementById("divotpvalidation").style.display = "none";
    var modal = document.getElementById("Aadhaarpopup");
    modal.style.display = "block";
    generateCaptcha();
}
function closeAadhaarpopup() {
    var model = document.getElementById("Aadhaarpopup");
    model.style.display = "none";
    $("#aadhaarnumber").val('');
    $("#aadhaarcaptcha").val('');
    $("#aadharimagecaptcha").attr("src", "");
}
function bindAadhaarData(data) {
    $("#aadharimagecaptcha").attr("src", "data:image/png;base64," + data.image);
    $("#aadhaaruuid").val(data.uuid);
}
function generateCaptcha() {
    showloader();
    $.ajax({
        type: "POST",
        url: "/Aadhaar/GenerateAadhaarCaptcha",
        data: { datenow: Date.now() },
        success: function (data) {
            bindAadhaarData(data);
            hideloader();
        }
    });
}
function validateAadhaarnumber() {
    showloader();
    $.ajax({
        type: "POST",
        url: "/Aadhaar/SendAadhaarNumber",
        data: { uuid: $("#aadhaaruuid").val(), aadharnumber: $("#aadhaarnumber").val().trim(), aadharcaptcha: $("#aadhaarcaptcha").val().trim(), datenow: Date.now() },
        success: function (data) {
            if (data.toLowerCase().includes("otp")) {
                document.getElementById("divotpvalidation").style.display = "block";
                document.getElementById("divSubmit").style.display = "inline-block";
                document.getElementById("divValidate").style.display = "none";
            } else {

            }
            hideloader();
            alert(data);
        }
    });
}
function submitOtpnumber() {
    showloader();
    $.ajax({
        type: "POST",
        url: "/Aadhaar/SubmitOTPNumber",
        data: { uuid: $("#aadhaaruuid").val(), otp: $("#txtAadhaarOtp").val().trim(), datenow: Date.now() },
        success: function (data) {
            $.ajax({
                type: "POST",
                url: "/Aadhaar/FetchAadhaarKYC",
                data: { uuid: $("#aadhaaruuid").val(), datenow: Date.now() },
                success: function (dataKYC) {
                    openAadhaarKYCpopup();
                    BindKYCData(dataKYC);
                    BindData(dataKYC);
                    hideloader();
                }
            });
        }
    });
}
function BindKYCData(data) {
    var html = '';
    html += '<table style="overflow:auto" width="100%">';
    html += '<tr><td rowspan="4"><img src="data:image/png;base64,' + data.doc_face + '" alt="Red dot" /></td><td>Name</td><td>' + data.name + '</td></tr>';
    html += '<tr style="word-wrap: break-word"><td>Address</td><td style="word-wrap: break-word">' + data.address + '</td></tr>';
    html += '<tr><td>Gender</td><td>' + data.gender + '</td></tr>';
    html += '<tr><td>DOB</td><td>' + data.dob + '</td></tr>';
    html += '</table>';
    $("#divAadhaarKYC").empty().html(html);
    document.getElementById("btnSubmitDetails").disabled = false;
}
function openAadhaarKYCpopup() {
    var modal = document.getElementById("AadhaarKYCpopup");
    modal.style.display = "block";
    closeAadhaarpopup();
}
function closeAadhaarKYCpopup() {
    var model = document.getElementById("AadhaarKYCpopup");
    model.style.display = "none";
}
function showloader() {
    $(".loader").show();
}
function hideloader() {
    $(".loader").hide();
}