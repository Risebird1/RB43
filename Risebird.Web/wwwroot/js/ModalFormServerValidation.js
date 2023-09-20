
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
            var isValidElement = newBody.find('[name="IsValid"]') == 'True';
            if (isValidElement != null) {
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

    var audioFeedback_placeholderElement = $('#modal_clientAudioFeedback_placeholder');


    $('button[data-toggle="ajax-modal-feedback_audio"]').click(function (event) {
        OpenPopUp(event, audioFeedback_placeholderElement);

    });

    var recipientActivity_placeholderElement = $('#modal_recipientActivity_placeholder');


    $('button[data-toggle="ajax-modal-recipientActivity"]').click(function (event) {

     OpenPopUps(event, recipientActivity_placeholderElement);
    

    });

    var candidateCommunicationDetails_placeholderElement = $('#modal_candidateCommunicationDetails_placeholder');


    $('a[data-toggle="ajax-modal-CommunicationDetails"]').click(function (event) {
        OpenPopUps(event, candidateCommunicationDetails_placeholderElement);
   
    });

    //var sme_placeholderElement = $('#modal_SME_placeholder');


    //$('button[data-toggle="ajax-modal-SubjectMatterExpert"]').click(function (event) {
    //    debugger;
    //    OpenPopUp(event, sme_placeholderElement);

    //});
    var sme_placeholderElement = $('#modal_sme_placeholder');


    $('button[data-toggle="ajax-modal-smeActivity"]').click(function (event) {
        OpenPopUp(event, sme_placeholderElement);

    });
    sme_placeholderElement.on('click', '[data-save="modal"]', function (event) {
        event.preventDefault();
        var form = $(this).parents('.modal').find('form');
        var actionUrl = form.attr('action');
        //var dataToSend = form.serialize();
        ////File data
        //debugger;
        //var file_data = $('input[name="JDFile"]')[0].files;
        //for (var i = 0; i < file_data.length; i++) {
        //    dataToSend.append(file_data[i].name, file_data[i]);
        //}
        var data = new FormData();

        //Form data
        var form_data = form.serializeArray();
        $.each(form_data, function (key, input) {
            data.append(input.name, input.value);
        });

        //File data
        var file_data = $('input[name="JDFile"]')[0].files;
        for (var i = 0; i < file_data.length; i++) {
            data.append(file_data[i].name, file_data[i]);
        }

        //console.log(dataToSend);
        //$.post(actionUrl, data).done(function (dataRecieved) {
            
        //    var newBody = $('.modal-body', dataRecieved);
        //    placeholderElement.find('.modal-body').replaceWith(newBody);

        //});


        $.ajax({
            url: actionUrl,
            method: "post",
            processData: false,
            contentType: false,
            data: data,
            success: function (dataRecieved) {
                var newBody = $('.modal-body', dataRecieved);
                sme_placeholderElement.find('.modal-body').replaceWith(newBody);

                // find IsValid input field and check it's value
                // if it's valid then hide modal window
                var isValidElement = newBody.find('[name="IsValidCustom"]') == 'True';
                if (isValidElement != null) {
                    var isValid = newBody.find('[name="IsValidCustom"]').val() == 'True';
                    if (isValid) {
                        sme_placeholderElement.find('.modal').modal('hide');
                        var html = "Data updated successfully!";
                        
                        Swal.fire({
                            title: '<strong>Successful</u></strong>',
                            type: 'success',
                            html: html,
                            confirmButtonText: "Close",
                            allowOutsideClick: false
                        });
                    }
                }
            },
            error: function (e) {
                //error
            }
        });
    })

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
    if (placeholderElement == '#modal_candidateCommunicationDetails_placeholder') {
        $('#modal_recipientActivity_placeholder').empty();
    }
    $.get(url).done(function (data) {
        placeholderElement.html(data);
        if (setRating == true) {
            setClientFeedbackRating();
        }
        placeholderElement.find('.modal').modal('show');
    });
}



