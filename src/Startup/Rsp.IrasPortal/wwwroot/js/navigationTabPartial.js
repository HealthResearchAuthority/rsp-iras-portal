(function () {
    // Prevent double-initialization if this script is included multiple times
    if (window.__navTabsInitialized) return;
    window.__navTabsInitialized = true;

    /**
     * Build a per-tab scroll cache key tied to current path and activeTabId.
     * This ensures each tab can have its own remembered scroll position.
     */
    function buildScrollKey(urlStr) {
        const u = new URL(urlStr, window.location.origin);
        const tab = u.searchParams.get('activeTabId') || 'default';
        return `scroll:${u.pathname}?tab=${tab}`;
    }

    /**
     * Visually mark the clicked tab as active by toggling the GOV.UK class.
     */
    function setActiveTab(linkEl) {
        const list = linkEl.closest('.govuk-service-navigation__list');
        if (!list) return;
        list
            .querySelectorAll('.govuk-service-navigation__item')
            .forEach(li => li.classList.remove('govuk-service-navigation__item--active'));

        const li = linkEl.closest('.govuk-service-navigation__item');
        if (li) li.classList.add('govuk-service-navigation__item--active');
    }

    /**
     * Fetch the full HTML for the target URL, parse it, and swap only the
     * #tab-content-root from the fetched document into the current page.
     * Also updates URL and restores per-tab scroll position.
     */
    async function fetchAndSwap(url, linkEl) {
        // Save current scroll position for current tab before navigating
        const prevKey = buildScrollKey(window.location.href);
        sessionStorage.setItem(prevKey, String(window.scrollY));

        // Fetch full HTML (same-origin credentials for cookie-based auth)
        const res = await fetch(url, { credentials: 'same-origin' });

        // If the backend responds with error (e.g., 403 for unauthorized Comments),
        // fallback to a normal navigation so server can redirect properly.
        if (!res.ok) throw new Error(`Fetch failed: ${res.status}`);

        const html = await res.text();

        // Parse the incoming HTML and extract new #tab-content-root
        const parser = new DOMParser();
        const doc = parser.parseFromString(html, 'text/html');
        const newRoot = doc.querySelector('#tab-content-root');
        const container = document.querySelector('#tab-content-root');

        // If the container is missing, do a full navigation
        if (!newRoot || !container) {
            window.location.href = url;
            return;
        }

        // Replace panel content
        container.innerHTML = newRoot.innerHTML;

        // Update the URL in the address bar without reloading
        window.history.pushState({}, '', url);

        // Update the active tab styling
        setActiveTab(linkEl);

        // Restore scroll if previously cached for the destination tab
        const nextKey = buildScrollKey(url);
        const targetY = Number(sessionStorage.getItem(nextKey) || 0);

        // Wait until layout is tall enough to scroll to the target position.
        // This prevents premature scrolling before images/tables/partials have rendered.
        let tries = 0;
        const maxTries = 30;     // ~3s total with 100ms intervals
        const threshold = 50;    // safety margin so we don't require exact height

        const tryRestore = () => {
            tries++;
            const docHeight = document.documentElement.scrollHeight;
            if (docHeight > targetY + threshold || tries >= maxTries) {
                window.scrollTo({ top: targetY, behavior: 'auto' });
            } else {
                setTimeout(tryRestore, 100);
            }
        };
        requestAnimationFrame(tryRestore);
    }

    /**
     * Click interception (progressive enhancement):
     * - If user uses Ctrl/Meta/Shift/middle-click, let the browser handle it.
     * - Otherwise, prevent default and do a mini-SPA fetch & swap.
     */
    document.addEventListener('click', function (e) {
        const a = e.target.closest('a.govuk-service-navigation__link[data-tab]');
        if (!a) return;

        // Allow opening in new tab/window or middle-click
        if (e.ctrlKey || e.metaKey || e.shiftKey || e.button !== 0) return;

        e.preventDefault();

        const url = a.href;

        // Attempt SPA-like swap; fallback to full navigation on any error.
        fetchAndSwap(url, a).catch(() => {
            window.location.href = url;
        });
    });

    /**
     * Handle Back/Forward navigation:
     * When user uses the browser history, fetch the page for the current URL and
     * swap the tab content to match the address bar (preserving the SPA feel).
     */
    window.addEventListener('popstate', function () {
        const candidateLink =
            // Try to find the exact link for the current URL
            document.querySelector(`a.govuk-service-navigation__link[data-tab][href="${window.location.href}"]`) ||
            // Or just pick the first tab link as a fallback for styling
            document.querySelector('a.govuk-service-navigation__link[data-tab]') ||
            // Dummy element to avoid null checks
            document.createElement('a');

        fetchAndSwap(window.location.href, candidateLink).catch(() => {
            // If swapping fails, fall back to a full reload
            window.location.reload();
        });
    });

    /**
     * Disable automatic scroll restoration from the browser and manage it ourselves.
     * This ensures consistent behavior when swapping DOM content without full reloads.
     */
    if ('scrollRestoration' in window.history) {
        window.history.scrollRestoration = 'manual';
    }
})();