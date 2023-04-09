function checkfilterDate() {
    var startDateValue = document.getElementById("StartDate").value;
    var val = document.getElementById("EndDate").value;
    var d = new Date();

    var year = val.substring(0, 4);
    var month = parseInt(val.substring(5, 7)) - 1;
    var date = val.substring(8, 10);
    var endDate = new Date(year, month, date);

    var yearStart = startDateValue.substring(0, 4);
    var monthStart = parseInt(startDateValue.substring(5, 7)) - 1;
    var dateStart = startDateValue.substring(8, 10);
    var startDate = new Date(yearStart, monthStart, dateStart);

    if (((endDate - startDate) / (1000 * 3600 * 24)) > 45) {
        alert("Start Date & End Date difference can't be greater than 45 days.");
        document.getElementById("EndDate").focus();
        return false;
    }
    return true;
}