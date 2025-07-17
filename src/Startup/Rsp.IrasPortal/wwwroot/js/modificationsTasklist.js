/// <reference path="../lib/jquery/dist/jquery.js" />

// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

$(function () {
    // Toggle all checkboxes when master is changed
    $('#select-all-modifications').on('change', function () {
        $('.child-checkbox').prop('checked', this.checked);
    });

    // Optional: Sync master checkbox when any child changes
    $('.child-checkbox').on('change', function () {
        const all = $('.child-checkbox').length;
        const checked = $('.child-checkbox:checked').length;
        $('#select-all-modifications').prop('checked', all === checked);
    });
});