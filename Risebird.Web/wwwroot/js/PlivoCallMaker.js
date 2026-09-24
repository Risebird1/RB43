// ----------------------------
// PlivoCallMaker.js — Plivo browser calling for the shared call modal.
// Loaded by TwilioCallModalPartial.cshtml only when PlivoSettings is configured.
// TwilioCallMaker.js dispatches callRecipient()/hangUp() here when the provider is 'plivo'.
// Uses the modal globals from TwilioCallMaker.js (callRecipientName, callRecipientEmail, irfIdVar, ...).
// ----------------------------
(function () {
    if (window.plivoCallRecipient) return; // partial can be rendered twice on a page

    const PLIVO_SDK_URL = "https://cdn.plivo.com/sdk/browser/v2/plivo.min.js";
    const TOKEN_REFRESH_MS = 55 * 60 * 1000; // token is issued for 1 hour

    let plivoSdk = null;
    let plivoLoginPromise = null;
    let plivoCallActive = false;

    // ----------------------------
    // SDK load + login
    // ----------------------------
    function loadPlivoSdk() {
        return new Promise(function (resolve, reject) {
            if (window.Plivo) return resolve();
            const script = document.createElement("script");
            script.src = PLIVO_SDK_URL;
            script.onload = function () { resolve(); };
            script.onerror = function () { reject(new Error("Could not load the Plivo SDK")); };
            document.head.appendChild(script);
        });
    }

    async function fetchPlivoToken() {
        const response = await fetch("/PlivoCalls/Generate", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ page: window.location.pathname })
        });
        if (!response.ok) throw new Error("Token request failed: " + response.status);
        const data = await response.json();
        if (!data.token) throw new Error("No Plivo token returned");
        return data.token;
    }

    function plivoLogin() {
        plivoLoginPromise = (async function () {
            await loadPlivoSdk();
            if (!plivoSdk) {
                plivoSdk = new window.Plivo({ debug: "ERROR", permOnClick: true, enableTracking: true, closeProtection: false });
                setupPlivoHandlers(plivoSdk.client);
            }
            const token = await fetchPlivoToken();

            await new Promise(function (resolve, reject) {
                let done = false;
                plivoSdk.client.on("onLogin", function () { if (!done) { done = true; resolve(); } });
                plivoSdk.client.on("onLoginFailed", function (cause) { if (!done) { done = true; reject(new Error("Plivo login failed: " + cause)); } });
                plivoSdk.client.loginWithAccessToken(token);
            });

            setTimeout(refreshPlivoLogin, TOKEN_REFRESH_MS);
        })();

        const current = plivoLoginPromise;
        current.catch(function (err) {
            console.error("Plivo login error:", err);
            if (plivoLoginPromise === current) plivoLoginPromise = null; // retry on the next call
        });
        return current;
    }

    function refreshPlivoLogin() {
        if (plivoCallActive) {
            setTimeout(refreshPlivoLogin, 60000); // try again after the call
            return;
        }
        try { plivoSdk.client.logout(); } catch (e) { }
        plivoLogin();
    }

    // ----------------------------
    // Events → call modal
    // ----------------------------
    function setupPlivoHandlers(client) {
        client.on("onLogin", function () { console.log("✅ Plivo client logged in."); });
        client.on("onLoginFailed", function (cause) { console.error("❌ onLoginFailed:", cause); });

        client.on("onCallRemoteRinging", function () {
            console.log("☎️ Plivo call ringing...");
        });

        client.on("onCallAnswered", function (callInfo) {
            document.getElementById("callingLabel").innerHTML = "In call with <b>" + callRecipientName + "</b>";
            const callUuid = (callInfo && callInfo.callUUID) || (client.getCallUUID ? client.getCallUUID() : null);
            logPlivoCall(callUuid);
        });

        client.on("onCallTerminated", function () {
            plivoCallActive = false;
            if (typeof isSwitchingProvider !== "undefined" && isSwitchingProvider) return; // redialing on Twilio
            document.getElementById("callingLabel").innerHTML = "Call disconnected!";
            setTimeout(afterTimeOut, 2000);
        });

        client.on("onCallFailed", function (cause) {
            plivoCallActive = false;
            console.error("⚠️ Plivo call failed:", cause);
            document.getElementById("callingLabel").innerHTML = "Call failed: " + cause;
            setTimeout(afterTimeOut, 2000);
        });
    }

    // ----------------------------
    // Outgoing call / hang up
    // ----------------------------
    async function plivoCallRecipient(phoneNumber) {
        // Plivo expects digits only (no +, spaces or dashes), also in X-PH- header values.
        const phone = String(phoneNumber || "").replace(/[^\d]/g, "");
        if (phone.length < 9) {
            console.warn("Invalid phone number");
            return;
        }

        try {
            await (plivoLoginPromise || plivoLogin());
            console.log("Dialing via Plivo:", phone);
            plivoCallActive = true;
            plivoSdk.client.call(phone, { "X-PH-To": phone });
        } catch (err) {
            plivoCallActive = false;
            document.getElementById("callingLabel").innerHTML = "Call failed: " + err.message;
            setTimeout(afterTimeOut, 2000);
        }
    }

    function plivoHangUp() {
        if (plivoSdk && plivoCallActive) {
            try { plivoSdk.client.hangup(); } catch (e) { console.warn("Plivo hangup error:", e); }
        }
        plivoCallActive = false;
    }

    // ----------------------------
    // Call log (backend) — same endpoint as Twilio, Twilio recording API skipped for Plivo
    // ----------------------------
    function logPlivoCall(callUuid) {
        if (!callUuid) return;
        fetch(`/User/makecall?sid=${encodeURIComponent(callUuid)}&provider=Plivo&IRFId=${encodeURIComponent(irfIdVar)}&callRecipient=${encodeURIComponent(callRecipientEmail)}&callRecipientPhoneNumber=${encodeURIComponent(callRecipientPhoneNumber)}&recipientType=${encodeURIComponent(recipientType)}`)
            .then(() => console.log("📝 Plivo call logged:", callUuid))
            .catch(err => console.error("Plivo call log failed:", err));
    }

    window.plivoCallRecipient = plivoCallRecipient;
    window.plivoHangUp = plivoHangUp;

    // Page load: load SDK and log in so the first call connects quickly.
    plivoLogin();
})();
