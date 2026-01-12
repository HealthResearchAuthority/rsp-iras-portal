/// <reference path="../lib/jquery/dist/jquery.js" />

$(function () {
    const KEY_PREFIX = 'selectAllModifications:page=';
    const onTaskListPage = window.location.pathname.toLowerCase().includes('/modificationstasklist');
    if (!onTaskListPage) return;

    // Derive current page number (?pageNumber= or ?page=), default 1
    const params = new URLSearchParams(window.location.search);
    let pageNumber = parseInt(params.get('pageNumber') || params.get('page') || '1', 10);
    if (!Number.isFinite(pageNumber) || pageNumber < 1) pageNumber = 1;

    const STORAGE_KEY = `${KEY_PREFIX}${pageNumber}`;
    const $selectAll = $('#select-all-modifications');
    if (!$selectAll.length) return;

    function clearAllSelectionState() {
        // Remove all persisted "select all" states across pages
        for (let i = sessionStorage.length - 1; i >= 0; i--) {
            const k = sessionStorage.key(i);
            if (k && k.startsWith(KEY_PREFIX)) {
                sessionStorage.removeItem(k);
            }
        }
        // Uncheck everything currently on the page
        $selectAll.prop('checked', false);
        $('.child-checkbox').prop('checked', false);
    }

    function applyState() {
        const saved = sessionStorage.getItem(STORAGE_KEY);
        const isChecked = saved === 'true';
        if (isChecked) {
            $selectAll.prop('checked', true);
            $('.child-checkbox').prop('checked', true);
        } else {
            // clean up stale key if present
            sessionStorage.removeItem(STORAGE_KEY);
        }
    }

    applyState();

    window.addEventListener('pageshow', function () {
        applyState();
    });

    // Maintain select-all state within a page until explicitly cleared
    $(document).on('change', '#select-all-modifications', function () {
        const isChecked = this.checked;
        sessionStorage.setItem(STORAGE_KEY, String(isChecked));
        $('.child-checkbox').prop('checked', isChecked);
    });

    $(document).on('change', '.child-checkbox', function () {
        const $children = $('.child-checkbox');
        const allChecked = $children.length > 0 && $children.filter(':checked').length === $children.length;
        $selectAll.prop('checked', allChecked);
        sessionStorage.setItem(STORAGE_KEY, String(allChecked));
    });

    $(document).on('click', '.js-clear-select-all', function () {
        clearAllSelectionState();
    });

    $(document).on('click', 'a[href*="sort="], a[href*="Sort="], a[href*="order="], .js-sort-link', function () {
        clearAllSelectionState();
    });
});