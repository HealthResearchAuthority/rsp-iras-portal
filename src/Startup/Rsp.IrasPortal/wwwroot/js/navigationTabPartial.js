/// <reference path="../lib/jquery/dist/jquery.js" />

(function ($) {
    // Prevent double initialization
    if (window.__navTabsInitialized) return;
    window.__navTabsInitialized = true;

    const $win = $(window);
    const $doc = $(document);

    let currentXhr = null;

    /**
     * Shared scroll key per pathname (all tabs share scroll state).
     */
    function buildScrollKey(urlStr) {
        const u = new URL(urlStr, window.location.origin);
        return 'scroll:' + u.pathname;
    }

    /**
     * Save the scroll position for this page.
     */
    function saveScrollFor(urlStr) {
        const key = buildScrollKey(urlStr);
        try {
            sessionStorage.setItem(key, String($win.scrollTop()));
        }
        catch (err) {
            console.debug('scroll save ignored', err);
        }
    }

    /**
     * Restore scroll position, waiting until content height is sufficient.
     */
    function restoreScrollFor(urlStr) {
        const key = buildScrollKey(urlStr);
        const targetY = Number(sessionStorage.getItem(key) || 0);

        let tries = 0;
        const maxTries = 30;
        const threshold = 50;

        function tryRestore() {
            tries++;
            const docHeight = document.documentElement.scrollHeight;

            if (docHeight > targetY + threshold || tries >= maxTries) {
                window.scrollTo({ top: targetY, left: 0, behavior: 'auto' });
            } else {
                setTimeout(tryRestore, 100);
            }
        }

        requestAnimationFrame(tryRestore);
    }

    /**
     * Mark clicked tab as active.
     */
    function setActiveTab($link) {
        if (!$link?.length) {
            return;
        }

        const $list = $link.closest('.govuk-service-navigation__list');
        if (!$list.length) {
            return;
        }

        $list.find('.govuk-service-navigation__item')
            .removeClass('govuk-service-navigation__item--active');

        $link.closest('.govuk-service-navigation__item')
            .addClass('govuk-service-navigation__item--active');
    }

    /**
     * Find link corresponding to current URL (used for popstate).
     */
    function findLinkForUrl(urlStr) {
        const u = new URL(urlStr, window.location.origin);
        const targetTab = u.searchParams.get('activeTabId');

        const $links = $('a.govuk-service-navigation__link[data-tab]');
        if (!targetTab) {
            return $links.first();
        }

        const $match = $links.filter(function () {
            const linkTab = $(this).data('tab');
            return linkTab === targetTab;
        }).first();

        return $match.length ? $match : $links.first();
    }

    /**
     * Reinitialize GOV.UK Frontend & unobtrusive validation.
     */
    function reinitEnhancements($container) {
        if ($?.validator?.unobtrusive) {
            $container.find('form').each(function () {
                $(this)
                    .removeData('validator')
                    .removeData('unobtrusiveValidation');
            });

            $.validator.unobtrusive.parse($container);
        }

        const initAll = window?.GOVUKFrontend?.initAll;
        if (typeof initAll === 'function') {
            initAll($container[0]);
        }
    }

    /**
     * Fetch page HTML, extract tab content, replace it, update URL, restore scroll.
     */
    function fetchAndSwap(urlStr, $linkForStyling) {
        saveScrollFor(window.location.href);

        // Cancel previous AJAX request
        if (typeof currentXhr?.abort === 'function') {
            currentXhr.abort();
        }

        currentXhr = $.ajax({
            url: urlStr,
            method: 'GET',
            xhrFields: { withCredentials: true }
        })
            .done(function (html) {
                const $parsed = $('<div>').append($.parseHTML(html, document, true));
                const $newRoot = $parsed.find('#tab-content-root').first();
                const $container = $('#tab-content-root');

                if (!$newRoot.length || !$container.length) {
                    window.location.href = urlStr;
                    return;
                }

                $container.html($newRoot.html());

                // Update browser address bar without reload
                window.history.pushState({}, '', urlStr);

                // Update tab styling
                setActiveTab($linkForStyling?.length ? $linkForStyling : findLinkForUrl(urlStr));

                // Remove focus to avoid persistent yellow highlight (focus-within)
                $linkForStyling?.blur?.();

                // Re-init client enhancements
                reinitEnhancements($container);

                // Restore scroll
                restoreScrollFor(urlStr);
            })
            .fail(function () {
                window.location.href = urlStr;
            })
            .always(function () {
                currentXhr = null;
            });

        return currentXhr;
    }

    /**
     * Handle tab clicks (SPA-like navigation).
     */
    $doc.on('click', 'a.govuk-service-navigation__link[data-tab]', function (e) {
        if (e.ctrlKey || e.metaKey || e.shiftKey || e.button !== 0) {
            return; // let browser handle new tab, etc.
        }

        e.preventDefault();

        const href = this.href;
        fetchAndSwap(href, $(this));
    });

    /**
     * Browser Back/Forward button support.
     */
    $win.on('popstate', function () {
        const href = window.location.href;
        const $link = findLinkForUrl(href);

        const jqxhr = fetchAndSwap(href, $link);

        jqxhr?.fail?.(() => {
            window.location.reload();
        });
    });

    /**
     * Save scroll before unloading the page.
     */
    $win.on('beforeunload', function () {
        saveScrollFor(window.location.href);
    });

    /**
     * Disable automatic scroll restoration by the browser.
     */
    if ('scrollRestoration' in window.history) {
        window.history.scrollRestoration = 'manual';
    }
})(jQuery);