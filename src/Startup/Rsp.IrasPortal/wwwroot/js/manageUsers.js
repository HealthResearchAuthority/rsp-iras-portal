/// <reference path="../lib/jquery/dist/jquery.js" />

// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

$(function () {
    // Hide all conditional questions initially if the selected role is 'operations'
    let selectedRole = $('#Role').val();

    if (selectedRole != 'operations') {
        $(".conditional-field").hide();
    }
});

/**
 * Updates the visibility of a conditional question based on rule evaluation.
 * @param {string} questionId - The ID of the conditional question.
 * @param {jQuery} $conditionalElement - The jQuery object for the conditional question element.
 */
function changeRoleSelection(role) {
    if (role === 'operations') {
        $(".conditional-field").show();
    } else {
        $(".conditional-field").hide();
    }
}

function changeRoleSelection(role) {
    const hasOperationsRole = role.some(role =>
        role.RoleName === 'operations' && role.IsSelected
    );

    if (hasOperationsRole) {
        $(".conditional-field").show();
    } else {
        $(".conditional-field").hide();
    }
}


function submitFormWithAction(formId, url) {
    let form = document.getElementById(formId);
    form.action = url;
    form.submit();
}