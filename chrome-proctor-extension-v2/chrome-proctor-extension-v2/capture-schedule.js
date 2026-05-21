/**
 * Shared capture timing — screenshots ~3.75–5/min, video chunks ~3.3–4.3/min.
 */
const CAPTURE_BASE_MS = 18000;
const CAPTURE_JITTER_RATIO = 0.2;

const CAPTURE_SCREENSHOT_MIN_GAP_MS = 12000;
const CAPTURE_SCREENSHOT_MAX_GAP_MS = 16000;
const CAPTURE_VIDEO_MIN_GAP_MS = 14000;
const CAPTURE_VIDEO_MAX_GAP_MS = 18000;

function jitteredCaptureDelayMs(baseMs, jitterRatio, bounds) {
  const base = Math.max(1000, Number(baseMs) || CAPTURE_BASE_MS);
  const ratio = Math.min(0.5, Math.max(0, Number(jitterRatio) || CAPTURE_JITTER_RATIO));
  const spread = base * ratio;
  const minBound = bounds?.minMs != null ? bounds.minMs : Math.max(10000, base - spread);
  const maxBound = bounds?.maxMs != null ? bounds.maxMs : Math.min(300000, base + spread);
  const min = Math.min(minBound, maxBound);
  const max = Math.max(minBound, maxBound);
  return Math.round(min + Math.random() * (max - min));
}

function interviewCaptureDelayMs(baseMs) {
  return interviewScreenshotDelayMs(baseMs);
}

function interviewScreenshotDelayMs(baseMs) {
  return jitteredCaptureDelayMs(baseMs, CAPTURE_JITTER_RATIO, {
    minMs: CAPTURE_SCREENSHOT_MIN_GAP_MS,
    maxMs: CAPTURE_SCREENSHOT_MAX_GAP_MS,
  });
}

function interviewVideoChunkDelayMs(baseMs) {
  return jitteredCaptureDelayMs(baseMs, CAPTURE_JITTER_RATIO, {
    minMs: CAPTURE_VIDEO_MIN_GAP_MS,
    maxMs: CAPTURE_VIDEO_MAX_GAP_MS,
  });
}

const CAPTURE_FIRST_SCREENSHOT_MS = 3000;
const CAPTURE_FIRST_CHUNK_MS = 4000;

function videoChunkTimesliceMs(baseMs) {
  return interviewVideoChunkDelayMs(baseMs || CAPTURE_BASE_MS);
}

