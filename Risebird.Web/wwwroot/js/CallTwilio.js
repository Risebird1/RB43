var member = "";
var modalId = "";
var labelId = "";


/* Callback to let us know Twilio Client is ready */
Twilio.Device.ready(function (device) {

});

function callMember(phoneNumber) {
    var params = { "phoneNumber": phoneNumber };
    if (phoneNumber != null && phoneNumber.length >= 9) {
        Twilio.Device.connect(params);
    }
}
function hangUp() {
    Twilio.Device.disconnectAll();
}

Twilio.Device.connect(function (connection) {
    var appendText = "In call with " + "<b>" + member + "</b>";
    document.getElementById(labelId).innerHTML = appendText;
});

function afterTimeOut() {
    var modal = document.getElementById(modalId);
    modal.style.display = "none";
}

Twilio.Device.disconnect(function (connection) {
    var appendText = "Call disconnected! ";
    document.getElementById(labelId).innerHTML = appendText;
    setTimeout(afterTimeOut, 2000);

});

function CallButtonClick(name, phone, modalElementId, labelElementId) {
    member = name;
    modalId = modalElementId;
    labelId = labelElementId;
    // Get the modal
    var modal = document.getElementById(modalId);
    // When the user clicks the button, open the modal
    modal.style.display = "block";
    var appendText = "Calling... " + "<b>" + member + "</b>";
    document.getElementById(labelId).innerHTML = appendText;
    callMember(phone);
}
function closeCallPopUp() {
    hangUp();
    var modal = document.getElementById(modalId);
    modal.style.display = "none";
}
