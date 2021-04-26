

/*--------------------Interview page-------------------*/

/*--------------------Interview page Ends-------------------*/




/*--------------------Interview Edit page-------------------*/

$(document).ready(function () {
    var inputs = document.getElementsByTagName("input");
    for (i = 0; i <= inputs.length; i++) {
        inputs[i].addEventListener("input", function () {
            document.getElementById("btnUpload").disabled = false;
        });
    }
});






function RadioChanged(val) {
    if (val == 'Upload') {
        document.getElementById("uploadDiv").style.display = "block";
        document.getElementById("linkDiv").style.display = "none";
        //document.getElementById("resumeLink").value = '';

    }

    else if (val == 'Link') {
        document.getElementById("uploadDiv").style.display = "none";
        document.getElementById("linkDiv").style.display = "block";
        document.getElementById("File").value = '';

    }
    else {
        document.getElementById("uploadDiv").style.display = "none";
        document.getElementById("linkDiv").style.display = "none";
        document.getElementById("File").value = '';
        //document.getElementById("resumeLink").value = '';
    }

}


function IRFChanged(val) {

    $.ajax({
        type: "GET",
        url: "/Interview/GetClientsByRoles",
        data: { 'val': val },
        success: function (data) {

            var drpdownClientHR = $("#ClientHrContact");
            var drpdownStaffingAgency = $("#staffingAgency");
            $("#ClientHrContact option").remove();
            $("#staffingAgency option").remove();

            //check if the data array contains atleast one of each roles
            var arr = [];

            for (i = 0; i < data.length; i++) {
                arr.push(data[i].value);
            }
            var CH = arr.indexOf("1");
            var SA = arr.indexOf("4");

            if (CH == (-1)) {
                var opt = $("<option />");

                //Set Text part.
                opt.html("No Client HR");

                //Set Value part.
                opt.val("");
                drpdownClientHR.append(opt);
            }
            if (SA == (-1)) {
                var optn = $("<option />");

                //Set Text part.
                optn.html("No Staffing Agency");

                //Set Value part.
                optn.val("");
                drpdownStaffingAgency.append(optn);
            }

            if (CH == (-1) && SA == (-1)) {
                return;
            }
            if (data.length > 0) {

                $(data).each(function () {
                    var option = $("<option />");

                    //Set Text part.
                    option.html(this.text);

                    //Set Value part.
                    option.val(this.text);

                    if (this.value != null && this.value == 1) {
                        drpdownClientHR.append(option);
                    }
                    else if (this.value != null && this.value == 4) {
                        drpdownStaffingAgency.append(option);
                    }

                });


            }


        }
    });

}

/*--------------------Interview Edit page Ends-------------------*/

