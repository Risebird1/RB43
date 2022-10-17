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
        data: { 'sid': callSid, 'IRFId': irfId, 'callRecipient': callRecipientEmail, 'callRecipientPhoneNumber': callRecipientPhoneNumber, 'recipientType': recipientType },
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
    recipientType = 'Candidate';
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
    document.getElementById("idCallingTitle").innerHTML = " Call ";
    callRecipient(callRecipientPhoneNumber);

}
function CommonCallButtonClick(name, mobileNumber, emailId = '', interviewIdParam = null, irfIdParam = null, recipientTypeParam = null) {
    callRecipientName = name;
    if (interviewIdParam != null) {
        interviewId = interviewIdParam;
    }
    callRecipientPhoneNumber = mobileNumber;
    callRecipientEmail = emailId;
    if (irfIdParam != null) {
        irfId = irfIdParam;
    }
    if (recipientTypeParam != null) {
        recipientType = recipientTypeParam;
    }
    // Get the modal
    var modal = document.getElementById("myCallModal");
    // When the user clicks the button, open the modal
    modal.style.display = "block";
    var appendText = "Calling... " + "<b>" + callRecipientName + "</b>";
    document.getElementById("callingLabel").innerHTML = appendText;
    document.getElementById("idCallingTitle").innerHTML = " Call ";

    callRecipient(callRecipientPhoneNumber);
}
function CommonCallButtonClickJson(imid) {
    var name = $("#hidcandidate_" + imid).val();
    var mobile = $("#hidmobile" + imid).val();
    var emailId = $("#hidcandidateEmail_" + imid).val();

    // Get the modal
    //CallButtonClickInterviewer(interviewer, mobile, emailId);
    CommonCallButtonClick(name, mobile, emailId, imid, null, 'Candidate');

}

function ModalButtonClickHR(phone, receiverName, type, interviewid, emailId = '') {
    callRecipientName = receiverName;
    interviewId = interviewid;
    callRecipientPhoneNumber = phone;
    callRecipientEmail = emailId;
    recipientType = type;
    // Get the modal
    var modal = document.getElementById("myCallModal");
    // When the user clicks the button, open the modal
    modal.style.display = "block";
    var appendText = "Calling... " + "<b>" + callRecipientName + "</b>";
    document.getElementById("callingLabel").innerHTML = appendText;
    document.getElementById("idCallingTitle").innerHTML = "Call " + type;
    //document.getElementById("idTitle").innerHTML = "Call " + type;
    callRecipient(callRecipientPhoneNumber);
}
function ModalButtonClickJsonHR(phone, id, type) {
    if (type == 1) {
        var name = $('#hidinterviewer' + id).val();
        var emailId = $('#hidinterviewerEmailId' + id).val();
        ModalButtonClickHR(phone, name, 'Interviewer', id, emailId);
    }
    else if (type == 2) {

        var name = $('#hidcandidate' + id).val();
        var emailId = $('#hidcandidateEmail' + id).val();
        ModalButtonClickHR(phone, name, 'Candidate', id, emailId);

}
    else if (type == 3) {

        var name = $('#hidclienthr' + id).val();
        ModalButtonClickHR(phone, name, 'Client HR', id);
    }

    else if (type == 4) {

        var name = $('#hidstaff' + id).val();
        ModalButtonClickHR(phone, name, 'StaffingAgency', id);

    }
}


function closeCallPopUp() {
    hangUp();
    var modal = document.getElementById("myCallModal");
    modal.style.display = "none";
}

function CallModalButtonClick(emailId, phone, interviewid, type = '') {
    callRecipientName = emailId;
    callRecipientEmail = emailId;
    callRecipientPhoneNumber = phone;
    interviewId = interviewid;
    recipientType = type;    
    // Get the modal
    var modal = document.getElementById("myCallModal");
    // When the user clicks the button, open the modal
    modal.style.display = "block";
    var appendText = "Calling... " + "<b>" + callRecipientName + "</b>";
    document.getElementById("callingLabel").innerHTML = appendText;
    document.getElementById("idCallingTitle").innerHTML = " Call ";
    callRecipient(callRecipientPhoneNumber);
}


