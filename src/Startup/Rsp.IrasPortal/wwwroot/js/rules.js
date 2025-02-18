/// <reference path="../lib/jquery/dist/jquery.js" />

/**
 * Checks if a rule is applicable or not
 * @param {string} questionId - The ID of the conditional question the rule applies to.
 */
function isRuleApplicable(questionId) {
    // Retrieve and filter rules where ParentQuestionId is not null, then sort by sequence
    const rules = JSON.parse(sessionStorage.getItem(questionId))
        .filter(r => r.ParentQuestionId !== null)
        .sort((a, b) => a.Sequence - b.Sequence);

    // Group the rules into AND and OR groups
    const ruleGroups = groupBy(rules, 'Mode');

    // Evaluate all rules in each group
    // and build evaluation dictionary
    // store the evaluation result for the rule in the group
    const evaluations = {
        AND: ruleGroups.AND?.map(evaluateRule) || [],
        OR: ruleGroups.OR?.map(evaluateRule) || [],
    };

    // Determine if the rule is applicable based on evaluations
    return processEvaluations(evaluations);
}

/**
 * Evaluates the conditions within the rule
 * @param {ruleObject} rule - rule object.
 */
function evaluateRule(rule) {
    // Filter the conditions for the IN operator, used to check equality or selection of options
    const conditions = rule.Conditions.filter(c => c.Operator === "IN");

    // Group the conditions into AND and OR groups
    const conditionGroups = groupBy(conditions, 'Mode');

    // Evaluate each group of conditions
    const evaluations = {
        AND: conditionGroups.AND?.map(cond => {
            const evaluation = evaluateCondition(cond, rule.ParentQuestionId);

            // do not negate if the condition is not satisfied
            // because of no selection for the parent question
            // or if Negate is false
            if (evaluation.NoSelection || !cond.Negate) {
                return evaluation.EvaluationResult
            }

            // Apply negation if cond.Negate is true and if NoSelection is false
            return !evaluation.EvaluationResult;
        }) || [],
        OR: conditionGroups.OR?.map(cond => {
            const evaluation = evaluateCondition(cond, rule.ParentQuestionId);

            // do not negate if the condition is not satisfied
            // because of no selection for the parent question
            // or if Negate is false
            if (evaluation.NoSelection || !cond.Negate) {
                return evaluation.EvaluationResult
            }

            // Apply negation if cond.Negate is true and if NoSelection is false
            return !evaluation.EvaluationResult;
        }) || [],
    };

    // Determine if the conditions within the rule are satisfied
    return processEvaluations(evaluations);
}

/**
 * Evaluates the condition to determine if it applies based on the parent question's answers
 * @param {conditionObject} condition - condition object.
 * @param {string} parentQuestionId - parent question Id.
 */
function evaluateCondition(condition, parentQuestionId) {
    // If the operator is unsupported, the condition is not satisfied
    if (condition.Operator !== "IN") {
        return { EvaluationResult: false, NoSelection: false };
    }

    // Gather radio/checkbox inputs whose IDs start with the parentQuestionId
    const radioInputs = $(`input[id^="${parentQuestionId}"]`);
    const selectedRadioAnswers = radioInputs.filter(":checked");

    // Gather drop-down (select) elements whose IDs start with the parentQuestionId
    const selectInputs = $(`select[id^="${parentQuestionId}"]`);

    let selectedIds = [];

    // Process radio/checkbox inputs: extract the answer part from their IDs (format: questionId_answerId)
    selectedRadioAnswers.each(function () {
        const idParts = this.id.split("_");
        if (idParts.length > 1) {
            selectedIds.push(idParts[1]);
        }
    });

    // Process drop-down selections: get the selected value if it exists
    selectInputs.each(function () {
        const val = $(this).val();
        if (val !== "" && val !== null) {
            selectedIds.push(val);
        }
    });

    // If no selection is made across both types, the condition is not satisfied
    if (selectedIds.length === 0) {
        return { EvaluationResult: false, NoSelection: true };
    }

    // Get the matching options by checking if they are included in the parent options specified in the condition
    const matchingOptions = condition.ParentOptions.filter(option => selectedIds.includes(option));

    // Evaluate based on the option type specified in the condition
    switch (condition.OptionType) {
        case "Single":
            // Only one option must be selected and it must match one of the parent options
            return { EvaluationResult: selectedIds.length === 1 && matchingOptions.length === 1, NoSelection: false };
        case "Exact":
            // The count of selected options must match the count of matching parent options
            return { EvaluationResult: matchingOptions.length === selectedIds.length, NoSelection: false };
        default:
            // At least one selected option must match one of the parent options
            return { EvaluationResult: matchingOptions.length > 0, NoSelection: false };
    }
}


/**
 * Processes evaluation results to determine rule applicability
 * @param {Object} evaluations - Object containing AND and OR group evaluations.
 */
function processEvaluations(evaluations) {
    // Extract evaluations for AND and OR groups
    const { AND: andGroupEvaluation, OR: orGroupEvaluation } = evaluations;

    // If both groups are empty, the condition is not satisfied
    if (andGroupEvaluation.length === 0 && orGroupEvaluation.length === 0) {
        return false;
    }

    // If all conditions in both groups are false, the condition is not satisfied
    if (andGroupEvaluation.every(result => !result) && orGroupEvaluation.every(result => !result)) {
        return false;
    }

    // If all AND conditions are true or at least one OR condition is true, the condition is satisfied
    return andGroupEvaluation.every(Boolean) || orGroupEvaluation.some(Boolean);
}

/**
 * Groups an array of objects by a specified key
 * @param {array} array - The array to group.
 * @param {string} key - The key to group by.
 */
function groupBy(array, key) {
    return array.reduce((result, item) => {
        const groupKey = item[key];
        if (!result[groupKey]) {
            result[groupKey] = [];
        }
        result[groupKey].push(item);
        return result;
    }, {});
}