
/* -----------------------------
   GLOBAL VARIABLES
------------------------------ */
var callRecipientName = "";
var irfId = "";
var callRecipientEmail = "";
var callRecipientPhoneNumber = "";
var recipientType = '';
var interviewId = '';

var device;              // Twilio.Device instance
var activeConnection;    // Current active call connection

/* -----------------------------
   MODAL HANDLER
------------------------------ */
var span = document.getElementById("callPopUp");
if (span) {
    span.onclick = function() {
        hangUp();
        document.getElementById("myCallModal").style.display = "none";
    };
}

/* -----------------------------
   TOKEN GENERATION & DEVICE
------------------------------ */
async function GenerateToken() {
    try {
        const response = await fetch("/TwilioCalls/Generate", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ page: window.location.pathname })
        });
        const data = await response.json();

        if (!device) {
            setupDevice(data.token);
        } else {
            device.updateToken(data.token);
        }

        setTimeout(GenerateToken, 300000); // Refresh token every 5 mins
    } catch (err) {
        console.error("Error generating token:", err);
    }
}

function setupDevice(token) {
    device = new Twilio.Device(token, {
        codecPreferences: ["opus","pcmu"],
        allowIncomingWhileBusy: true
    });

    // Device ready
    device.on("registered", () => console.log("Device registered"));
    device.on("unregistered", () => console.log("Device unregistered"));
    device.on("error", err => console.error("Device error:", err));

    // Incoming call handler (optional)
    device.on("incoming", connection => {
        console.log("Incoming call", connection);
        // Optional: auto accept or show modal
    });

    // Outgoing call connected
    device.on("connect", connection => {
        activeConnection = connection;
        document.getElementById("callingLabel").innerHTML =
            "In call with <b>" + callRecipientName + "</b>";
        if (connection.parameters && connection.parameters.CallSid) {
            startCallRecording(connection.parameters.CallSid);
        }
    });

    // Outgoing call disconnected
    device.on("disconnect", connection => {
        if (activeConnection === connection) activeConnection = null;
        document.getElementById("callingLabel").innerHTML = "Call disconnected!";
        setTimeout(afterTimeOut, 2000);
    });
}

/* -----------------------------
   CALL FUNCTIONS
------------------------------ */
function callRecipient(phoneNumber) {
    //debugger;
    //phoneNumber = "+918888571923";
    if (!device) return console.error("Device not initialized");

    callRecipientPhoneNumber = phoneNumber;
    if (!phoneNumber || phoneNumber.length < 9) return;

    device.connect({ phoneNumber });
}

function hangUp() {
    if (activeConnection) {
        activeConnection.disconnect();
        activeConnection = null;
    }
    if (device) device.disconnectAll();
}

function afterTimeOut() {
    document.getElementById("myCallModal").style.display = "none";
}

function startCallRecording(callSid) {
    fetch("/User/makecall?sid=" + encodeURIComponent(callSid)
        + "&IRFId=" + encodeURIComponent(irfId)
        + "&callRecipient=" + encodeURIComponent(callRecipientEmail)
        + "&callRecipientPhoneNumber=" + encodeURIComponent(callRecipientPhoneNumber)
        + "&recipientType=" + encodeURIComponent(recipientType),
        { method: "GET" }
    ).then(() => console.log("Recording request sent for CallSid:", callSid));
}

/* -----------------------------
   MODAL & BUTTON HELPERS
------------------------------ */
function showCallModal(name, type = '') {
    const modal = document.getElementById("myCallModal");
    modal.style.display = "block";
    document.getElementById("callingLabel").innerHTML = "Calling... <b>" + name + "</b>";
    document.getElementById("idCallingTitle").innerHTML = type ? "Call " + type : "Call";
}

function CallButtonClick(candidateObj, indexid) {
    callRecipientName = indexid === 1 ? candidateObj.Name : candidateObj.name;
    callRecipientEmail = indexid === 1 ? candidateObj.EmailId : candidateObj.emailId;
    irfId = indexid === 1 ? candidateObj.IRFId : candidateObj.irfId;
    recipientType = 'Candidate';

    showCallModal(callRecipientName);

    const phone = candidateObj.Phone || candidateObj.mobileNumber || candidateObj.phone;
    callRecipient(phone);
}

function CallButtonClickInterviewerjson(userId) {
    const name = $("#hidfirstname" + userId).val();
    const phone = $("#hidmobile" + userId).val();
    const email = $("#hidcandidateEmail_" + userId).val();
    CallButtonClickInterviewer(name, phone, email);
}

function CallButtonClickInterviewer(name, phoneNumber, emailId) {
    callRecipientName = name;
    callRecipientEmail = emailId;
    callRecipientPhoneNumber = phoneNumber;
    recipientType = 'Interviewer';
    irfId = '';

    showCallModal(callRecipientName);
    callRecipient(callRecipientPhoneNumber);
}

function CommonCallButtonClick(name, mobileNumber, emailId = '', interviewIdParam = null, irfIdParam = null, recipientTypeParam = null) {
    callRecipientName = name;
    if (interviewIdParam) interviewId = interviewIdParam;
    callRecipientPhoneNumber = mobileNumber;
    callRecipientEmail = emailId;
    if (irfIdParam) irfId = irfIdParam;
    if (recipientTypeParam) recipientType = recipientTypeParam;

    showCallModal(callRecipientName);
    callRecipient(callRecipientPhoneNumber);
}

function CommonCallButtonClickJson(imid) {
    const name = $("#hidcandidate_" + imid).val();
    const mobile = $("#hidmobile" + imid).val();
    const email = $("#hidcandidateEmail_" + imid).val();
    CommonCallButtonClick(name, mobile, email, imid, null, 'Candidate');
}

function CommonCallButtonClickJsonV2(imid) {
    const name = $("#hidcandidate_" + imid).val();
    const mobile = $("#hidmobile" + imid).val();
    const email = $("#hidcandidateEmail_" + imid).val();
    CommonCallButtonClick(name, mobile, email, null, null, 'Interviewer');
}

function ModalButtonClickHR(phone, receiverName, type, interviewid, emailId = '') {
    callRecipientName = receiverName;
    interviewId = interviewid;
    callRecipientPhoneNumber = phone;
    callRecipientEmail = emailId;
    recipientType = type;

    showCallModal(callRecipientName, type);
    callRecipient(callRecipientPhoneNumber);
}

function ModalButtonClickJsonHR(phone, id, type) {
    if (type === 1) {
        const name = $('#hidinterviewer' + id).val();
        const emailId = $('#hidinterviewerEmailId' + id).val();
        ModalButtonClickHR(phone, name, 'Interviewer', id, emailId);
    } else if (type === 2) {
        const name = $('#hidcandidate' + id).val();
        const emailId = $('#hidcandidateEmail_' + id).val();
        ModalButtonClickHR(phone, name, 'Candidate', id, emailId);
    } else if (type === 3) {
        const name = $('#hidclienthr' + id).val();
        ModalButtonClickHR(phone, name, 'Client HR', id);
    } else if (type === 4) {
        const name = $('#hidstaff' + id).val();
        ModalButtonClickHR(phone, name, 'StaffingAgency', id);
    }
}

function closeCallPopUp() {
    hangUp();
    document.getElementById("myCallModal").style.display = "none";
}

function CallModalButtonClick(emailId, phone, interviewid, type = '') {
    callRecipientName = emailId;
    callRecipientEmail = emailId;
    callRecipientPhoneNumber = phone;
    interviewId = interviewid;
    recipientType = type;

    showCallModal(callRecipientName);
    callRecipient(callRecipientPhoneNumber);
}

/* -----------------------------
   INIT
------------------------------ */
document.addEventListener("DOMContentLoaded", GenerateToken);

