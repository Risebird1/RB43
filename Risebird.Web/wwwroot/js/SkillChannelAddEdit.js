function CheckDuplicate(idInitials, searchFor, value, currentIndex) {
    var searchForUsers = $(searchFor);
    var users = searchForUsers.filter(checkNode);

    function checkNode(index) {
        var user = searchForUsers[index];
        tagName = user.tagName;
        if (tagName != null && tagName != '') {

            if (tagName.toLowerCase() == 'select' && user.disabled == false) {
                return true;
            }
            else {
                var elementIdIndex = user.id.split('_')[1];
                return (tagName.toLowerCase() == "input" && index == elementIdIndex);
            }

        }
        else {
            return false;
        }
    }
    if (users != null) {
        for (i = 0; i < users.length; i++) {


            if (i != parseInt(currentIndex)) {
                var elementId = idInitials + '_' + i + '__UserId';
                userId = document.getElementById(elementId).value;
                if (value !=null && value != '' && userId !=null && parseInt(userId) == parseInt(value)) {
                    //duplicate
                    alert("This user is already added to this list! Kindly choose another user.");
                    document.getElementById(idInitials + '_' + currentIndex + '__UserId').value = "";
                    document.getElementById(idInitials + '_' + currentIndex + '__PrimarySkill').value = "";
                    document.getElementById(idInitials + '_' + currentIndex + '__PerSuccessRate').value = "";
                    return true;
                }
            }

        }
    }
    return false;
}

