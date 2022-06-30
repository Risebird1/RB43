function FilterAssessmentGroupDropDown(categorySelected, assessmentGroupCategoryMapArray) {
    $("#AssessmentGroupTemplate option").remove();
    if (categorySelected != null && categorySelected != '') {

        if (assessmentGroupCategoryMapArray != null) {
            const result = assessmentGroupCategoryMapArray.filter(checkForCorrespondingAssessmentGroup);

            function checkForCorrespondingAssessmentGroup(arrayItem) {
                if (arrayItem != null) {
                    if (arrayItem.Item2 != null && arrayItem.Item2.toLowerCase() == categorySelected.toLowerCase() && arrayItem.Item1 != null) {
                        return true;
                    }
                }
                return false;
            }

            AppendOptionsToAssessmentGroupsFilterDropDown(result);
        }


    }
    else {

        if (assessmentGroupCategoryMapArray != null) {

            AppendOptionsToAssessmentGroupsFilterDropDown(assessmentGroupCategoryMapArray);
        }
    }
    return;
}

function AppendOptionsToAssessmentGroupsFilterDropDown(data) {
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
    }
    return;
}