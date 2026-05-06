# Risebird Chrome Proctor Extension

Production-grade Chrome Extension (Manifest V3) for monitor-only screen-share proctoring with backend session control, violation reporting, and live media telemetry.

## Features

- Monitor-only enforcement using `getDisplayMedia({ video: { displaySurface: "monitor" }, audio: false })`.
- Rejects non-monitor share modes and records explicit violations.
- Streams screen chunks (MediaRecorder) every ~5 seconds to backend.
- Uploads screenshot snapshots of current exam tab at policy interval.
- Reports heartbeat with status and diagnostics while session is active.
- Captures violations (copy/cut/paste/right-click/page hidden/fullscreen exit/window blur).
- Dynamic restricted-domain blocking via dashboard policy and DNR dynamic rules.
- No auto-end on violation: violations are logged and reported only.

## File Layout

- `manifest.json` - MV3 declaration.
- `service-worker.js` - session orchestration, backend calls, alarms, DNR sync.
- `content-monitor.js` - monitor capture + violation interception + chunk emission.
- `popup.html` / `popup.js` / `popup.css` - operator UI.
- `rules/restricted-sites.json` - static rule resource placeholder.
- `scripts/build-extension-zip.sh` - distribution packaging script.

## Backend API Paths

The service worker uses these endpoints:

- `/api/proctoring/session/join`
- `/api/proctoring/session/start`
- `/api/proctoring/session/event`
- `/api/proctoring/session/screenshot`
- `/api/proctoring/session/screen-chunk`
- `/api/proctoring/session/heartbeat`
- `/api/proctoring/session/stop`

All requests include:

- `Authorization: Bearer <sessionToken>`

## Local Development

1. Open Chrome and go to `chrome://extensions`.
2. Enable **Developer mode**.
3. Click **Load unpacked** and select `chrome-proctor-extension-v2`.
4. Open extension popup and provide:
   - Backend URL
   - Session token
   - Session ID
   - Candidate ID (optional)
5. Start session from popup and approve monitor sharing prompt.

## Packaging

Run:

```bash
bash scripts/build-extension-zip.sh
```

Zip output is generated in `dist/`.

## Security Notes

- Session token is required before network actions.
- Dynamic DNR rules are rebuilt per session from dashboard policy.
- Violation reporting is best-effort and does not terminate the session.
