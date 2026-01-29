/// <reference path="../lib/jquery/dist/jquery.js" />

$(function () {
    const scrollKey = 'scroll:' + location.pathname;

    // 1) Save scroll when a tab link is clicked
    $('a.govuk-service-navigation__link[data-tab]').on('click', function () {
        let y = $(window).scrollTop();
        localStorage.setItem(scrollKey, String(y));
    });

    // 2) Safety net: also save before unload (covers refresh or non-tab navigation)
    $(window).on('beforeunload', function () {
        let y = $(window).scrollTop();
        localStorage.setItem(scrollKey, String(y));
    });

    // 3) Restore on load
    let y = Number(localStorage.getItem(scrollKey) || 0);

    if (y > 0) {
        $(window).scrollTop(y);
    }
});