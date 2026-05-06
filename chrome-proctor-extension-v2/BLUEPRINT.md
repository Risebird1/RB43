# Implementation Blueprint

## Session Lifecycle

### Start

1. Validate popup inputs (`backendBaseUrl`, `sessionToken`, `sessionId`).
2. Call `/api/proctoring/session/join`.
3. Call `/api/proctoring/session/start`.
4. Read dashboard policy from handshake payload.
5. Configure intervals and domain policy.
6. Apply DNR dynamic block rules.
7. Start heartbeat + screenshot alarms.
8. Start content-side monitor capture and chunk streaming.

### Active

- Heartbeat interval posts current status/violation counters.
- Screenshot interval captures current exam tab and uploads.
- Content script sends chunk uploads every chunk interval.
- Content script violation hooks emit reporting events.

### Stop

1. Stop content capture stream.
2. Clear alarms.
3. POST `/api/proctoring/session/stop`.
4. Remove dynamic DNR rules.
5. Preserve diagnostics for popup visibility.

## Violation Event Catalog

- `COPY_ATTEMPT`
- `CUT_ATTEMPT`
- `PASTE_ATTEMPT`
- `RIGHT_CLICK_ATTEMPT`
- `PAGE_HIDDEN`
- `FULLSCREEN_EXIT`
- `WINDOW_BLUR`
- `NON_MONITOR_SHARE_REJECTED`
- `SCREEN_SHARE_STOPPED`
- `SCREEN_CHUNK_UPLOAD_FAILED`
- `SCREENSHOT_UPLOAD_FAILED`

## Dashboard Policy Contract

Expected policy fields from join/start handshake:

```json
{
  "policy": {
    "screenshotIntervalSeconds": 15,
    "heartbeatIntervalSeconds": 10,
    "chunkIntervalMs": 5000,
    "restrictedDomains": ["facebook.com", "web.whatsapp.com"],
    "allowedHosts": ["exam.risebird.com"]
  }
}
```

If absent, extension uses internal defaults.

## Operational Guarantees

- All backend calls use bearer token.
- Failure to upload chunk/screenshot raises explicit violation event.
- Timer cleanup is guaranteed on session stop.
- Session remains active on violation unless explicitly stopped.
