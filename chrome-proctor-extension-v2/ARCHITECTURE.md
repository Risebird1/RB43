# Architecture

## Runtime Components

- **Popup UI (`popup.*`)**
  - Collects session bootstrap values.
  - Starts/stops proctoring through runtime messaging.
  - Displays live diagnostics from service worker state.
  - Shows restricted domains and allowed hosts as dashboard-controlled.

- **Service Worker (`service-worker.js`)**
  - Owns session lifecycle state.
  - Calls backend APIs with Bearer token.
  - Sends heartbeats and screenshot uploads via alarm timers.
  - Receives chunk uploads and violations from content script.
  - Maintains violation counters/diagnostics.
  - Syncs dynamic DNR rules from dashboard handshake policy.

- **Content Script (`content-monitor.js`)**
  - Starts monitor-only `getDisplayMedia`.
  - Verifies `displaySurface` and rejects tab/window sharing.
  - Streams media chunks using `MediaRecorder`.
  - Captures and reports browser-level violations.
  - Reports screen-share-stop violation when track ends.

## Data Flow

1. Popup sends `START_SESSION` with backend/session details.
2. Service worker calls `/join` then `/start`.
3. Service worker extracts policy and updates DNR dynamic rules.
4. Popup asks content script to start monitor-only capture.
5. Content script emits `UPLOAD_SCREEN_CHUNK` every chunk interval.
6. Service worker uploads chunks to `/screen-chunk`.
7. Service worker alarm sends heartbeat to `/heartbeat`.
8. Service worker alarm captures exam tab screenshot and posts `/screenshot`.
9. Content script violation events are forwarded to `/event`.
10. Popup sends `STOP_SESSION`; worker posts `/stop` and clears timers/rules.

## State Model (Service Worker)

- `sessionActive`, `sessionId`, `sessionToken`, `backendBaseUrl`, `tabId`
- Policy values:
  - `screenshotIntervalSeconds`
  - `heartbeatIntervalSeconds`
  - `chunkIntervalMs`
  - `restrictedDomains`
  - `allowedHosts`
- Diagnostics:
  - `screenshotUploads`
  - `lastScreenshotAt`
  - `lastHeartbeatAt`
  - `violationCount`
  - `violations[]`

State is persisted in `chrome.storage.local` to survive worker restarts.

## Enforcement Policy

- Violations are **recorded and reported**.
- Violations do **not** automatically terminate session.
- Share-mode violations (non-monitor) are blocked at capture startup.
