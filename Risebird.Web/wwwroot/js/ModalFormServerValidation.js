$(document).ready(function () {
    var placeholderElement = $('#modal_clientComment_placeholder');
    $('button[data-toggle="ajax-modal"]').click(function (event) {
        var url = $(this).data('url');
        $.get(url).done(function (data) {
            placeholderElement.html(data);
            setClientFeedbackRating();
            placeholderElement.find('.modal').modal('show');
        });
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
            }
        });

    });

    var el = document.getElementById("modal_clientComment_placeholder");
    el.addEventListener("keypress", function (event) {
        if (event.key === "Enter") {
            event.preventDefault();
        }
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
