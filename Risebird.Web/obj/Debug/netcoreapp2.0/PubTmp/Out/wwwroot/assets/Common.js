function Notify(msg, Type, position) {
    //setTimeout(function () {


    swal({ title: Type, text: msg, type: Type.toLowerCase() });


    // }, 1000);
}


function findElement(arr, propName, propValue) {
    for (var i = 0; i < arr.length; i++)
        if (arr[i][propName] == propValue)
            return i;
}


function numbersonly(e) {
    var unicode = e.charCode ? e.charCode : e.keyCode
    if (unicode != 8) { //if the key isn't the backspace key (which we should allow)
        if (unicode < 48 || unicode > 57) //if not a number
            return false //disable key press
        //  alert("Please ! Enter numeric value only");
        //  else  alert("Please ! Enter numeric value only"); 
    }

}

function numbersonly1(e) {

    var unicode = e.charCode ? e.charCode : e.keyCode
    if (unicode != 8  ) { //if the key isn't the backspace key (which we should allow)
        if ((unicode < 48 || unicode > 57) && unicode != 45) //if not a number
            return false //disable key press
        //  alert("Please ! Enter numeric value only");
        //  else  alert("Please ! Enter numeric value only"); 
    }

}

function alphabetsonly(e) {
    var unicode = e.charCode ? e.charCode : e.keyCode
    if (unicode != 8) { //if the key isn't the backspace key (which we should allow)
        if ((unicode < 65 || unicode > 90) && (unicode < 97 || unicode > 122) && unicode!=32) //if not a number
            return false //disable key press
        //  alert("Please ! Enter numeric value only");
        //  else  alert("Please ! Enter numeric value only"); 
    }

}



