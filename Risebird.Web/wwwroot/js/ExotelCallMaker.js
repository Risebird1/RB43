// ----------------------------
// ExotelCallMaker.js — Exotel calling for the shared call modal.
// Loaded by TwilioCallModalPartial.cshtml only when the Exotel section is configured.
// TwilioCallMaker.js dispatches callRecipient()/hangUp() here when the provider is 'exotel'.
// Uses the modal globals from TwilioCallMaker.js (callRecipientName, callRecipientEmail, irfIdVar, ...).
//
// Every Exotel call is placed server-side (User/ExotelConnect → Exotel Calls/connect): Exotel rings the
// agent first, then dials the recipient and bridges. The Exotel WebSDK has no outbound dial of its own.
//   • Softphone (window.rbExotelWebRtc + this agent has a SIP subscriber): Exotel rings this browser,
//     which answers automatically — the agent talks in the browser, like Twilio/Plivo.
//   • Click-to-call (otherwise): Exotel rings the agent's own phone (UserList mobile number).
// ----------------------------
(function () {
    if (window.exotelCallRecipient) return; // partial can be rendered twice on a page

    const REGISTER_TIMEOUT_MS = 15000;
    const ANSWER_TIMEOUT_MS = 60000; // how long to wait for Exotel to ring the softphone

    let webClient = null;
    let registerPromise = null;
    let registered = false;
    let expectingCall = false;   // we asked Exotel to ring this browser; answer the next incoming call
    let expectTimer = null;
    let callActive = false;
    let placing = false;

    function setLabel(html) {
        const label = document.getElementById("callingLabel");
        if (label) label.innerHTML = html;
    }

    function escapeHtml(value) {
        return String(value || "").replace(/[&<>"']/g, function (c) {
            return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c];
        });
    }

    function endWithMessage(message) {
        setLabel(message);
        setTimeout(afterTimeOut, 2000);
    }

    // ----------------------------
    // Softphone (Exotel Voice WebSDK) — register the agent's SIP subscriber
    // ----------------------------
    function softphoneAvailable() {
        return window.rbExotelWebRtc === true && window.exotelSDK && window.exotelSDK.ExotelWebClient;
    }

    async function fetchSipAccount() {
        const response = await fetch("/TwilioCalls/ExotelToken", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: "{}"
        });
        if (!response.ok) throw new Error("Exotel token request failed: " + response.status);
        const data = await response.json();
        return data.sip; // null → no softphone for this agent
    }

    function exotelRegister() {
        registerPromise = (async function () {
            const sip = await fetchSipAccount();
            if (!sip) return false;

            webClient = new window.exotelSDK.ExotelWebClient();
            await new Promise(function (resolve, reject) {
                let done = false;
                const timer = setTimeout(function () {
                    if (!done) { done = true; reject(new Error("Exotel registration timed out")); }
                }, REGISTER_TIMEOUT_MS);

                function onRegister(state) {
                    if (state === "registered") {
                        registered = true;
                        console.log("✅ Exotel softphone registered.");
                        if (!done) { done = true; clearTimeout(timer); resolve(); }
                    } else if (state === "unregistered" || state === "terminated") {
                        registered = false;
                        if (!done) { done = true; clearTimeout(timer); reject(new Error("Exotel registration failed")); }
                    }
                }

                webClient.initWebrtc({
                    userName: sip.userName,
                    authUser: sip.userName,
                    sipdomain: sip.sipDomain,
                    domain: sip.domain,
                    displayname: sip.displayName,
                    secret: sip.secret,
                    port: sip.port,
                    security: "wss"
                }, onRegister, onCallEvent, function () { /* multi-tab session events: not used */ });
                webClient.DoRegister();
            });
            return true;
        })();

        const current = registerPromise;
        current.catch(function (err) {
            console.error("Exotel softphone error:", err);
            if (registerPromise === current) registerPromise = null; // retry on the next call
        });
        return current;
    }

    function clearExpecting() {
        expectingCall = false;
        if (expectTimer) { clearTimeout(expectTimer); expectTimer = null; }
    }

    function onCallEvent(callObj, eventType) {
        if (eventType === "incoming") {
            if (!expectingCall) {
                console.log("📞 Exotel incoming call ignored (not placed from this modal).");
                return;
            }
            clearExpecting();
            try { webClient.getCall().Answer(); } catch (e) { console.warn("Exotel answer error:", e); }
        } else if (eventType === "connected") {
            callActive = true;
            setLabel("In call with <b>" + escapeHtml(callRecipientName) + "</b>");
        } else if (eventType === "callEnded" || eventType === "terminated") {
            const wasActive = callActive;
            callActive = false;
            clearExpecting();
            if (typeof isSwitchingProvider !== "undefined" && isSwitchingProvider) return; // redialing on another provider
            if (wasActive || eventType === "terminated") endWithMessage("Call disconnected!");
        }
    }

    // ----------------------------
    // Outgoing call / hang up
    // ----------------------------
    async function exotelCallRecipient(phoneNumber) {
        const phone = String(phoneNumber || "").trim();
        if (phone.replace(/[^\d]/g, "").length < 9) {
            console.warn("Invalid phone number");
            return;
        }
        if (placing) return;
        placing = true;

        try {
            let useSoftphone = false;
            if (softphoneAvailable()) {
                try { useSoftphone = await (registerPromise || exotelRegister()); }
                catch (e) { useSoftphone = false; } // fall back to click-to-call
                useSoftphone = useSoftphone && registered;
            }

            if (useSoftphone) {
                expectingCall = true;
                expectTimer = setTimeout(function () {
                    if (expectingCall) { clearExpecting(); endWithMessage("Call failed: Exotel did not connect"); }
                }, ANSWER_TIMEOUT_MS);
            }

            console.log("📞 Exotel: calling", phone, useSoftphone ? "(softphone)" : "(rings your phone first)");
            const numericInterviewId = parseInt(typeof interviewId !== "undefined" ? interviewId : "", 10);
            const response = await fetch("/User/ExotelConnect", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                // Pages pass ids as numbers (e.g. IRFId from the candidate JSON); the server expects strings,
                // and a number in a string field makes the whole JSON body fail to bind.
                body: JSON.stringify({
                    phoneNumber: phone,
                    interviewId: isNaN(numericInterviewId) ? null : numericInterviewId,
                    callRecipient: callRecipientEmail ? String(callRecipientEmail) : null,
                    irfId: irfIdVar ? String(irfIdVar) : null,
                    recipientType: recipientType ? String(recipientType) : null,
                    mode: useSoftphone ? "webrtc" : "phone"
                })
            });
            const data = response.ok ? await response.json() : { success: false, error: "Request failed: " + response.status };

            if (!data.success) {
                clearExpecting();
                endWithMessage("Call failed: " + escapeHtml(data.error || "unknown error"));
                return;
            }

            console.log("📝 Exotel call placed:", data.sid, data.mode);
            if (data.mode === "webrtc") {
                setLabel("Connecting... <b>" + escapeHtml(callRecipientName) + "</b>");
            } else {
                clearExpecting();
                setLabel("Exotel is calling your phone — answer it to be connected to <b>" + escapeHtml(callRecipientName) +
                    "</b>.<br><small>End the call from your phone.</small>");
            }
        } catch (err) {
            clearExpecting();
            endWithMessage("Call failed: " + escapeHtml(err.message));
        } finally {
            placing = false;
        }
    }

    function exotelHangUp() {
        clearExpecting();
        if (webClient && callActive) {
            try { webClient.getCall().Hangup(); } catch (e) { console.warn("Exotel hangup error:", e); }
        }
        callActive = false;
        // Click-to-call runs on the agent's phone; it is ended there.
    }

    window.exotelCallRecipient = exotelCallRecipient;
    window.exotelHangUp = exotelHangUp;

    // Page load: register the softphone so the first call connects quickly.
    if (softphoneAvailable()) exotelRegister();
})();
