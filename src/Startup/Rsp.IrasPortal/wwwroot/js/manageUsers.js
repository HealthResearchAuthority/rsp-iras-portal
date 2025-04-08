/// <reference path="../lib/jquery/dist/jquery.js" />

// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.
$(function () {
    function toggleConditionalField() {
        let shouldShow = false;

        $('.govuk-checkboxes__item').each(function () {
            const checkbox = $(this).find('input[type="checkbox"]');
            const labelText = $(this).find('label').text().trim();
            const isChecked = checkbox.is(':checked');

            if (labelText.toLowerCase() === 'operations' && isChecked) {
                shouldShow = true;
            }
        });

        if (shouldShow) {
            $(".conditional-field").show();
        } else {
            $(".conditional-field").hide();
        }
    }

    // Initial check on page load
    toggleConditionalField();

    // Bind change event to all checkboxes
    $('.govuk-checkboxes__input').on('change', function () {
        toggleConditionalField();
    });
});

/**
 * Updates the visibility of a conditional question based on rule evaluation.
 * @param {string} questionId - The ID of the conditional question.
 * @param {jQuery} $conditionalElement - The jQuery object for the conditional question element.
 */

function submitFormWithAction(formId, url) {
    let form = document.getElementById(formId);
    form.action = url;
    form.submit();
}