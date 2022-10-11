//<script src="//static.twilio.com/libs/twiliojs/1.2/twilio.min.js?v=7"></script>
/*$.getScript("//static.twilio.com/libs/twiliojs/1.2/twilio.min.js?v=7");*/

var callRecipientName = "";
//var canIrfid = "";
var irfId = "";
//var cancandidateEmail = "";
var callRecipientEmail = "";
var callRecipientPhoneNumber = "";
var recipientType = '';
var interviewId = '';

var span = document.getElementById("callPopUp");
// When the user clicks on <span> (x), close the modal
span.onclick = function () {
    hangUp();
    var modal = document.getElementById("myCallModal");
    modal.style.display = "none";
}

function GenerateToken() {
    $.post("/TwilioCalls/Generate", { page: window.location.pathname }, function (data) {
        // Set up the Twilio Client Device with the token
        Twilio.Device.setup(data.token);
    });
    setTimeout(GenerateToken, 300000);
}
$(document).ready(function () {
    GenerateToken();
});

/* Callback to let us know Twilio Client is ready */
Twilio.Device.ready(function (device) {

});

function callRecipient(phoneNumber) {
    callRecipientPhoneNumber = phoneNumber;
    var params = {"phoneNumber": phoneNumber };
    Twilio.Device.connect(params);
}

function hangUp() {
    Twilio.Device.disconnectAll();
}

Twilio.Device.connect(function (connection) {
    var appendText = "In call with " + "<b>" + callRecipientName + "</b>";
    document.getElementById("callingLabel").innerHTML = appendText;
    startCallRecording(connection.parameters.CallSid);
});

function afterTimeOut() {
    var modal = document.getElementById("myCallModal");
    modal.style.display = "none";
}

Twilio.Device.disconnect(function (connection) {
    var appendText = "Call disconnected! ";
    document.getElementById("callingLabel").innerHTML = appendText;
    setTimeout(afterTimeOut, 2000);

});
function startCallRecording(callSid) {
    $.ajax({
        url: '/User/makecall',
        type: "GET",
        //data: {'sid': sid, 'IRFId': canIrfid, 'CandidateEmail': cancandidateEmail },
        data: { 'callSid': callSid, 'IRFId': irfId, 'callRecipient': callRecipientEmail, 'callRecipientPhoneNumber': callRecipientPhoneNumber, 'recipientType': recipientType },
        success: function (data) {
        }
    });
}
//function startCallRecordingInterviewer(sid) {
//    $.ajax({
//        url: '/User/makecallInterviewer',
//        type: "GET",
//        data: { 'sid': sid, 'CandidateEmail': cancandidateEmailid },
//        success: function (data) {
//            console.log("rsid:" + data);
//        }
//    });
//}
function CallButtonClick(candidateObj, indexid) {
    if (indexid == 1) {
        callRecipientName = candidateObj.Name;
        irfId = candidateObj.IRFId;
        //irfId = candidateObj.IRFName;
        callRecipientEmail = candidateObj.EmailId;
    }
    else {
        callRecipientName = candidateObj.name;
        irfId = candidateObj.irfId;
        //irfId = candidateObj.irfName;
        callRecipientEmail = candidateObj.emailId;
    }
    // Get the modal
    var modal = document.getElementById("myCallModal");
    // When the user clicks the button, open the modal
    modal.style.display = "block";
    var appendText = "Calling... " + "<b>" + callRecipientName + "</b>";
    document.getElementById("callingLabel").innerHTML = appendText;
    if (indexid == 1) {
        callRecipient(candidateObj.Phone);
    }

    else {
        callRecipient(candidateObj.phone);
    }
}

function CallButtonClickInterviewerjson(userId) {
    var interviewer = $("#hidfirstname" + userId).val();
    var mobile = $("#hidmobile" + userId).val();
    var emailId = $("#hidcandidateEmail_" + userId).val();

    // Get the modal
    CallButtonClickInterviewer(interviewer, mobile, emailId);

}
function CallButtonClickInterviewer(name, phoneNumber, emailId) {
    callRecipientName = name;
    callRecipientEmail = emailId;
    callRecipientPhoneNumber = phoneNumber;
    irfId = '';
    recipientType = 'Interviewer';
    // Get the modal
    var modal = document.getElementById("myCallModal");
    // When the user clicks the button, open the modal
    modal.style.display = "block";
    var appendText = "Calling... " + "<b>" + callRecipientName + "</b>";
    document.getElementById("callingLabel").innerHTML = appendText;
    callRecipient(callRecipientPhoneNumber);

}
function closeCallPopUp() {
    hangUp();
    var modal = document.getElementById("myCallModal");
    modal.style.display = "none";
}


