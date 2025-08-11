/// <reference path="../lib/jquery/dist/jquery.js" />

$(function () {
    const STORAGE_KEY = 'selectAllModifications';
    const $selectAll = $('#select-all-modifications');

    if (!$selectAll.length) return;

    function applyState() {
        const saved = sessionStorage.getItem(STORAGE_KEY);
        const isChecked = saved === 'true';
        $selectAll.prop('checked', isChecked);
        // Re-select children each time in case pagination re-rendered them
        const $children = $('.child-checkbox');
        $children.prop('checked', isChecked);
    }

    // Apply on initial load
    applyState();

    // Also apply when navigating back/forward (bfcache)
    window.addEventListener('pageshow', function () {
        applyState();
    });

    // When master changes: apply to all + persist
    $(document).on('change', '#select-all-modifications', function () {
        const isChecked = this.checked;
        sessionStorage.setItem(STORAGE_KEY, String(isChecked));
        const $children = $('.child-checkbox');
        $children.prop('checked', isChecked);
    });

    // When any child changes: update master + persist
    $(document).on('change', '.child-checkbox', function () {
        const $children = $('.child-checkbox');
        const allChecked = $children.length > 0 && $children.filter(':checked').length === $children.length;
        $selectAll.prop('checked', allChecked);
        sessionStorage.setItem(STORAGE_KEY, String(allChecked));
    });

    // Optional: clear the remembered state when you “Clear filters”
    $(document).on('click', '.js-clear-select-all', function () {
        sessionStorage.removeItem(STORAGE_KEY);
    });
});
