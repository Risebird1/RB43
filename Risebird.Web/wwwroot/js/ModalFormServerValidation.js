
$(document).ready(function () {
    var placeholderElement = $('#modal_clientComment_placeholder');
    var dateOfInterview = '';
    $('button[data-toggle="ajax-modal"]').click(function (event) {
        OpenPopUp(event, placeholderElement, true);
        //var url = $(this).data('url');
        if (this.hasAttribute('data-interviewdate')) {
            dateOfInterview = $(this).data('interviewdate');
        }
        
    });

    placeholderElement.on('click', '[data-save="modal"]', function (event) {
        event.preventDefault();

        var form = $(this).parents('.modal').find('form');
        var actionUrl = form.attr('action');
        var dataToSend = form.serialize();
        $.post(actionUrl, dataToSend).done(function (data) {
            //var newBody = $('#comments_modal_body', data);
            //placeholderElement.find('#comments_modal_body').replaceWith(newBody);
            var newBody = $('.modal-body', data);
            placeholderElement.find('.modal-body').replaceWith(newBody);

            // find IsValid input field and check it's value
            // if it's valid then hide modal window
            var isValid = newBody.find('[name="IsValid"]').val() == 'True';
            if (isValid) {
                placeholderElement.find('.modal').modal('hide');
                var html = "Interviewer rated successfully!";
                if (dateOfInterview != null && dateOfInterview != '') {
                    html = "Interviewer rated successfully for the interview dated " + dateOfInterview + "!";
                }
                
                Swal.fire({
                    title: '<strong>Successful</u></strong>',
                    type: 'success',
                    html: html,
                    confirmButtonText: "Close",
                    allowOutsideClick: false
                });
            }
        });

    });

    var el = document.getElementById("modal_clientComment_placeholder");
    if (el != null) {
        el.addEventListener("keypress", function (event) {
            if (event.key === "Enter") {
                event.preventDefault();
            }
        });
    }
    

    var audio_placeholderElement = $('#modal_clientAudio_placeholder');


    $('button[data-toggle="ajax-modal-audio"]').click(function (event) {
        OpenPopUp(event, audio_placeholderElement);

    });

});

function ChangeClientFeedbackRating(value) {
    $("#Rating").val(value);
}

function setClientFeedbackRating() {
    var ratingElement = document.getElementById('Rating');
    if (ratingElement != null) {
        rating = $("#Rating").val();
        applyRating(rating);
    }
}

function applyRating(rating) {
    if (rating != '' && parseInt(rating) > 0) {
        document.getElementById("Q2_" + rating).checked = true;
    }
    else {
        for (i = 1; i <= 5; i++) {
            document.getElementById("Q2_" + i).checked = false;
        }
    }
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

function OpenPopUp(event, placeholderElement, setRating = false) {

    //var url = $(this).data('url');
    var url = event.delegateTarget.dataset.url;
    $.get(url).done(function (data) {
        placeholderElement.html(data);
        if (setRating == true) {
            setClientFeedbackRating();
        }
        placeholderElement.find('.modal').modal('show');
    });
}
