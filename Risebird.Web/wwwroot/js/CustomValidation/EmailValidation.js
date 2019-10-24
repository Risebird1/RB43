function validateEmail() {
    debugger;
    //var mailformat = /^\w+([\.-]?\w+)*@\w+([\.-]?\w+)*(\.\w{2,3})+$/;
    //emailIds[i].match(mailformat)
    //to.match(mailformat)
    var to = document.getElementById("TO").value;
    if (to.includes(";")) {
        var emailIds = to.split(";");
        for (i = 0; i < emailIds.length; i++) {
            if (!emailIsValid(emailIds[i])) {

                document.getElementById("checkTo").innerHTML = "Kindly check the EmailId(s) entered. Use \";\" to add multiple recipients.";
                return false;
            }

        }
    }
    else {
        if (!emailIsValid(to)) {

            document.getElementById("checkTo").innerHTML = "Kindly check the EmailId(s) entered. Use \";\" to add multiple recipients.";

            alert("No!");
            return false;
        }
    }
    document.getElementById("checkTo").innerHTML = "";

    return true;

}

function emailIsValid(email) {
    debugger;
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}