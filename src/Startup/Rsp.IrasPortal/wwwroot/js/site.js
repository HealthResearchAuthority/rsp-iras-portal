// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Runs on EVERY page
(function () {
    const onTaskListPage = window.location.pathname.toLowerCase().includes('/modificationstasklist');

    if (onTaskListPage) return;

    // Remove any keys related to select-all-modifications
    for (let i = sessionStorage.length - 1; i >= 0; i--) {
        const key = sessionStorage.key(i);
        if (!key) continue;

        const k = key.toLowerCase();
        if (k.includes('selectallmodifications') || k.includes('select-all-modifications')) {
            sessionStorage.removeItem(key);
        }
    }

    function enableKeyboardActivation(selector) {
        document.querySelectorAll(selector).forEach(el => {
            el.addEventListener('keydown', function (event) {
                if (event.key === 'Enter') {
                    event.preventDefault();
                    el.click();
                }
            });
        });
    }

    // Initialise once DOM is ready
    document.addEventListener('DOMContentLoaded', function () {
        enableKeyboardActivation('.js-key-activatable');
    });
})();