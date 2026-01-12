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
        $(".conditional").hide();

        // Iterate over each conditional div on the page
        document.querySelectorAll('div.conditional').forEach(div => {
            const parentId = div.getAttribute('data-parents');   // Parent question(s) this div depends on
            const questionId = div.getAttribute('data-questionid'); // This question's ID

            // Find the closest <dl> ancestor to determine which document index this div belongs to
            const dl = div.closest('dl');
            const docIndex = dl?.querySelector('input[type="hidden"][name="data-docindex"]');
            const docIndexValue = docIndex?.value; // Index of the document in the model

            // Retrieve stored rules for the parent question and current question
            const rulesJson = sessionStorage.getItem(parentId);
            const questionRulesJson = sessionStorage.getItem(questionId);

            if (rulesJson && questionRulesJson) {
                const rules = JSON.parse(rulesJson);               // Parent's selected options
                const questionRules = JSON.parse(questionRulesJson); // Current question's rules

                // Get the selected option for this parent question for the current doc index
                const selectedOption = rules[docIndexValue]?.SelectedOption;

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
                        div.classList.remove("conditional");
                        div.style.display = "";
                    }
                }
            }
        });
    }

    // Expose the handler globally so Razor pages can call it with JSON data
    window.conditionalRulesHandler = {
        init: initConditionalRules
    };
})();