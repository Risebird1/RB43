function isEmail(email) {
    var regex = /^([a-zA-Z0-9_.+-])+\@(([a-zA-Z0-9-])+\.)+([a-zA-Z0-9]{2,4})+$/;
    return regex.test(email);
}

function OpenHtmlDocument(htmlData) {

    let child = window.open("about:blank", "mailContent");
    child.document.write(htmlData);
    child.document.close();
}
function moreless(id) {
    $('.moretext_' + id).slideToggle();
    if ($('#read_' + id).val() == 'Readmore') {
        $('#read_' + id).empty().html('Read more');
        $('#read_' + id).val('Readless');
    } else {
        $('#read_' + id).empty().html('Read less');
        $('#read_' + id).val('Readmore');
    }
}
function RemoveInterviewerFromSkillChannel(interviewerEmail, irfid) {
    swal.fire({
        title: 'Are you sure?',
        text: 'Want to remove interviewer from the Skill channel interviews',
        type: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes, Remove it!',
        preConfirm: function () {
            return new Promise(function (resolve, reject) {
                $.ajax({
                    type: "POST",
                    url: "/Interview/RemoveInterviewerFromIRF",
                    data: { interviewerEmail: interviewerEmail, irfid: irfid },
                    success: function (result) {
                        resolve(result);
                    }
                })
            })
        },
        allowOutsideClick: false
    }).then(function (result) {
        if (result.dismiss !== 'cancel') {
            swal.fire({
                title: 'Removed!',
                type: 'success',
                html: '<p>The Interviewer has been removed.</p>',
                showCancelButton: false,
                confirmButtonColor: '#3085d6',
                confirmButtonText: 'Close!',
                allowOutsideClick: false
            }).then(function () {
                location.reload(true);
            })
        }
        else if (result.dismiss === 'cancel') {
            swal.fire(
                'Cancelled',
                'remove cancelled! :)',
                'error'
            )
        }
    })
}