let mediaRecorder = null;
let mediaStream = null;
let audioChunks = [];
let recordedAudioBlob = null;

export async function startRecording() {
    if (mediaRecorder?.state === "recording") {
        return;
    }

    mediaStream = await navigator.mediaDevices.getUserMedia({ audio: true });
    audioChunks = [];

    const options = getRecorderOptions();
    mediaRecorder = new MediaRecorder(mediaStream, options);

    mediaRecorder.addEventListener("dataavailable", event => {
        if (event.data.size > 0) {
            audioChunks.push(event.data);
        }
    });

    mediaRecorder.start();
}

export async function stopRecording() {
    if (!mediaRecorder || mediaRecorder.state !== "recording") {
        return null;
    }

    const stopped = new Promise(resolve => {
        mediaRecorder.addEventListener("stop", resolve, { once: true });
    });

    mediaRecorder.stop();
    await stopped;

    const contentType = mediaRecorder.mimeType || "audio/webm";
    recordedAudioBlob = new Blob(audioChunks, { type: contentType });
    stopTracks();

    return {
        contentType,
        fileName: `recording.${getFileExtension(contentType)}`
    };
}

export function getRecordedAudioStream() {
    return recordedAudioBlob;
}

export function cancelRecording() {
    if (mediaRecorder?.state === "recording") {
        mediaRecorder.stop();
    }

    stopTracks();
}

function getRecorderOptions() {
    const supportedTypes = [
        "audio/webm;codecs=opus",
        "audio/webm",
        "audio/mp4"
    ];

    const mimeType = supportedTypes.find(type => MediaRecorder.isTypeSupported(type));
    if (!mimeType) {
        return {};
    }

    return { mimeType };
}

function getFileExtension(contentType) {
    if (contentType.includes("mp4")) {
        return "mp4";
    }

    return "webm";
}

function stopTracks() {
    mediaStream?.getTracks().forEach(track => track.stop());
    mediaStream = null;
    mediaRecorder = null;
    audioChunks = [];
}
