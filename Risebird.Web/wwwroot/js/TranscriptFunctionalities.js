var totalHighlightedWords = 0;
var currentlyFocusedOn = -1;
var highlightedWords;
//var toBeFocusedOn = 0;
function GetMeetingAudioText(imid, transcriptUrl = null) {
    var transcriptElement = document.getElementById('id_transcript');
    if (transcriptElement != null) {
        transcriptElement.innerHTML = '';
        $.ajax({
            url: '/Interview/GetMeetingAudioText',
            data: { 'imid': imid, 'transcriptUrl': transcriptUrl },
            async: true,
            success: function (data) {
                if (data == null || data.convertedText == null || data.convertedText == '') {
                    transcriptElement.innerHTML = 'Not available';
                }
                else {

                    var innerText = '<label class="matchtxt" style="color:#1ca396;padding:0px;font-size:17px;">Total Keywords Match Found:</label><span class="matcnum" style="font-size: 17px; color:#1ca396;font-weight: 600;padding-left: 2px;">' + data.total + '</span><br>  <div class="matchdiv"><div style="overflow-y:scroll;white-space:break-spaces;font-size:14px;" class="scroller" id="style-2">' + data.convertedText + '</div></div>'
                    totalHighlightedWords = data.total;
                    transcriptElement.innerHTML = innerText;

                }

            }

        });
    }
    
}

$(document).ready(function () {
    highlightedWords = document.getElementsByClassName('highlighted')
});

function ScrollToAppropriateHighlightedText(direction) {
    const focusedElement = document.querySelector('.focused');
    if (focusedElement) {
        focusedElement.classList.remove('focused');
    }
    if (totalHighlightedWords > 0) {
        if (direction === 'n')//n stands for next
        {

            if (currentlyFocusedOn < (totalHighlightedWords - 1)) {

                var elementTobeFocusedOn = highlightedWords[currentlyFocusedOn + 1];
                elementTobeFocusedOn.classList.add('focused');
                elementTobeFocusedOn.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
                currentlyFocusedOn = currentlyFocusedOn + 1;
                document.getElementById("idbtnPrevious").style.pointerEvents = "";
                document.getElementById("idbtnPrevious").style.opacity = "1";

            }
            else {
                document.getElementById("idbtnNext").style.pointerEvents = "none";
                document.getElementById("idbtnNext").style.opacity = "0.5";
            }
            
        }
        if (direction === 'p')//p stands for previous
        {
            if (currentlyFocusedOn > 0) {

                var elementTobeFocusedOn = highlightedWords[currentlyFocusedOn - 1];
                elementTobeFocusedOn.classList.add('focused');
                elementTobeFocusedOn.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
                currentlyFocusedOn = currentlyFocusedOn - 1;
                document.getElementById("idbtnNext").style.pointerEvents = "";
                document.getElementById("idbtnNext").style.opacity = "1";
               
            }
            else {
                document.getElementById("idbtnPrevious").style.pointerEvents = "none";
                document.getElementById("idbtnPrevious").style.opacity = "0.5";

            }

        }
        
    }
    return;
}

