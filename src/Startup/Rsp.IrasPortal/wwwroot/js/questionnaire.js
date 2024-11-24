/// <reference path="../lib/jquery/dist/jquery.js" />
/// <reference path="../js/rules.js" />

// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// questionnaire.js

$(function () {
    // Hide all conditional questions initially
    $(".conditional").hide();

    $(".conditional").each(function (_, conditionalElement) {
        const $conditionalElement = $(conditionalElement);
        const questionId = $conditionalElement.data("questionid");
        const parentQuestions = $conditionalElement.data("parents");

        // Skip processing if questionId or parentQuestions is missing
        if (!questionId || !parentQuestions) return;

        const parentQuestionIds = parentQuestions.split(',');

        // Set up event listeners for parent questions
        parentQuestionIds.forEach(function (parentQuestionId) {
            const $parentInputs = $(`input[id^=${parentQuestionId}]`);

            // Evaluate rules initially and set visibility
            updateConditionalVisibility(questionId, $conditionalElement);

            // Add event listener to update visibility on parent input changes
            $parentInputs.on("change", function () {
                console.log(`Evaluating rules for ${questionId}`);
                updateConditionalVisibility(questionId, $conditionalElement);
            });
        });
    });
});

/**
 * Updates the visibility of a conditional question based on rule evaluation.
 * @param {string} questionId - The ID of the conditional question.
 * @param {jQuery} $conditionalElement - The jQuery object for the conditional question element.
 */
function updateConditionalVisibility(questionId, $conditionalElement) {
    const isApplicable = isRuleApplicable(questionId);
    const scrollPosition = $(window).scrollTop();

    if (isApplicable) {
        $conditionalElement.slideDown();
        $(`#${questionId}_guide`).slideDown();
    } else {
        $conditionalElement.slideUp().find(":text").val("");
        $conditionalElement.slideUp().find(":radio", ":checkbox").prop("checked", false).trigger("change");
        $(`#${questionId}_guide`).slideUp();
    }

    // Maintain the current scroll position
    $(window).scrollTop(scrollPosition);
}