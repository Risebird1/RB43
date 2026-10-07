// ----------------------------
// Global variables
// ----------------------------
let device;       // Twilio.Device instance
let activeCall;   // Current call instance

let callRecipientName = "";
let irfIdVar = "";
let callRecipientEmail = "";
let callRecipientPhoneNumber = "";
let recipientType = '';
let interviewId = '';


// ----------------------------
// Call provider (Twilio / Plivo)
// ----------------------------
// window.rbPlivoConfigured / window.rbDefaultProvider are emitted by TwilioCallModalPartial.cshtml.
// plivoCallRecipient() / plivoHangUp() come from PlivoCallMaker.js (loaded only when Plivo is configured).
let isSwitchingProvider = false;

function getCallProvider() {
    let stored = null;
    try { stored = localStorage.getItem('rb_call_provider'); } catch (e) { }

    if (stored === 'twilio') return 'twilio';
    if (stored === 'plivo' && window.rbPlivoConfigured !== false) return 'plivo';
    return window.rbDefaultProvider === 'plivo' ? 'plivo' : 'twilio';
}

function highlightCallProvider() {
    const current = getCallProvider();
    document.querySelectorAll('.rb-call-provider-btn').forEach(function (btn) {
        const active = btn.getAttribute('data-provider') === current;
        btn.classList.toggle('btn-primary', active);
        btn.classList.toggle('btn-default', !active);
    });
}

function setCallProvider(provider) {
    if (provider !== 'plivo' && provider !== 'twilio') return;
    if (provider === 'plivo' && window.rbPlivoConfigured !== true) return;

    const previous = getCallProvider();
    try { localStorage.setItem('rb_call_provider', provider); } catch (e) { }
    highlightCallProvider();

    // Mid-call: hang up and redial the same number on the new provider.
    const modal = document.getElementById("myCallModal");
    if (previous !== provider && callRecipientPhoneNumber && modal && modal.style.display === "block") {
        const number = callRecipientPhoneNumber;
        isSwitchingProvider = true;
        hangUp();
        setTimeout(function () {
            document.getElementById("callingLabel").innerHTML = "Calling... <b>" + callRecipientName + "</b>";
            callRecipient(number);
        }, 800);
        setTimeout(function () { isSwitchingProvider = false; }, 3000);
    }
}

document.addEventListener("DOMContentLoaded", highlightCallProvider);


// ----------------------------
// Initialization only after user gesture
// ----------------------------
let isDeviceInitialized = false;

async function initializeTwilioAfterGesture() {
    if (isDeviceInitialized) return; // already initialized
    console.log("Initializing Twilio Device...");

    await GenerateToken();
    // Only count it as done when the device really exists, so a failed start (e.g. SDK not loaded) can be retried.
    isDeviceInitialized = !!device;
    console.log(isDeviceInitialized ? "Twilio Device ready." : "Twilio Device not ready.");
}

// The Twilio browser SDK is loaded by _Layout, but pages on _PublicLayout / _PublicLayout2 (e.g. IRF/DeliveryHeadInstruction)
// do not include it, so load it on demand here, the same way PlivoCallMaker.js loads the Plivo SDK.
const TWILIO_SDK_URL = "/lib/twilio.min.js";
let twilioSdkPromise = null;

function ensureTwilioSdk() {
    if (typeof Twilio !== "undefined") return Promise.resolve();
    if (!twilioSdkPromise) {
        twilioSdkPromise = new Promise((resolve, reject) => {
            const script = document.createElement("script");
            script.src = TWILIO_SDK_URL;
            script.onload = () => resolve();
            script.onerror = () => {
                twilioSdkPromise = null; // allow a retry on the next call
                reject(new Error("Could not load the Twilio SDK"));
            };
            document.head.appendChild(script);
        });
    }
    return twilioSdkPromise;
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

        if (!device) {
            await ensureTwilioSdk();
            setupDevice(data.token);
        } else {
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
    if (typeof Twilio === "undefined" || !token) {
        console.warn("Twilio SDK or token not available.");
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
    highlightCallProvider();
    if (getCallProvider() === 'plivo' && typeof plivoCallRecipient === 'function') {
        callRecipientPhoneNumber = phoneNumber;
        plivoCallRecipient(phoneNumber);
        return;
    }

    if (!device) {
        // Not started yet (or the start failed): start it now, then dial once. Previously the call was silently dropped.
        isDeviceInitialized = false;
        initializeTwilioAfterGesture().then(() => {
            if (device) {
                callRecipient(phoneNumber);
            } else {
                console.error("Twilio device could not be started.");
                const label = document.getElementById("callingLabel");
                if (label) label.innerHTML = "Call failed: calling is not available right now. Please try again.";
            }
        });
        return;
    }

    callRecipientPhoneNumber = phoneNumber;

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
        const text = "In call with <b>" + callRecipientName + "</b>";
        document.getElementById("callingLabel").innerHTML = text;
        startCallRecording(call.parameters.CallSid);
    });

    call.on("disconnect", () => {
        if (isSwitchingProvider) return; // redialing on Plivo
        document.getElementById("callingLabel").innerHTML = "Call disconnected!";
        setTimeout(afterTimeOut, 2000);
    });

    call.on("cancel", () => {
        document.getElementById("callingLabel").innerHTML = "Call cancelled!";
        setTimeout(afterTimeOut, 2000);
    });

    call.on("error", (error) => {
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
    if (typeof plivoHangUp === 'function') {
        plivoHangUp();
    }

    if (activeCall) {
        //debugger;
        activeCall.disconnect();
        activeCall = null;
    }
    
    if (device) {
        device.disconnectAll();
    }
}

function afterTimeOut() {
    if (isSwitchingProvider) return; // keep the modal open while redialing on the other provider
    const modal = document.getElementById("myCallModal");
    if (modal) modal.style.display = "none";
}


// ----------------------------
// Recording trigger (backend)
// ----------------------------
function startCallRecording(callSid) {
    fetch(`/User/makecall?sid=${encodeURIComponent(callSid)}&IRFId=${encodeURIComponent(irfIdVar)}&callRecipient=${encodeURIComponent(callRecipientEmail)}&callRecipientPhoneNumber=${encodeURIComponent(callRecipientPhoneNumber)}&recipientType=${encodeURIComponent(recipientType)}`)
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
    // Default state: no edit step, Hang Up visible. The editable flow re-shows the row.
    var _row = document.getElementById("rbEditNumberRow");
    if (_row) _row.style.display = "none";
    var _hu = document.getElementById("hangUpButton");
    if (_hu) _hu.style.display = "block";
}

// Normalise to digits only and toggle the Indian country code (91) prefix.
function rbToggle91() {
    var el = document.getElementById("rbEditNumber");
    if (!el) return;
    var n = String(el.value || "").replace(/[^\d]/g, "");
    if (n.indexOf("91") === 0 && n.length > 10) {
        n = n.substring(2);          // remove 91
    } else if (n.length === 10) {
        n = "91" + n;                // add 91 to a bare 10-digit number
    }
    el.value = n;
}

// Call button with an edit-the-number step: opens the modal with the number
// pre-filled and editable, and dials only when the agent presses Call.
function CommonCallButtonClickEditable(name, mobileNumber, emailId = '', interviewIdParam = null, irfIdParam = null, recipientTypeParam = null) {
    callRecipientName = name;
    if (interviewIdParam) interviewId = interviewIdParam;
    callRecipientEmail = emailId;
    if (irfIdParam) irfIdVar = irfIdParam;
    if (recipientTypeParam) recipientType = recipientTypeParam;
    // Clear the previous call's number: otherwise picking Plivo/Twilio before pressing
    // Call makes setCallProvider() redial that old number, skipping the edit step.
    callRecipientPhoneNumber = "";

    showCallModal(name, recipientTypeParam || '');
    document.getElementById("callingLabel").innerHTML = "Call <b>" + (name || '') + "</b>";
    var el = document.getElementById("rbEditNumber");
    if (el) el.value = String(mobileNumber || "").replace(/[^\d]/g, "");
    var row = document.getElementById("rbEditNumberRow");
    if (row) row.style.display = "block";
    var hu = document.getElementById("hangUpButton");
    if (hu) hu.style.display = "none";   // nothing to hang up until the call is placed
}

function rbPlaceEditedCall() {
    var el = document.getElementById("rbEditNumber");
    var num = el ? String(el.value || "").replace(/[^\d]/g, "") : callRecipientPhoneNumber;
    if (!num || num.length < 9) { alert("Please enter a valid phone number."); return; }
    // Twilio's <Dial><Number> needs E.164 ("+91..."): without the "+" it reads the number as
    // national to the US caller ID and the call never connects. Plivo strips the "+" itself.
    if (num.length > 10) num = "+" + num;
    callRecipientPhoneNumber = num;
    var row = document.getElementById("rbEditNumberRow");
    if (row) row.style.display = "none";
    var hu = document.getElementById("hangUpButton");
    if (hu) hu.style.display = "block";
    document.getElementById("callingLabel").innerHTML = "Calling... <b>" + (callRecipientName || '') + "</b>";
    callRecipient(num);
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

// Same as CommonCallButtonClickEditable, reading the values from the element's data- attributes
// (data-name, data-mobile, data-email, data-interview, data-irf, data-type) so quotes in names cannot break the click.
function CommonCallButtonClickFromData(el) {
    const d = (el && el.dataset) || {};
    CommonCallButtonClickEditable(d.name || '', d.mobile || '', d.email || '', d.interview || null, d.irf || null, d.type || null);
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