//webkitURL is deprecated but nevertheless
URL = window.URL || window.webkitURL;

var gumStream; 						//stream from getUserMedia()
var rec; 							//Recorder.js object
var input; 							//MediaStreamAudioSourceNode we'll be recording

// shim for AudioContext when it's not avb. 
var AudioContext = window.AudioContext || window.webkitAudioContext;
var audioContext;//audio context to help us record
var interviewId;
var irfid;
var candidateType;
function startRecording() {
    //console.log("recordButton clicked");
    document.getElementById("stopRecButton").disabled = false;
    document.getElementById("startRecButton").disabled = true;
    document.getElementById("cancelRecButton").disabled = false;
    document.getElementById("recordingStatus").innerHTML = "Recording started...";

    var constraints = { audio: true, video: false };


    navigator.mediaDevices.getUserMedia(constraints).then(function (stream) {
        //console.log("getUserMedia() success, stream created, initializing Recorder.js ...");

        /*
            create an audio context after getUserMedia is called
            sampleRate might change after getUserMedia is called, like it does on macOS when recording through AirPods
            the sampleRate defaults to the one set in your OS for your playback device

        */
        audioContext = new AudioContext();

        //update the format 

        /*  assign to gumStream for later use  */
        gumStream = stream;

        /* use the stream */
        input = audioContext.createMediaStreamSource(stream);

        /* 
            Create the Recorder object and configure to record mono sound (1 channel)
            Recording 2 channels  will double the file size
        */
        rec = new Recorder(input, { numChannels: 1 });

        //start the recording process
        rec.record();

        //console.log("Recording started");

    }).catch(function (err) {
        //enable the record button if getUserMedia() fails

    });
}

//function pauseRecording() {
//    console.log("pauseButton clicked rec.recording=", rec.recording);
//    if (rec.recording) {
//        //pause
//        rec.stop();
//        pauseButton.innerHTML = "Resume";
//    } else {
//        //resume
//        rec.record()
//        pauseButton.innerHTML = "Pause";

//    }
//}

function stopRecording(id, canType) {
    document.getElementById("stopRecButton").disabled = true;
    document.getElementById("startRecButton").disabled = false;
    document.getElementById("cancelRecButton").disabled = true;
    document.getElementById("recordingStatus").innerHTML = "Recording stopped!";
    setTimeout(afterTimeOut, 500);
    interviewId = id;
    candidateType = canType;
    //tell the recorder to stop the recording
    rec.stop();

    //stop microphone access
    gumStream.getAudioTracks()[0].stop();

    //create the wav blob and pass it on to createDownloadLink
    rec.exportWAV(createDownloadLink);
}

function createDownloadLink(blob) {
    var list = document.getElementById("recordingsList");
    var firstChild = list.firstChild;
    if (firstChild) {
        list.removeChild(list.childNodes[0]);

    }


    var url = URL.createObjectURL(blob);
    var au = document.createElement('audio');
    //var li = document.createElement('li');
    var li = document.createElement('div');
    var link = document.createElement('a');
    //var recordingList = document.getElementById("recordingsList");
    //name of .wav file to use during upload and download (without extendion)
    var filename = "Audio_FeedBack_" + interviewId + "_" + new Date().toISOString();//new Date().toISOString();

    //add controls to the <audio> element
    au.controls = true;
    au.src = url;

    //save to disk link
    link.href = url;
    link.download = filename + ".wav"; //download forces the browser to donwload the file using the  filename
    link.innerHTML = "Save to disk";

    //add the new audio element to li
    li.appendChild(au);

    //add the filename to the li
    //li.appendChild(document.createTextNode(filename + ".wav "));

    //add the save to disk link to li
    li.appendChild(link);


    document.getElementById('btnUpload').disabled = true;

    var fd = new FormData();
    fd.append("audio_data", blob, filename + ".wav");
    fd.append('candidateType', candidateType);

    $.ajax(
        {
            url: '/Interview/UploadAudio',
            data: fd,
            processData: false,
            contentType: false,
            type: "POST",
            success: function (data) {
                document.getElementById('FeedbackAudioLink').value = data;
                document.getElementById('btnUpload').disabled = false;
                document.getElementById('playUploadedAudioOne').style.display = "none";
                document.getElementById('playUploadedAudioTwo').style.display = "block";
                document.getElementById('anchorAudioId').href = data;
                splitData = data.split("/");
                document.getElementById('anchorAudioId').innerHTML = splitData[splitData.length - 1];
                document.getElementById('controlAudioId').src = data;

            }
        }
    );
    //li.appendChild(document.createTextNode(" "));//add a space in between
    //li.appendChild(upload);//add the upload link to li

    //add the li element to the ol
    recordingsList.appendChild(li);
    document.getElementById('recordingRow').style.display = "block";

}
function afterTimeOut() {
    document.getElementById("recordingStatus").innerHTML = '';
}

function cancelRecording() {
    document.getElementById("stopRecButton").disabled = true;
    document.getElementById("startRecButton").disabled = false;
    document.getElementById("cancelRecButton").disabled = true;
    document.getElementById("recordingStatus").innerHTML = "Recording cancelled!";
    setTimeout(afterTimeOut, 500);
    //console.log("stopButton clicked");
    //disable the stop button, enable the record too allow for new recordings
    //reset button just in case the recording is stopped while paused
    //tell the recorder to stop the recording
    rec.stop();
    gumStream.getAudioTracks()[0].stop();
}
function stopIRFRecording(id) {
    document.getElementById("stopRecButton").disabled = true;
    document.getElementById("startRecButton").disabled = false;
    document.getElementById("cancelRecButton").disabled = true;
    document.getElementById("recordingStatus").innerHTML = "Recording stopped!";
    interviewId = id;
    candidateType = 3;
    //tell the recorder to stop the recording
    rec.stop();

    //stop microphone access
    gumStream.getAudioTracks()[0].stop();

    //create the wav blob and pass it on to createDownloadLink
    rec.exportWAV(createIRFDownloadLink);
}
function createIRFDownloadLink(blob) {
    var list = document.getElementById("recordingsList");
    var firstChild = list.firstChild;
    if (firstChild) {
        list.removeChild(list.childNodes[0]);
    }

    var url = URL.createObjectURL(blob);
    var au = document.createElement('audio');
    var li = document.createElement('div');
    var link = document.createElement('a');
    var filename = "Audio_ClientIRF_" + interviewId + "_" + new Date().toISOString();

    au.controls = true;
    au.src = url;

    //save to disk link
    link.href = url;
    link.download = filename + ".wav";
    link.innerHTML = "Save to disk";

    //add the new audio element to li
    li.appendChild(au);

    //add the save to disk link to li
    li.appendChild(link);

    var fd = new FormData();
    fd.append("audio_data", blob, filename + ".wav");

    $.ajax({
        url: '/IRF/UploadAudio',
        data: fd,
        processData: false,
        contentType: false,
        type: "POST",
        success: function (data) {
            document.getElementById("recordingStatus").innerHTML = '';
            document.getElementById('anchorAudioId').href = data;
            splitData = data.split("/");
            document.getElementById('anchorAudioId').innerHTML = splitData[splitData.length - 1];
            document.getElementById('controlAudioId').src = data;
        }
    });
    recordingsList.appendChild(li);
    document.getElementById('recordingRow').style.display = "block";
}


function startClientRecording() {
    document.getElementById("startClientRecButton").disabled = true;
    document.getElementById("stopClientRecButton").disabled = false;
    document.getElementById("cancelClientRecButton").disabled = false;
    document.getElementById("clientRecordingStatus").innerHTML = "Recording started...";

    var constraints = { audio: true, video: false };


    navigator.mediaDevices.getUserMedia(constraints).then(function (stream) {
        //console.log("getUserMedia() success, stream created, initializing Recorder.js ...");

        /*
            create an audio context after getUserMedia is called
            sampleRate might change after getUserMedia is called, like it does on macOS when recording through AirPods
            the sampleRate defaults to the one set in your OS for your playback device

        */
        audioContext = new AudioContext();

        //update the format 

        /*  assign to gumStream for later use  */
        gumStream = stream;

        /* use the stream */
        input = audioContext.createMediaStreamSource(stream);

        /* 
            Create the Recorder object and configure to record mono sound (1 channel)
            Recording 2 channels  will double the file size
        */
        debugger;
        rec = new Recorder(input, { numChannels: 1 });

        //start the recording process
        rec.record();

        //console.log("Recording started");

    }).catch(function (err) {
        //enable the record button if getUserMedia() fails

    });
}

function stopClientRecording(interviewId, irfid) {
    debugger;
    document.getElementById("stopClientRecButton").disabled = true;
    document.getElementById("startClientRecButton").disabled = false;
    document.getElementById("cancelClientRecButton").disabled = true;
    document.getElementById("clientRecordingStatus").innerHTML = "Recording stopped!";
    setTimeout(afterClientRecordingTimeOut, 500);
    interviewId = interviewId;
    irfid = irfid;
    //candidateType = canType;
    //tell the recorder to stop the recording
    rec.stop();

    //stop microphone access
    gumStream.getAudioTracks()[0].stop();

    //create the wav blob and pass it on to createDownloadLink
    rec.exportWAV(createClientRecordingDownloadLink);
}



function createClientRecordingDownloadLink(blob) {


    var fd = new FormData();
    fd.append("audio_data", blob, filename + ".wav");
    fd.append('interviewId', interviewId);
    fd.append('isClientFeedback', true);
    fd.append('irfid', irfid);

    $.ajax(
        {
            url: '/Interview/UploadAudio',
            data: fd,
            processData: false,
            contentType: false,
            type: "POST",
            success: function (data) {

                splitData = data.split("/");

                // Create anchor element.
                var a = document.createElement('a');

                // Set the href property.
                a.href = data;
                a.id = "anchorAudioId";
                a.setAttribute('download', '');
                if (splitData != null && splitData.length > 1) {
                    a.innerText = splitData[splitData.length - 1];
                }


                // Append the anchor element.
                const elementFile = document.getElementById("td_audioFile").children[0];
                elementFile.replaceChild(a, elementFile.childNodes[0]);

                var audio = document.createElement('audio');
                audio.id = 'controlAudioId';
                audio.controls = 'controls';
                audio.src = data;

                const elementAudio = document.getElementById("td_audioControl");//.children[0];
                elementAudio.removeChild(elementAudio.children[0]);
                elementAudio.appendChild(audio);

            }
        }
    );


}

function afterClientRecordingTimeOut() {
    document.getElementById("clientRecordingStatus").innerHTML = '';
}

function cancelClientRecording() {
    document.getElementById("stopClientRecButton").disabled = true;
    document.getElementById("startClientRecButton").disabled = false;
    document.getElementById("cancelClientRecButton").disabled = true;
    document.getElementById("clientRecordingStatus").innerHTML = "Recording cancelled!";
    setTimeout(afterClientRecordingTimeOut, 500);
    //console.log("stopButton clicked");
    //disable the stop button, enable the record too allow for new recordings
    //reset button just in case the recording is stopped while paused
    //tell the recorder to stop the recording
    rec.stop();
    gumStream.getAudioTracks()[0].stop();
}