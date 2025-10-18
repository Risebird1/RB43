var member = "";
var modalId = "";
var labelId = "";


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
