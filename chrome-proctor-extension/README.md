# Risebird Proctoring Chrome Extension

This extension implements a practical model for Chrome-based proctoring across four layers: **capture**, **observe**, **control**, and **coordinate**.

## Implemented features

- Session start/stop and violation counting
- Candidate-context aware session start from join-dashboard handshake
- Tab switching, window switching, and focus-loss detection
- Restricted-domain detection and blocking (static ruleset `rules/restricted-sites.json` + dynamic rules from popup settings)
- Screen/camera/mic permission check from popup
- Browsing cleanup (`chrome.browsingData.remove`) for the last N hours
- Local event history with downloadable JSON report

## Load extension locally

1. Open `chrome://extensions`
2. Enable **Developer mode**
3. Click **Load unpacked**
4. Select the `chrome-proctor-extension` folder

## Candidate join-dashboard integration

### A) Extension readiness probe

```html
<script>
  window.postMessage({ type: 'RB_PROCTORING_HELLO' }, window.location.origin);
  window.addEventListener('message', function (event) {
    if (event.origin !== window.location.origin) return;
    if (event.data?.type === 'RB_PROCTORING_EXTENSION_READY') {
      console.log('Proctoring extension detected', event.data.payload);
    }
  });
</script>
```

### B) Join handshake

```html
<script>
  function notifyJoinToExtension(interviewId, candidateId, token) {
    window.postMessage(
      {
        type: 'RB_PROCTORING_JOIN',
        payload: {
          interviewId: String(interviewId),
          candidateId: String(candidateId),
          proctorSessionToken: token
        }
      },
      window.location.origin
    );
  }
</script>
```

### C) Query-string fallback

If the join URL contains `interviewId` and `proctorSessionToken`, the content script sends the handshake once automatically.

Example:

`https://your-domain/interview/join?interviewId=123&candidateId=456&proctorSessionToken=abc123`

## Notes

- The capture flow is a **consent check**; a full recording/upload pipeline should be implemented server-side.
- Use legal/privacy disclosures and explicit candidate consent before production deployment.
