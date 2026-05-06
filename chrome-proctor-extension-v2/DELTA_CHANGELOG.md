# Delta Changelog (2026-03-13 Requirements)

## Added

- Implemented complete MV3 extension skeleton under `chrome-proctor-extension-v2`.
- Added all required backend API path integrations:
  - `/api/proctoring/session/join`
  - `/api/proctoring/session/start`
  - `/api/proctoring/session/event`
  - `/api/proctoring/session/screenshot`
  - `/api/proctoring/session/screen-chunk`
  - `/api/proctoring/session/heartbeat`
  - `/api/proctoring/session/stop`
- Added bearer authorization handling (`Authorization: Bearer <sessionToken>`).
- Added dashboard-driven policy ingestion and dynamic DNR rule sync.
- Added popup diagnostics and dashboard-controlled domain display.

## Enforcement and Monitoring

- Enforced monitor-only intent in content script:
  - `getDisplayMedia({ video: { displaySurface: "monitor" }, audio: false })`
- Added explicit rejection of tab/window share with violation logging.
- Added share-stop violation when monitor stream ends.
- Added violation listeners for:
  - copy
  - cut
  - paste
  - right click / context menu
  - page hidden/backgrounded
  - fullscreen exit
  - window blur/focus loss
- Preserved non-terminal policy: no automatic session stop on violations.

## Telemetry Pipelines

- Added MediaRecorder chunk flow (~5s cadence):
  - content script emits `UPLOAD_SCREEN_CHUNK` with `chunkSeq`, `mimeType`, `capturedAt`, `data`.
  - service worker posts chunk payload to `/screen-chunk`.
  - upload failure generates violation: `Screen chunk upload failed`.
- Added periodic visible-tab screenshot upload flow to `/screenshot`.
- Added screenshot diagnostics in state/report:
  - `screenshotUploads`
  - `lastScreenshotAt`
- Added periodic heartbeat flow with status, violation count, and timestamp.
- Added alarm/timer cleanup on stop.
