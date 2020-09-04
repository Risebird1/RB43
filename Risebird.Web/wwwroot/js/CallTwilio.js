var member = "";
var modalId = "";
var headerId = "";


/* Callback to let us know Twilio Client is ready */
Twilio.Device.ready(function (device) {

});

function callMember(phoneNumber) {
    var params = { "phoneNumber": phoneNumber };
    Twilio.Device.connect(params);
}
function hangUp() {
    Twilio.Device.disconnectAll();
}

Twilio.Device.connect(function (connection) {
    var appendText = "In call with " + "<b>" + member + "</b>";
    document.getElementById(headerId).innerHTML = appendText;
});

function afterTimeOut() {
    var modal = document.getElementById(modalId);
    modal.style.display = "none";
}

Twilio.Device.disconnect(function (connection) {
    var appendText = "Call disconnected! ";
    document.getElementById(headerId).innerHTML = appendText;
    setTimeout(afterTimeOut, 2000);

});

function CallButtonClick(name, phone, modalElementId, headerElementId) {
    member = name;
    modalId = modalElementId;
    headerId = headerElementId;
    // Get the modal
    var modal = document.getElementById(modalId);
    // When the user clicks the button, open the modal
    modal.style.display = "block";
    var appendText = "Calling... " + "<b>" + member + "</b>";
    document.getElementById(headerId).innerHTML = appendText;
    callMember(phone);
}
function closeCallPopUp() {
    hangUp();
    var modal = document.getElementById(modalId);
    modal.style.display = "none";
}
