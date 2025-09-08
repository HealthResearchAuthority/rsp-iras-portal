/// <reference path="../lib/jquery/dist/jquery.js" />
/// <reference path="../js/rules.js" />

// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// questionnaire.js
$(function () {
    // Hide all conditional questions initially
    $(".conditional, .conditional-field").hide();

    $(".conditional, .conditional-field").each(function (_, conditionalElement) {
        const $conditionalElement = $(conditionalElement);
        const questionId = $conditionalElement.data("questionid");
        const parentQuestions = $conditionalElement.data("parents");

        // Skip processing if questionId or parentQuestions is missing
        if (!questionId || !parentQuestions) return;

        const parentQuestionIds = parentQuestions.toString().split(',');

        // Set up event listeners for parent questions
        parentQuestionIds.forEach(function (parentQuestionId) {
            // Select both radio button inputs and drop-down lists whose id starts with parentQuestionId
            const $parentInputs = $(`input[id^="${parentQuestionId}"], select[id^="${parentQuestionId}"], div[id^="${parentQuestionId}"] > select`);

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
        // Clear text inputs
        $conditionalElement.find(":text").val("");
        // Uncheck radio buttons and checkboxes
        $conditionalElement.find(":radio, :checkbox").prop("checked", false).trigger("change");
        // Reset drop-down lists to their default (first) option
        $conditionalElement.find("select").prop("selectedIndex", 0).trigger("change");

        $conditionalElement.slideUp();
        $(`#${questionId}_guide`).slideUp();
    }

    // Restore the original scroll position
    $(window).scrollTop(scrollPosition);
}