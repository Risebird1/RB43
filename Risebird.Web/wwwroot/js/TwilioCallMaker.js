// ----------------------------
// Global variables
// ----------------------------
let device;       // Twilio.Device instance
let activeCall;   // Current call instance

var callRecipientName = "";
var irfIdVar = "";
var callRecipientEmail = "";
var callRecipientPhoneNumber = "";
var recipientType = '';
var interviewId = '';

function isPlivoCallConfigured() {
    return window.rbPlivoConfigured === true;
}

function getCallProvider() {
    var stored = null;
    try {
        stored = localStorage.getItem("rb_call_provider");
    } catch (e) { }

    if (stored === "plivo" && isPlivoCallConfigured()) {
        return "plivo";
    }
    if (stored === "twilio") {
        return "twilio";
    }
    if (isPlivoCallConfigured() && window.rbDefaultProvider === "plivo") {
        return "plivo";
    }
    return "twilio";
}

function highlightCallProviderToggle() {
    var provider = getCallProvider();
    var twilioBtn = document.getElementById("rbProviderTwilio");
    var plivoBtn = document.getElementById("rbProviderPlivo");
    if (twilioBtn) {
        twilioBtn.classList.toggle("active", provider === "twilio");
        twilioBtn.disabled = false;
    }
    if (plivoBtn) {
        plivoBtn.classList.toggle("active", provider === "plivo");
        plivoBtn.disabled = !isPlivoCallConfigured();
        if (!isPlivoCallConfigured()) {
            plivoBtn.title = "Plivo is not configured";
        }
    }
}

function setCallProvider(p) {
    if (p === "plivo" && !isPlivoCallConfigured()) {
        return;
    }
    var previous = getCallProvider();
    try {
        localStorage.setItem("rb_call_provider", p);
    } catch (e) { }
    highlightCallProviderToggle();

    var inCall = window.rbCallInProgress === true;
    if (inCall && previous !== p && callRecipientPhoneNumber) {
        window.rbProviderSwitching = true;
        if (previous === "plivo") {
            if (typeof plivoHangUp === "function") {
                plivoHangUp();
            }
        } else {
            if (activeCall) {
                activeCall.disconnect();
                activeCall = null;
            }
            if (device && typeof device.disconnectAll === "function") {
                device.disconnectAll();
            }
        }
        window.rbCallInProgress = false;
        var label = document.getElementById("callingLabel");
        if (label) {
            label.innerHTML = "Calling... <b>" + callRecipientName + "</b>";
        }
        callRecipient(callRecipientPhoneNumber);
    }
}

if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", highlightCallProviderToggle);
} else {
    highlightCallProviderToggle();
}


// ----------------------------
// Initialization only after user gesture
// ----------------------------
let isDeviceInitialized = false;

async function initializeTwilioAfterGesture() {
    if (isDeviceInitialized) return; // already initialized
    console.log("Initializing Twilio Device...");

    await GenerateToken();
    isDeviceInitialized = true;
    console.log("Twilio Device ready.");
}

document.addEventListener("click", initializeTwilioAfterGesture, { once: true });
document.addEventListener("keydown", initializeTwilioAfterGesture, { once: true });


// ----------------------------
// Token generation + Device setup
// ----------------------------
async function GenerateToken() {
    try {
        const response = await fetch("/TwilioCalls/Generate", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ page: window.location.pathname })
        });

        const data = await response.json();

        if (typeof Twilio === "undefined" || typeof Twilio.Device !== "function") {
            console.error("Twilio Device is not available.");
            return;
        }

        if (!device) {
            setupDevice(data.token);
        } else if (typeof device.updateToken === "function") {
            await device.updateToken(data.token);
            console.log("Token updated successfully");
        }

        // Refresh token every 5 minutes
        setTimeout(GenerateToken, 300000);

    } catch (err) {
        console.error("Error generating token:", err);
    }
}

function setupDevice(token) {
    //debugger;
    if (typeof Twilio === "undefined" || typeof Twilio.Device !== "function") {
        console.error("Twilio Device is not available.");
        return;
    }
    device = new Twilio.Device(token, {
        logLevel: 1,
        codecPreferences: ["opus", "pcmu"],
        allowIncomingWhileBusy: true,
    });

    device.on("registered", () => console.log("✅ Twilio Device registered."));
    device.on("unregistered", () => console.log("🚫 Twilio Device unregistered."));
    device.on("error", (error) => console.error("❌ Device error:", error));
    device.on("incoming", (call) => console.log("📞 Incoming call detected:", call));

    console.log("Twilio Device initialized.");
}


// ----------------------------
// Outgoing Call
// ----------------------------
function callRecipient(phoneNumber) {
    callRecipientPhoneNumber = phoneNumber;

    if (getCallProvider() === "plivo") {
        if (typeof plivoCallRecipient === "function") {
            plivoCallRecipient(phoneNumber);
        } else {
            console.error("Plivo caller is not loaded.");
        }
        return;
    }

    if (!device) {
        console.error("Device not ready. Wait for initialization or user gesture.");
        return;
    }

    if (phoneNumber && phoneNumber.length >= 9) {
        const params = { phoneNumber };
        console.log("Dialing:", phoneNumber);
        activeCall = device.connect({ params });
        setupCallHandlers(activeCall);
    } else {
        console.warn("Invalid phone number");
    }
}


// ----------------------------
// Call Event Handlers
// ----------------------------
function setupCallHandlers(call) {
    //debugger;
    if (!call) return;

    call.on("ringing", () => {
        console.log("☎️ Call ringing...");
    });

    call.on("accept", () => {
        window.rbCallInProgress = true;
        const text = "In call with <b>" + callRecipientName + "</b>";
        document.getElementById("callingLabel").innerHTML = text;
        startCallRecording(call.parameters.CallSid);
    });

    call.on("disconnect", () => {
        window.rbCallInProgress = false;
        if (window.rbProviderSwitching) {
            window.rbProviderSwitching = false;
            return;
        }
        document.getElementById("callingLabel").innerHTML = "Call disconnected!";
        setTimeout(afterTimeOut, 2000);
    });

    call.on("cancel", () => {
        window.rbCallInProgress = false;
        if (window.rbProviderSwitching) {
            window.rbProviderSwitching = false;
            return;
        }
        document.getElementById("callingLabel").innerHTML = "Call cancelled!";
        setTimeout(afterTimeOut, 2000);
    });

    call.on("error", (error) => {
        window.rbCallInProgress = false;
        console.error("⚠️ Call error:", error);
        document.getElementById("callingLabel").innerHTML = "Call failed: " + error.message;
        setTimeout(afterTimeOut, 2000);
    });
}


// ----------------------------
// Hangup / Cleanup
// ----------------------------
function hangUp() {
    //debugger;
    if (getCallProvider() === "plivo" && typeof plivoHangUp === "function") {
        plivoHangUp();
        return;
    }

    if (activeCall) {
        //debugger;
        activeCall.disconnect();
        activeCall = null;
    }

    if (device && typeof device.disconnectAll === "function") {
        device.disconnectAll();
    }
}

function afterTimeOut() {
    const modal = document.getElementById("myCallModal");
    if (modal) modal.style.display = "none";
}


// ----------------------------
// Recording trigger (backend)
// ----------------------------
function startCallRecording(callSid, providerOverride) {
    var url = `/User/makecall?sid=${encodeURIComponent(callSid)}&IRFId=${encodeURIComponent(irfIdVar)}&callRecipient=${encodeURIComponent(callRecipientEmail)}&callRecipientPhoneNumber=${encodeURIComponent(callRecipientPhoneNumber)}&recipientType=${encodeURIComponent(recipientType)}`;
    if (providerOverride === "Plivo" || getCallProvider() === "plivo") {
        url += "&provider=Plivo";
    }
    fetch(url)
        .then(() => console.log("🎙️ Recording request sent for:", callSid))
        .catch(err => console.error("Recording request failed:", err));
}


// ----------------------------
// UI / Button Functions
// ----------------------------
function openModalAndCall(name, phone, email, type, irf, interview) {
    callRecipientName = name;
    callRecipientPhoneNumber = phone;
    callRecipientEmail = email || "";
    recipientType = type || "";
    irfIdVar = irf || "";
    interviewId = interview || "";

    const modal = document.getElementById("myCallModal");
    if (modal) modal.style.display = "block";

    document.getElementById("callingLabel").innerHTML = "Calling... <b>" + callRecipientName + "</b>";
    document.getElementById("idCallingTitle").innerHTML = "Call " + (type || "");

    // Make sure Twilio is initialized
    initializeTwilioAfterGesture().then(() => callRecipient(callRecipientPhoneNumber));
}


// ----------------------------
// Close popup
// ----------------------------
function closeCallPopUp() {
    hangUp();
    const modal = document.getElementById("myCallModal");
    if (modal) modal.style.display = "none";
}
function showCallModal(name, type = '') {
    const modal = document.getElementById("myCallModal");
    modal.style.display = "block";
    document.getElementById("callingLabel").innerHTML = "Calling... <b>" + name + "</b>";
    document.getElementById("idCallingTitle").innerHTML = type ? "Call " + type : "Call";
}
function CallButtonClick(candidateObj, indexid) {
    callRecipientName = indexid === 1 ? candidateObj.Name : candidateObj.name;
    callRecipientEmail = indexid === 1 ? candidateObj.EmailId : candidateObj.emailId;
    irfIdVar = indexid === 1 ? candidateObj.IRFId : candidateObj.irfId;
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
    irfIdVar = '';

    showCallModal(callRecipientName);
    callRecipient(callRecipientPhoneNumber);
}

function CommonCallButtonClick(name, mobileNumber, emailId = '', interviewIdParam = null, irfIdParam = null, recipientTypeParam = null) {
    callRecipientName = name;
    if (interviewIdParam) interviewId = interviewIdParam;
    callRecipientPhoneNumber = mobileNumber;
    callRecipientEmail = emailId;
    if (irfIdParam) irfIdVar = irfIdParam;
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

function CallModalButtonClick(emailId, phone, interviewid, type = '') {
    callRecipientName = emailId;
    callRecipientEmail = emailId;
    callRecipientPhoneNumber = phone;
    interviewId = interviewid;
    recipientType = type;

    showCallModal(callRecipientName);
    callRecipient(callRecipientPhoneNumber);
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