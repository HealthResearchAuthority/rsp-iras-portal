/// <reference path="../lib/jquery/dist/jquery.js" />

// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.
$(function () {
    function toggleConditionalField() {
        // Hide all conditional fields first
        $(".conditional-field").hide();

        // Get selected roles in lowercase
        let selectedRoles = [];
        $("input:checked[name^='UserRoles']").each(function () {
            const labelText = $("label[for='" + $(this).attr('id') + "']").text().trim().toLowerCase();
            selectedRoles.push(labelText);
        });

        // Show any conditional field where data-role contains one of the selected roles
        $(".conditional-field").each(function () {
            const rolesAttr = $(this).data("role"); // already lowercase in HTML
            if (!rolesAttr) return;

            const rolesList = rolesAttr.split(",").map(r => r.trim());
            if (rolesList.some(r => selectedRoles.includes(r))) {
                $(this).show();
            }
        });
    }

    // Run on page load
    toggleConditionalField();

    // Run on change
    $("input[type='checkbox'][name^='UserRoles']").on('change', toggleConditionalField);
});


function submitFormWithAction(formId, url) {
    let form = document.getElementById(formId);
    form.action = url;
    form.submit();
}

document.addEventListener("DOMContentLoaded", function () {
    initCheckboxCount("Search.Country", "country-hint");
    initCheckboxCount("Search.ReviewBodies", "reviewbody-hint");
    initCheckboxCount("Search.UserRoles", "userroles-hint");
});
