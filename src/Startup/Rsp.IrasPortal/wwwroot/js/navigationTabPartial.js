/// <reference path="../lib/jquery/dist/jquery.js" />

$(function () {
    var scrollKey = 'scroll:' + location.pathname;

    // 1) Save scroll when a tab link is clicked
    $('a.govuk-service-navigation__link[data-tab]').on('click', function () {
        var y = $(window).scrollTop();
        localStorage.setItem(scrollKey, String(y));
    });

    // 2) Safety net: also save before unload (covers refresh or non-tab navigation)
    $(window).on('beforeunload', function () {
        var y = $(window).scrollTop();
        localStorage.setItem(scrollKey, String(y));
    });

    // 3) Restore on load
    var y = Number(localStorage.getItem(scrollKey) || 0);

    if (y > 0) {
        $(window).scrollTop(y);
    }
});