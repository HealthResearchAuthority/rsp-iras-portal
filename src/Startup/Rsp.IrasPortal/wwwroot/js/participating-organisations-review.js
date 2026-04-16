/// <reference path="../lib/jquery/dist/jquery.js" />

(function () {
    /**
     * Initializes conditional rules for questions.
     * @param {Array} conditionalRulesJson - Array of objects containing QuestionId and its Rules
     */
    function initConditionalRules(conditionalRulesJson) {
        // Store each question's conditional rules in sessionStorage keyed by QuestionId
        conditionalRulesJson.forEach(rule => {
            sessionStorage.setItem(rule.QuestionId, JSON.stringify(rule.Rules));
        });

        // Initially hide all elements with the 'conditional' class
        $(".po-conditional").hide();

        // Iterate over each conditional div on the page
        $(".po-conditional").each((_, conditionalElement) => {
            const $conditionalElement = $(conditionalElement);
            const questionId = $conditionalElement.data("questionid");
            const parentId = $conditionalElement.data("parents");

            // Skip processing if questionId or parentId is missing
            if (!questionId || !parentId) return;

            // Find the hidden input to determine which document index this div belongs to
            const orgIndex = $conditionalElement.find('input:hidden[name="data-orgindex"]');
            const orgIndexValue = orgIndex.val(); // Index of the document in the model

            // Retrieve stored rules for the parent question and current question
            const rulesJson = sessionStorage.getItem(parentId);
            const questionRulesJson = sessionStorage.getItem(questionId);

            if (rulesJson && questionRulesJson) {
                const rules = JSON.parse(rulesJson);               // Parent's selected options
                const questionRules = JSON.parse(questionRulesJson); // Current question's rules

                // Get the selected option for this parent question for the current org index
                const selectedOption = rules[orgIndexValue]?.SelectedOption;

                // Only proceed if there are rules defined for this question
                if (Array.isArray(questionRules) && questionRules.length > 0) {
                    // Check if any rule's conditions match the parent's selected option
                    const matches = questionRules.some(rule =>
                        rule.Conditions?.some(cond =>
                            cond.ParentOptions?.includes(selectedOption)
                        )
                    );

                    // If conditions match, show the div and remove the 'conditional' class
                    if (matches) {
                        $conditionalElement.removeClass("po-conditional");
                        $conditionalElement.show();
                    }
                }
            }
        });
    }

    // Expose the handler globally so Razor pages can call it with JSON data
    globalThis.conditionalRulesHandler = {
        init: initConditionalRules
    };
})();