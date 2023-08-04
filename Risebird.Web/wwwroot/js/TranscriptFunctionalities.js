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
                    if (data.keywordsFound != null && data.keywordsFound != '') {
                       
                    }
                    var innerText = '<div class="clearfix"><label class="matchtxt" style="color:#1ca396;padding:0px;font-size:17px;float:left">Suspicious Keywords Match Found:</label><span id="key" class="matcnum" style="foat:left"><h1  style="color:red;display:none;">' + data.keywordsFound + '</h1><div id=keycontainer></div></span></div><br>  <div class="matchdiv"><div style="overflow-y:scroll;white-space:break-spaces;font-size:14px;" class="scroller" id="style-2">' + data.convertedText + '</div></div>'
                    totalHighlightedWords = data.total;
                    transcriptElement.innerHTML = innerText;
                    const h1 = document.querySelector('h1');
                        output = document.getElementById('keycontainer'),
                        update = function () {
                            var words = h1.textContent.trim().split(/,\s*/);
                            while (output.firstChild) output.removeChild(output.firstChild);
                            for (var i = 0; i < words.length; i++) {
                                var div = document.createElement('div');
                                div.textContent = words[i];
                                output.appendChild(div);
                            }
                        };
                    update();
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

