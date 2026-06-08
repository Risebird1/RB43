/**
 * Shared proctoring flow constants (imported by service worker via importScripts).
 */
const PROCTORING_FLOW_PHASE = {
  NOT_JOINED: 'not_joined',
  JOINING_SESSION: 'joining_session',
  WAITING_FOR_SCREEN_SHARE: 'waiting_for_screen_share',
  SCREEN_SHARING_ACTIVE: 'screen_sharing_active',
  PROCTORING_ACTIVE: 'proctoring_active',
  SCREEN_SHARE_STOPPED: 'screen_share_stopped',
  SESSION_ENDED: 'session_ended',
};

const MIN_SCREEN_SHARE_PROMPT_INTERVAL_MS = 8000;
const MAX_AUTO_PROMPT_RETRIES = 1;

/** Strip legacy blocking overlay — only the native Chrome share picker should appear. */
if (typeof document !== 'undefined') {
  (function stripLegacyProctorScreenShareOverlay() {
    const overlayId = 'risebird-proctor-screen-share-overlay';
    function remove() {
      document.getElementById(overlayId)?.remove();
      document.querySelectorAll(`#${overlayId}`).forEach((el) => el.remove());
    }
    remove();
    const root = document.documentElement || document.body;
    if (root) new MutationObserver(remove).observe(root, { childList: true, subtree: true });
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', remove, { once: true });
    }
  })();
}
