
function GetMeetingAudioText(imid) {
    document.getElementById('id_transcript').innerHTML = '';
        $.ajax({
            url: '/Interview/GetMeetingAudioText',
            data: { 'imid': imid },
            async : true,
            success: function (data) {
                if (data == null || data.convertedText == null || data.convertedText == '') {
                    document.getElementById('id_transcript').innerHTML = 'Not available';
                }
                else {

                    var innerText = '<label class="matchtxt" style="color:#1ca396;padding:0px;font-size:17px;">Total Keywords Match Found:</label><span class="matcnum" style="font-size: 17px; color:#1ca396;font-weight: 600;padding-left: 2px;">' + data.total + '</span><br>  <div class="matchdiv"><div style="overflow-y:scroll;white-space:break-spaces;font-size:14px;" class="scroller" id="style-2">' + data.convertedText + '</div></div>'

                    document.getElementById('id_transcript').innerHTML = innerText;

                }

            }

        });
    }