/// <reference path="../lib/jquery/dist/jquery.js" />

$(function () {
    const KEY_PREFIX = 'selectAllModifications:page=';
    const onTaskListPage = window.location.pathname.toLowerCase().includes('/modificationstasklist');
    if (!onTaskListPage) return; // cleaner handles non-tasklist pages

    // Derive current page number (?pageNumber= or ?page=), default 1
    const params = new URLSearchParams(window.location.search);
    let pageNumber = parseInt(params.get('pageNumber') || params.get('page') || '1', 10);
    if (!Number.isFinite(pageNumber) || pageNumber < 1) pageNumber = 1;

    const STORAGE_KEY = `${KEY_PREFIX}${pageNumber}`;
    const $selectAll = $('#select-all-modifications');
    if (!$selectAll.length) return;

    function applyState() {
        const saved = sessionStorage.getItem(STORAGE_KEY);
        const isChecked = saved === 'true';
        if (isChecked) {
            $selectAll.prop('checked', isChecked);
            $('.child-checkbox').prop('checked', isChecked);
        } else {
            sessionStorage.removeItem(STORAGE_KEY);
        }
    }

    applyState();

    window.addEventListener('pageshow', function () {
        applyState();
    });

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
        sessionStorage.removeItem(STORAGE_KEY);
        $selectAll.prop('checked', false);
        $('.child-checkbox').prop('checked', false);
    });
});