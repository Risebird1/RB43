/**
 * Twilio Client configuration for the browser-calls-csharp
 * example application.
 */

// Store some selectors for elements we'll reuse
var callStatus = $("#call-status");
var answerButton = $(".answer-button");
var callSupportButton = $(".call-support-button");
var hangUpButton = $(".hangup-button");
var callCustomerButtons = $(".call-customer-button");

/* Helper function to update the call status bar */
function updateCallStatus(status) {
    callStatus.text(status);
}



