function AddRow() {
    //document.getElementById("btnUpload").disabled = false;

    var table = document.getElementById("SkillTable");
    var rowCount = table.rows.length;


    var $ttc = $("#SkillTable").find("tbody tr:last");
    var $tr = $ttc.clone();
    var trIndexReplaced = $("#SkillTable tr").length - 1;

    $tr.find("input,select,hidden").attr("name", function () { // find name in the cloned row
        var parts = this.id.match(/(\D+)_(\d+)__(\D+)$/); // extract parts from id, including index
        return parts[1] + "[" + ++parts[2] + "]." + parts[3]; // build new name
    }).attr("id", function () { // change id also
        var parts = this.id.match(/(\D+)_(\d+)__(\D+)$/); // extract parts
        return parts[1] + "_" + ++parts[2] + "__" + parts[3]; // build new id
    });
    $tr.find("span[data-valmsg-for]").attr("data-valmsg-for", function () { // find validation message
        var parts = $(this).attr("data-valmsg-for").match(/(\D+)\[(\d+)]\.(\D+)$/); // extract parts from the referring attribute
        return parts[1] + "[" + ++parts[2] + "]." + parts[3]; // build new value
    })



    // reset fields in the new row
    $tr.find("select").val("");
    $tr.find("input[type=text]").val("");
    $tr.find("input[type=hidden]").val("");

    $tr.find("a").val("");

    // add cloned row as last row
    $("#SkillTable").find("tbody tr:last").after($tr);

    // Find the affected form
    var $form = $tr.closest("FORM");

    // Unbind existing validation
    $form.unbind();
    $form.data("validator", null);

    // Check document for changes
    //$.validator.unobtrusive.parse(document);
    // document.getElementById("btnUpload").disabled = false;

}

function DeleteRowSkills(element, collectionName, tdOne, tdTwo, tdThree, tdFour, tdFive, tdSix, tdSeven) {

    if ($(element).parents('tbody').find('tr').length === 1) {

        alert('Must have atleast one Row');
        return false;
    }

    else {
        //document.getElementById("btnUpload").disabled = false;

        //var currentvalue = $(this).attr('value');
        var closestRow = $(element).closest('tr');
        var currentIndex = closestRow[0].sectionRowIndex;
        var totalRows = $(element).parents('tbody').find('tr').length;
        var maxIndex = totalRows - 1;
        if (currentIndex == maxIndex) {
            $(element).closest('tr').remove();
        }
        //ClientCollection_1__ClientRoleId
        else {
            $(element).closest('tr').remove();
            var startIndex = currentIndex + 1;
            var endIndex = maxIndex + 1;
            for (i = startIndex; i < endIndex; i++) {
                var elem1 = document.getElementById(collectionName + "_" + i + "__" + tdOne);
                elem1.id = collectionName + "_" + (i - 1) + "__" + tdOne;
                //name="ClientCollection[3].ClientUserId"
                elem1.name = collectionName + "[" + (i - 1) + "]." + tdOne;
                var elem2 = document.getElementById(collectionName + "_" + i + "__" + tdTwo);
                elem2.id = collectionName + "_" + (i - 1) + "__" + tdTwo;
                elem2.name = collectionName + "[" + (i - 1) + "]." + tdTwo;

                var elem3 = document.getElementById(collectionName + "_" + i + "__" + tdThree);
                elem3.id = collectionName + "_" + (i - 1) + "__" + tdThree;
                elem3.name = collectionName + "[" + (i - 1) + "]." + tdThree;

                if (tdFour != '') {
                    var elem4 = document.getElementById(collectionName + "_" + i + "__" + tdFour);
                    elem4.id = collectionName + "_" + (i - 1) + "__" + tdFour;
                    elem4.name = collectionName + "[" + (i - 1) + "]." + tdFour;
                }

                if (tdFive != null && tdFive != '') {
                    var elem5 = document.getElementById(collectionName + "_" + i + "__" + tdFive);
                    elem5.id = collectionName + "_" + (i - 1) + "__" + tdFive;
                    elem5.name = collectionName + "[" + (i - 1) + "]." + tdFive;
                }
                if (tdSix != null && tdSix != '') {
                    var elem6 = document.getElementById(collectionName + "_" + i + "__" + tdSix);
                    elem6.id = collectionName + "_" + (i - 1) + "__" + tdSix;
                    elem6.name = collectionName + "[" + (i - 1) + "]." + tdSix;
                }
                if (tdSeven != null && tdSeven != '') {
                    var elem7 = document.getElementById(collectionName + "_" + i + "__" + tdSeven);
                    elem7.id = collectionName + "_" + (i - 1) + "__" + tdSeven;
                    elem7.name = collectionName + "[" + (i - 1) + "]." + tdSeven;
                }

            }

        }

    }

}
