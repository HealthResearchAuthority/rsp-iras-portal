/// <summary>
/// Checks if rule is applicable or not
/// </summary>
/// <param name="question">Question instance the rule applies to</param>
function isRuleApplicable(questionId) {
    // initialise evaluations dictionary
    // to store the rules evaluation results
    // to process them later to decide
    // if the rule is applicable
    let evaluations = {
        "AND": [],
        "OR": []
    };

    // get the rules where
    // ParentQuestionId is not null
    // order by sequence
    // Rules that are not dependent on a parent question e.g. length, regex
    // are applied as DependentRules on the question itself for the property
    let rules = JSON.parse(localStorage.getItem(questionId))
        .filter(r => r.ParentQuestionId !== null)
        .sort((a, b) => a.Sequence - b.Sequence);

    // group the rules into AND and OR rules
    // processing the AND rules first
    let ruleGroups = groupBy(rules, 'Mode');
    $.each(ruleGroups, function (mode, ruleGroup) {
        $.each(ruleGroup, function (index, rule) {
            // evaluate the rule, this will evaluate
            // all of the conditions within the rule
            let evaluationResult = evaluateRule(rule);
            // build evaluation dictionary
            // store the evaluation result for the rule in the group
            // and continue processing more rules
            evaluations[mode].push(evaluationResult);
        });
    });

    // now process all the results to see if
    // rule is applicable
    return processEvaluations(evaluations);
}

/// <summary>
/// Evaluates the conditions within the rules
/// </summary>
/// <param name="rule"><see cref="RuleDto"/></param>
function evaluateRule(rule) {
    // find the parent question
    //let question = _questions.find(function (q) {
    //    return q.QuestionId === rule.ParentQuestionId;
    //});

    // if question is null there is no rule to apply
    //if (!question) {
    //    return false;
    //}

    // initialise evaluations dictionary
    // to store the conditions evaluation results
    // to process them later to decide
    // if the condition is applicable
    let evaluations = {
        "AND": [],
        "OR": []
    };

    // get the conditions for the IN
    // Operator. This is sufficient to
    // check equality, or single or multiple options
    // are selected. For NOT IN, we can simply negate
    // other conditions e.g. length, regex
    // are applied as DependentRules on the question itself for the property
    let conditions = rule.Conditions.filter(c => c.Operator === "IN");

    // group the condition into AND and OR conditions
    // processing the AND conditions first
    let conditionGroups = groupBy(conditions, 'Mode');
    $.each(conditionGroups, function (mode, conditionGroup) {
        $.each(conditionGroup, function (index, condition) {
            // evaluate the condition
            let evaluationResult = evaluateCondition(condition, question);
            // do not negate if the condition is not satisfied
            // because of no selection for the parent question
            if (!evaluationResult.NoSelection) {
                // negates the condition using ^= operator if condition.Negate is true
                evaluationResult.EvaluationResult ^= condition.Negate;
            }
            condition.IsApplicable = evaluationResult.EvaluationResult;
            // build evaluation dictionary
            // store the evaluation result for the condition in the group
            // and continue processing more conditions
            evaluations[mode].push(evaluationResult.EvaluationResult);
        });
    });

    // now process all the results to see if
    // condition is applicable
    return processEvaluations(evaluations);
}

/// <summary>
/// Evaluates the condition if it applies to the conditional question
/// based on the answers of the parent question
/// </summary>
/// <param name="condition"><see cref="ConditionDto"/></param>
/// <param name="question"><see cref="QuestionViewModel"/></param>
function evaluateCondition(condition, question) {
    switch (condition.Operator) {
        // condition operator is IN, we need to check
        // if parentQuestion selected option are in
        // the condition's parentoptions
        case "IN":
            // if question data type is boolean or radio
            // only one option can be selected
            if (question.DataType === "Boolean" || question.DataType === "Radio button") {
                // if no selection has been made
                // condition is not satisfied
                if (!question.SelectedOption) {
                    return { EvaluationResult: false, NoSelection: true };
                }
                // check if the selected option exists within the ParentOptions specified in the condition
                return { EvaluationResult: condition.ParentOptions.includes(question.SelectedOption), NoSelection: false };
            }
            // if question data type is checkbox
            // one or more options can be selected
            if (question.DataType === "Checkbox") {
                let selectedAnswers = question.Answers.filter(function (a) {
                    return a.IsSelected;
                }).map(function (a) {
                    return a.AnswerId;
                });
                // if no selection has been made
                // condition is not satisfied
                if (!selectedAnswers.length) {
                    return { EvaluationResult: false, NoSelection: true };
                }
                // check if the selected options exist within the parent options specified in the condition
                // by performing the intersection
                let selectedOptions = condition.ParentOptions.filter(function (opt) {
                    return selectedAnswers.includes(opt);
                });
                // only single option should be selected
                if (condition.OptionType === "Single") {
                    return { EvaluationResult: selectedAnswers.length === 1 && selectedOptions.length === 1, NoSelection: false };
                }
                if (condition.OptionType === "Exact") {
                    // the count should be equal to the selected option
                    // means all of the specified options exist
                    return { EvaluationResult: selectedOptions.length === selectedAnswers.length, NoSelection: false };
                }
                return { EvaluationResult: selectedOptions.length > 0, NoSelection: false };
            }
            break;
        default:
            return { EvaluationResult: false, NoSelection: false };
    }
    return { EvaluationResult: false, NoSelection: false };
}

function processEvaluations(evaluations) {
    let andGroupEvaluation = evaluations["AND"];
    let orGroupEvaluation = evaluations["OR"];
    // condition is not satisfied
    // rule/condition is not applicable
    if (andGroupEvaluation.length === 0 && orGroupEvaluation.length === 0) {
        return false;
    }
    // both group did not satisfy the condition
    // rule/condition won't be apply
    if (andGroupEvaluation.every(function (item) {
        return !item;
    }) && orGroupEvaluation.every(function (item) {
        return !item;
    })) {
        return false;
    }
    // AND group satisfied the conditions
    // or OR group has a satisfied condition
    // rule/condition will apply
    if (andGroupEvaluation.every(function (item) {
        return item;
    })) {
        return true;
    }
}