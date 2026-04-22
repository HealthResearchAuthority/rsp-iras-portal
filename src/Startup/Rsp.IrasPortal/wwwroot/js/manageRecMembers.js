/// <reference path="../lib/jquery/dist/jquery.js" />

// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.
$(function () {
    function toggleConditionalField() {
        var isMemberLeft = $('input[name="MemberLeftOrganisation"]:checked').val() === "true";

        if (isMemberLeft) {
            $(".conditional-field").show();
        } else {
            $(".conditional-field").hide();
        }
    }

    // Run on page load
    toggleConditionalField();

    // Run on change
    $('input[name="MemberLeftOrganisation"]').on('change', toggleConditionalField);
});