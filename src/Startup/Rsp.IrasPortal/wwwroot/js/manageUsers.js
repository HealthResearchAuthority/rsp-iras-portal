/// <reference path="../lib/jquery/dist/jquery.js" />

// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.
$(function () {
    function toggleConditionalField() {
        let showConditionalFields = false;

        // get all the checked checkboxes with name starting with UserRoles
        $("input:checked[name^='UserRoles']").each(function () {
            // get the label for the checkbox
            const label = $("label[for='" + $(this).attr('id') + "']");

            // check if the label text is 'Operations'
            if (label.text() === 'operations') {
                $(".conditional-field").show();
                showConditionalFields = true;
                return false;
            }
        });

        // if no checkbox with label 'Operations' is checked, hide the conditional field
        if (!showConditionalFields) {
            $(".conditional-field").hide();
        }
    }

    // Initial check on page load
    toggleConditionalField();

    // Bind change event to all checkboxes with name starting with UserRoles
    $("input[type='checkbox'][name^='UserRoles']").on('change', function () {
        toggleConditionalField();
    });
});

function submitFormWithAction(formId, url) {
    let form = document.getElementById(formId);
    form.action = url;
    form.submit();
}