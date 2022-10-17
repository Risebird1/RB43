
$.getScript("https://cdn.tiny.cloud/1/no-api-key/tinymce/6/tinymce.min.js");

function closeBroadcastModal() {
    var modal = document.getElementById("broadcastModal");
    tinymce.remove('#bcMessage');
    modal.style.display = "none";
}
function OpenBroadcastModal(irfid, Irfname, interviewId, shouldSendEmail = true) {
    var modalBct = document.getElementById("broadcastModal");
    modalBct.style.display = "block";

    tinymce.init({
        selector: "#bcMessage",
        menubar: false,
        statusbar: false,
        plugins: 'link',
        height: "300",
        toolbar: 'undo redo | bold italic underline strikethrough | fontfamily fontsize blocks | alignleft aligncenter alignright alignjustify | outdent indent |  numlist bullist checklist | forecolor backcolor casechange permanentpen formatpainter removeformat | pagebreak | charmap emoticons | fullscreen  preview save | insertfile image media pageembed template link anchor codesample | a11ycheck ltr rtl | showcomments addcomment',

    });
    $("#hid_irfid_broadcast").val(irfid);
    $("#hid_interviewId_broadcast").val(interviewId);
    $("#hid_shouldSendEmail_broadcast").val(shouldSendEmail);
    //$("#boradcastIrfName").text(Irfname);
    $("#filterModalLabel").text('');
    $("#filterModalLabel").text('Broadcast (' + Irfname + ')');
    return;
}

function SendBroadcast() {
    //var message = document.getElementById("bcMessage").value;
    var message = tinymce.activeEditor.getContent();
    var subject = document.getElementById("bcSubject").value;
    var irfid = document.getElementById("hid_irfid_broadcast").value;
    var interviewId = document.getElementById("hid_interviewId_broadcast").value;
    var shouldSendEmail = document.getElementById("hid_shouldSendEmail_broadcast").value;
    $.ajax({
        type: "POST",
        url: "/IRF/BroadcastIRFmembers",
        data: { 'message': message, 'subject': subject, 'irfid': irfid, 'interviewId': interviewId, 'shouldSendEmail' : shouldSendEmail },
        success: function (data) {
            //window.location.href = '/IRF/Index';
            document.getElementById("broadcastModal").style.display = "none";

            Swal.fire({
                title: data,
                type: 'info',
                //html: ,
                confirmButtonText: "Close",
                allowOutsideClick: false
            });
        }
    });
}