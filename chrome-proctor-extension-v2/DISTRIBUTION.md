# Distribution

## Build a Zip Package

From `chrome-proctor-extension-v2`:

```bash
bash scripts/build-extension-zip.sh
```

The generated artifact is:

- `dist/chrome-proctor-extension-v2-YYYYMMDD-HHMMSS.zip`

## Install Unpacked (QA / UAT)

1. Open `chrome://extensions`.
2. Enable **Developer mode**.
3. Select **Load unpacked**.
4. Pick the `chrome-proctor-extension-v2` directory.
5. Pin extension and launch popup.

## Publish Flow (Enterprise / Internal)

1. Build and sign off tested zip artifact.
2. Upload to internal extension distribution channel or Chrome Web Store private listing.
3. Roll out by policy to managed candidate machines.
4. Verify:
   - API connectivity with bearer token.
   - Monitor-only capture enforcement.
   - Dashboard policy sync and DNR blocking behavior.
   - Screenshot/chunk/heartbeat telemetry.

## Release Checklist

- [ ] Endpoints reachable over TLS.
- [ ] Session token issued and accepted.
- [ ] Handshake policy returns expected restricted domains.
- [ ] Screenshot and chunk upload backend storage validated.
- [ ] Violation events visible in proctoring dashboard.
- [ ] Stop event and timer cleanup validated.
