// NOTE: `var` (not `let`) and the self-assigning initializers below are deliberate.
// Some views render the call modal partial more than once (e.g. Candidate/InterviewDetails),
// which loads this file twice. `let` would throw "Identifier has already been declared"
// and kill the second load; `var x = x || default` keeps the already-established state.
var plivoSdk = plivoSdk || null;
var plivoReady = plivoReady || false;
var plivoRefreshTimer = plivoRefreshTimer || null;
var plivoAnsweredUuid = plivoAnsweredUuid || null;

function plivoIsConfigured() {
    return window.rbPlivoConfigured === true;
}

function plivoIsInCall() {
    return window.rbCallInProgress === true && typeof getCallProvider === "function" && getCallProvider() === "plivo";
}

// Plivo expects the destination in E.164 *without* the leading '+' (e.g. 919876543210).
// Call buttons pass the number straight from the DB, which is usually a bare 10-digit
// Indian mobile, so normalize here. Mirrors PhoneNumberHelper.TrySanitizeIndianPhoneNumber.
// Returns null when the number cannot be dialled.
function plivoNormalizeNumber(raw) {
    if (!raw) {
        return null;
    }

    var trimmed = String(raw).trim();
    var digits = trimmed.replace(/\D/g, "").replace(/^00/, "");
    if (!digits) {
        return null;
    }

    // Already carries an explicit non-Indian country code: it is E.164 already,
    // so just hand back the digits.
    if (trimmed.charAt(0) === "+" && digits.indexOf("91") !== 0) {
        return digits.length >= 8 ? digits : null;
    }

    // Strip the country code ("91"/"0091") or a leading trunk '0' so we are left
    // with the bare 10-digit national number.
    if (digits.length > 10) {
        digits = digits.slice(-10);
    }

    // Indian mobile numbers are 10 digits starting with 6-9.
    if (digits.length !== 10 || digits.charAt(0) < "6") {
        return null;
    }

    return "91" + digits;
}

async function generatePlivoToken() {
    const response = await fetch("/PlivoCalls/Generate", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ page: window.location.pathname })
    });
    if (!response.ok) {
        var detail = "";
        try {
            const problem = await response.json();
            detail = problem && problem.error ? " - " + problem.error : "";
        } catch (e) { }
        throw new Error("Plivo token request failed: " + response.status + detail);
    }
    return response.json();
}

function bindPlivoClientEvents(client) {
    client.on("onLogin", function () {
        plivoReady = true;
        console.log("Plivo endpoint logged in.");
    });

    client.on("onLoginFailed", function (reason) {
        plivoReady = false;
        console.error("Plivo login failed:", reason);
    });

    client.on("onCalling", function () {
        window.rbCallInProgress = true;
        const label = document.getElementById("callingLabel");
        if (label) {
            label.innerHTML = "Calling... <b>" + (typeof callRecipientName === "string" ? callRecipientName : "") + "</b>";
        }
    });

    client.on("onCallRemoteRinging", function () {
        const label = document.getElementById("callingLabel");
        if (label) {
            label.innerHTML = "Ringing... <b>" + (typeof callRecipientName === "string" ? callRecipientName : "") + "</b>";
        }
    });

    client.on("onCallAnswered", function (callInfo) {
        window.rbCallInProgress = true;
        plivoAnsweredUuid = (callInfo && (callInfo.callUUID || callInfo.callUuid || callInfo.callId)) || "";
        const label = document.getElementById("callingLabel");
        if (label) {
            label.innerHTML = "In call with <b>" + (typeof callRecipientName === "string" ? callRecipientName : "") + "</b>";
        }
        if (typeof startCallRecording === "function") {
            startCallRecording(plivoAnsweredUuid, "Plivo");
        }
    });

    client.on("onCallTerminated", function () {
        window.rbCallInProgress = false;
        plivoAnsweredUuid = null;
        if (window.rbProviderSwitching) {
            window.rbProviderSwitching = false;
            return;
        }
        const label = document.getElementById("callingLabel");
        if (label) {
            label.innerHTML = "Call disconnected!";
        }
        if (typeof afterTimeOut === "function") {
            setTimeout(afterTimeOut, 2000);
        }
    });

    client.on("onCallFailed", function (reason) {
        window.rbCallInProgress = false;
        plivoAnsweredUuid = null;
        if (window.rbProviderSwitching) {
            window.rbProviderSwitching = false;
            return;
        }
        const label = document.getElementById("callingLabel");
        if (label) {
            label.innerHTML = "Call failed: " + (reason || "unknown");
        }
        if (typeof afterTimeOut === "function") {
            setTimeout(afterTimeOut, 2000);
        }
    });
}

async function initializePlivo() {
    if (!plivoIsConfigured()) {
        return;
    }
    if (typeof window.Plivo !== "function") {
        console.error("Plivo Browser SDK not loaded.");
        return;
    }

    try {
        const data = await generatePlivoToken();
        var token = data && (data.token || data.Token);
        if (!token) {
            console.error("Plivo login failed: no token from /PlivoCalls/Generate", data);
            return;
        }
        if (!plivoSdk) {
            plivoSdk = new window.Plivo({
                debug: "ERROR",
                permOnClick: true,
                enableTracking: false
            });
            bindPlivoClientEvents(plivoSdk.client);
        }
        plivoSdk.client.loginWithAccessToken(token);
        if (plivoRefreshTimer) {
            clearTimeout(plivoRefreshTimer);
        }
        plivoRefreshTimer = setTimeout(initializePlivo, 240000);
    } catch (err) {
        console.error("Error initializing Plivo:", err);
    }
}

function plivoCallRecipient(phoneNumber) {
    if (!plivoIsConfigured()) {
        console.error("Plivo is not configured.");
        return;
    }
    if (!plivoSdk || !plivoSdk.client || !plivoReady) {
        console.error("Plivo client not ready. Wait for login.");
        initializePlivo();
        return;
    }

    var destination = plivoNormalizeNumber(phoneNumber);
    if (!destination) {
        console.error("Plivo: cannot dial invalid phone number:", phoneNumber);
        const label = document.getElementById("callingLabel");
        if (label) {
            label.innerHTML = "Invalid phone number";
        }
        window.rbCallInProgress = false;
        return;
    }

    console.log("Dialing via Plivo:", destination);
    window.rbCallInProgress = true;
    // Plivo only allows alphanumeric custom-header values, so send the normalized digits.
    plivoSdk.client.call(destination, { "X-PH-To": destination });
}

function plivoHangUp() {
    if (plivoSdk && plivoSdk.client && typeof plivoSdk.client.hangup === "function") {
        try {
            plivoSdk.client.hangup();
        } catch (err) {
            console.error("Plivo hangup failed:", err);
        }
    }
    window.rbCallInProgress = false;
}

// Guarded so a second load of this file does not start a second login.
if (!window.rbPlivoInitStarted) {
    window.rbPlivoInitStarted = true;
    document.addEventListener("DOMContentLoaded", initializePlivo);
    if (document.readyState !== "loading") {
        initializePlivo();
    }
}
