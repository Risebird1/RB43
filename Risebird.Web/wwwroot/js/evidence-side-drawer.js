/**
 * Reusable evidence side drawer with iframe (Completed Interviews Evidence column).
 */
(function (window) {
    'use strict';

    var drawerEl = null;
    var backdropEl = null;
    var titleEl = null;
    var subtitleEl = null;
    var iframeEl = null;

    function ensureElements() {
        if (drawerEl) return;
        backdropEl = document.getElementById('ivEvidenceDrawerBackdrop');
        drawerEl = document.getElementById('ivEvidenceDrawer');
        titleEl = document.getElementById('ivEvidenceDrawerTitle');
        subtitleEl = document.getElementById('ivEvidenceDrawerSubtitle');
        iframeEl = document.getElementById('ivEvidenceDrawerIframe');
        if (!drawerEl || !backdropEl) return;

        backdropEl.addEventListener('click', close);
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && drawerEl.classList.contains('is-open')) {
                close();
            }
        });
        var closeBtn = document.getElementById('ivEvidenceDrawerClose');
        if (closeBtn) closeBtn.addEventListener('click', close);
    }

    function open(config) {
        ensureElements();
        if (!drawerEl || !iframeEl) return;

        var title = (config && config.title) || 'Evidence';
        var subtitle = (config && config.subtitle) || '';
        var url = (config && config.url) || 'about:blank';

        if (titleEl) titleEl.textContent = title;
        if (subtitleEl) subtitleEl.textContent = subtitle;

        if (window.innerWidth < 900 && config && config.openInNewTabOnMobile) {
            window.open(url, '_blank', 'noopener');
            return;
        }

        iframeEl.src = 'about:blank';
        backdropEl.classList.add('is-open');
        drawerEl.classList.add('is-open');
        backdropEl.setAttribute('aria-hidden', 'false');
        drawerEl.setAttribute('aria-hidden', 'false');
        document.body.style.overflow = 'hidden';
        iframeEl.src = url;
    }

    function close() {
        ensureElements();
        if (!drawerEl) return;
        backdropEl.classList.remove('is-open');
        drawerEl.classList.remove('is-open');
        backdropEl.setAttribute('aria-hidden', 'true');
        drawerEl.setAttribute('aria-hidden', 'true');
        document.body.style.overflow = '';
        if (iframeEl) iframeEl.src = 'about:blank';
    }

    window.EvidenceSideDrawer = {
        open: open,
        close: close
    };
})(window);
