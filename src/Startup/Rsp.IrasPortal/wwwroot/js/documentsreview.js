(function () {
    function initConditionalRules(conditionalRulesJson) {
        // Save each conditional rule by QuestionId
        conditionalRulesJson.forEach(rule => {
            sessionStorage.setItem(rule.QuestionId, JSON.stringify(rule.Rules));
        });

        // Initially hide all elements with 'conditional' class
        $(".conditional").hide();

        // For each conditional div, check its parent hidden input value
        document.querySelectorAll('div.conditional').forEach(div => {
            const parentId = div.getAttribute('data-parents');
            const questionId = div.getAttribute('data-questionid');

            // Find the closest dl ancestor
            const dl = div.closest('dl');
            const docIndex = dl?.querySelector('input[type="hidden"][name="data-docindex"]');
            const docIndexValue = docIndex?.value;

            const rulesJson = sessionStorage.getItem(parentId);
            const questionRulesJson = sessionStorage.getItem(questionId);

            if (rulesJson && questionRulesJson) {
                const rules = JSON.parse(rulesJson);
                const questionRules = JSON.parse(questionRulesJson);

                // Pick the SelectedOption for this docIndex
                const selectedOption = rules[docIndexValue]?.SelectedOption;

                if (Array.isArray(questionRules) && questionRules.length > 0) {
                    const matches = questionRules.some(rule =>
                        rule.Conditions?.some(cond =>
                            cond.ParentOptions?.includes(selectedOption)
                        )
                    );

                    if (matches) {
                        div.classList.remove("conditional");
                        div.style.display = "";
                    }
                }
            }
        });
    }

    // Expose globally so Razor can call it
    window.conditionalRulesHandler = {
        init: initConditionalRules
    };
})();