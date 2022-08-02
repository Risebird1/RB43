function FilterAsmtGrpPrSkillDropDown(categorySelected, asgGrpPrSkillsCategoryMapArray) {
    //$("#AssessmentGroupTemplate option").remove();
    //var primarySkillsOptions = $("#PrimarySkills option");
    //$("#PrimarySkills option").remove();
    //$(".px-md-2.px-1").remove();
    //$('#PrimarySkills').multiselect('refresh');
    //$("#PrimarySkills").multiselect("destroy").multiselect({
    //    // Your options...
    //});
    if (categorySelected != null && categorySelected != '') {

        if (asgGrpPrSkillsCategoryMapArray != null) {
            const result = asgGrpPrSkillsCategoryMapArray.filter(checkForCorrespondingAsmtGrpPrSkill);

            function checkForCorrespondingAsmtGrpPrSkill(arrayItem) {
                if (arrayItem != null) {
                    if (arrayItem.Item2 != null && arrayItem.Item2.toLowerCase() == categorySelected.toLowerCase() && arrayItem.Item1 != null) {
                        return true;
                    }
                }
                return false;
            }

            AppendOptionsToAsmtGrpPrSkillsFilterDropDown(result);
        }


    }
    else {

        if (asgGrpPrSkillsCategoryMapArray != null) {

            AppendOptionsToAsmtGrpPrSkillsFilterDropDown(asgGrpPrSkillsCategoryMapArray);
        }
    }
    return;
}

function AppendOptionsToAsmtGrpPrSkillsFilterDropDown(data) {
    $("#AssessmentGroupTemplate option").remove();
    $("#PrimarySkills option").remove();
    //$('#PrimarySkills').empty().multiSelect('refresh');
    if (data != null) {
        var drpAssessmentGroups = $("#AssessmentGroupTemplate");
        var option = $("<option />");
        option.html("Select");
        option.val();
        drpAssessmentGroups.append(option);
        if (data.length > 0) {
            $(data).each(function () {
                if (this.Item1 != null && this.Item1 != '') {
                    var option = $("<option />");
                    option.html(this.Item1);
                    option.val(this.Item1);
                    drpAssessmentGroups.append(option);
                }

            });
        }

        var isPrimarySkillAppended = false;
        var drpPrimarySkills = $("#PrimarySkills");
        var option = $("<option />");
        option.html("Select");
        option.val();
        drpPrimarySkills.append(option);
        if (data.length > 0) {
            $(data).each(function () {
                if (this.Item3 != null && this.Item3 != '') {
                    var skillArray = this.Item3.split(';');
                    $(skillArray).each(function () {
                        var option = $("<option />");
                        option.html(this);
                        option.val(this);
                        drpPrimarySkills.append(option);
                        if (!isPrimarySkillAppended) {
                            isPrimarySkillAppended = true;
                        }
                    })
                    
                }

            });
        }
        if (!isPrimarySkillAppended) {
            $("#PrimarySkills option").remove();

        }
    }
    return;
}