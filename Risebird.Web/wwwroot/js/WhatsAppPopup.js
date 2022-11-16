

    function closeClientwhatsappModel() {
        var model = document.getElementById("ClientwhatsappModel");
        model.style.display = "none";
    }
    function openClientwhatsappModel() {
        var model = document.getElementById("ClientwhatsappModel");
        model.style.display = "block";
    }
    function SubmitWhatsapp() {
        if ($("#clttxtwhatsappnumber").val().trim() != "") {

        $.ajax({
            type: "POST",
            url: "/Dashboard/SaveWhatsappNumber",
            data: { whatsappnumber: $("#clttxtwhatsappnumber").val().trim() },
            success: function (data) {

                swal.fire(data);
                closeClientwhatsappModel();
            }
        });
        }
    }
