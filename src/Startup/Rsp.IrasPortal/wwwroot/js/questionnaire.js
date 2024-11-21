/// <reference path="../lib/jquery/dist/jquery.js" />
/// <reference path="../js/rules.js" />

// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// questionnaire.js

$(function () {
    // Hide all conditional questions initially
    $(".conditional").hide()

    // Show conditional questions based on parent question selection
    $(".conditional").each(function (index, element) {
        let data = $(this).data("parents");

        if (data === undefined) {
            return;
        }

        let parents = data.split(',');
        let questionId = $(this).data("questionid");

        parents.forEach(function (parent) {
            $("input[id^=" + parent + "]").on("change", function () {
                console.log("Evaluating rules for " + questionId);

                let isRuleApplicable = true;

                if (isRuleApplicable) {
                    $(element).slideDown(500);
                }
            })
        })
    });

    //$('input[type="radio"], input[type="checkbox"]').on("change", function () {
    //    let parentQuestionId = $(this).data('parent-question-id');
    //    let selectedValue = $(this).val();

    //    // Hide all conditional questions related to this parent question
    //    $('.conditional-question[data-parent-question-id="' + parentQuestionId + '"]').hide();

    //    // Show the relevant conditional question
    //    $('.conditional-question[data-parent-question-id="' + parentQuestionId + '"][data-parent-answer="' + selectedValue + '"]').show();
    //});
});

//$(function () {
//    // On load, check the initial state of each question to apply conditional visibility
//    $('.conditional').each(function () {
//        const parentSelector = $(this).data('parent');
//        const showOnValue = $(this).data('show-on');
//        const parentValue = $('#' + parentSelector).val();

//        if (parentValue === showOnValue) {
//            $(this).show();
//        }
//    });

//    // Add event listener for changes on each parent question
//    $('[data-show-on]').each(function () {
//        const parentSelector = $(this).data('parent');
//        const showOnValue = $(this).data('show-on');
//        const conditionalElement = $(this);

//        $('#' + parentSelector).on('change', function () {
//            const selectedValue = $(this).val();

//            if (selectedValue === showOnValue) {
//                conditionalElement.slideDown(); // Show with animation
//            } else {
//                conditionalElement.slideUp(); // Hide with animation
//            }
//        });
//    });
//});