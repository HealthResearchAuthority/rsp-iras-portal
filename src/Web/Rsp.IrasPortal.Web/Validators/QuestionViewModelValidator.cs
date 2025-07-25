﻿using FluentValidation;
using FluentValidation.Results;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class QuestionViewModelValidator : QuestionViewModelValidatorBase
{
    private List<QuestionViewModel> _questions = [];

    protected override bool PreValidate(ValidationContext<QuestionViewModel> context, ValidationResult result)
    {
        _questions = (context.RootContextData["questions"] as List<QuestionViewModel>) ?? [];
        return base.PreValidate(context, result);
    }

    public QuestionViewModelValidator()
    {
        When(x => x.IsMandatory || IsRuleApplicable(x), () =>
        {
            RuleFor(x => x.AnswerText)
                .NotEmpty()
                .When(x => x.DataType is "Date" or "Email" or "Text")
                .WithMessage(GetValidationMessage)
                .DependentRules(() =>
                {
                    ConfigureLengthRule();
                    ConfigureRegExRule();
                    ConfigureDateRule();
                });

            RuleFor(x => x.Answers)
                .Must(ans => ans.Exists(a => a.IsSelected))
                .When(x => x.DataType == "Checkbox")
                .WithMessage(GetValidationMessage);

            RuleFor(x => x.SelectedOption)
                .Must(option => !string.IsNullOrWhiteSpace(option))
                .When(x => x.DataType is "Boolean" or "Radio button" or "Dropdown")
                .WithMessage(GetValidationMessage);
        });
    }

    /// <summary>
    /// Checks if rule is applicable or not
    /// </summary>
    /// <param name="question">Question instance the rule applies to</param>
    private bool IsRuleApplicable(QuestionViewModel question)
    {
        // initialise evaluations dictionary
        // to store the rules evaluation results
        // to process them later to deicde
        // if the rule is applicable
        var evaluations = new Dictionary<string, List<bool>>
        {
            { "AND", [] },
            { "OR", [] }
        };

        // get the rules where
        // ParentQuestionId is not null
        // order by sequence
        // Rules that are not dependent on a parent question e.g. length, regex
        // are applied as DependentRules on the question itself for the property
        var rules = question.Rules
                        .Where(r => r.ParentQuestionId != null)
                        .OrderBy(r => r.Sequence);

        // group the rules into AND and OR rules
        // processing the AND rules first
        foreach (var ruleGroup in rules.ToLookup(r => r.Mode))
        {
            foreach (var rule in ruleGroup)
            {
                // evaluate the rule, this will evaluate
                // all of the conditions wihin the rule
                var evaluationResult = EvaluateRule(rule);

                // build evaluation dictionary

                // store the evaluation result for the rule in the group
                // and continue processing more rules
                evaluations[ruleGroup.Key].Add(evaluationResult);
            }
        }

        // now process all the results to see if
        // rule is applicable
        return ProcessEvaluations(evaluations);
    }

    /// <summary>
    /// Evaluates the conditions within the rules
    /// </summary>
    /// <param name="rule"><see cref="RuleDto"/></param>
    private bool EvaluateRule(RuleDto rule)
    {
        // find the parent question
        var question = _questions.Find(q => q.QuestionId == rule.ParentQuestionId);

        // if question is null there is no rule to apply
        if (question == null)
        {
            return false;
        }

        // initialise evaluations dictionary
        // to store the conditions evaluation results
        // to process them later to deicde
        // if the condition is applicable
        var evaluations = new Dictionary<string, List<bool>>
        {
            { "AND", [] },
            { "OR", [] }
        };

        // get the conditions for the IN
        // Operator. This is sufficient to
        // check equality, or single or multiple options
        // are selected. For NOT IN, we can simply negate
        // other conditions e.g. length, regex
        // are applied as DependentRules on the question itself for the property
        var conditions = rule.Conditions.Where(c => c.Operator == "IN");

        // group the condition into AND and OR conditions
        // processing the AND conditions first
        foreach (var conditionGroup in conditions.ToLookup(c => c.Mode))
        {
            foreach (var condition in conditionGroup)
            {
                // evaluate the condition
                var (EvaluationResult, NoSelection) = EvaluateCondition(condition, question);

                // do not negate if the condition is not satisfied
                // because of no selection for the parent question
                if (!NoSelection)
                {
                    // negates the condition using ^= operator if condition.Negate is true
                    EvaluationResult ^= condition.Negate;
                }

                condition.IsApplicable = EvaluationResult;

                // build evaluation dictionary

                // store the evaluation result for the condition in the group
                // and continue processing more conditions
                evaluations[conditionGroup.Key].Add(EvaluationResult);
            }
        }

        // now process all the results to see if
        // condition is applicable
        return ProcessEvaluations(evaluations);
    }

    /// <summary>
    /// Evaluates the condition if it applies to the conditional question
    /// based on the answers of the parent question
    /// </summary>
    /// <param name="condition"><see cref="ConditionDto"/></param>
    /// <param name="question"><see cref="QuestionViewModel"/></param>
    private static (bool EvaluationResult, bool NoSelection) EvaluateCondition(ConditionDto condition, QuestionViewModel question)
    {
        switch (condition.Operator)
        {
            // condition operator is IN, we need to check
            // if parentQuestion selected option are in
            // the condition's parentoptions
            case "IN":

                // if question data type is boolean or radio
                // only one option can be selected
                if (question.DataType is "Boolean" or "Radio button")
                {
                    // if no selection has been made
                    // condition is not satisfied
                    if (string.IsNullOrWhiteSpace(question.SelectedOption))
                    {
                        return (false, true);
                    }

                    // check if the selected option exists with in the ParentOptions specified in the condition
                    return (condition.ParentOptions.Exists(opt => opt == question.SelectedOption), false);
                }

                // if question data type is checkbox
                // one or more options can be selected
                if (question.DataType is "Checkbox")
                {
                    var selectedAnswers = question.Answers.Where(a => a.IsSelected).Select(a => a.AnswerId);

                    // if no selection has been made
                    // condition is not satisfied
                    if (!selectedAnswers.Any())
                    {
                        return (false, true);
                    }

                    // check if the selectedoptions exists with in the parentoptions specified in the condition
                    // by performing the intersection
                    var selectedOptions = condition.ParentOptions.Intersect(selectedAnswers);

                    // only single option should be selected
                    if (condition.OptionType == "Single")
                    {
                        return (selectedAnswers.Count() == 1 && selectedOptions.Count() == 1, false);
                    }

                    if (condition.OptionType == "Exact")
                    {
                        // the count should be euqal to the selected option
                        // means all of the sepcified options exist
                        return (selectedOptions.Count() == selectedAnswers.Count(), false);
                    }

                    return (selectedOptions.Any(), false);
                }
                break;

            default:
                return (false, false);
        }

        return (false, false);
    }

    private static bool ProcessEvaluations(Dictionary<string, List<bool>> evaluations)
    {
        var andGroupEvaluation = evaluations["AND"];
        var orGroupEvaluation = evaluations["OR"];

        // condition is not satisfied
        // rule/condition is not applicable
        if (andGroupEvaluation.Count == 0 && orGroupEvaluation.Count == 0)
        {
            return false;
        }

        // both group did not satisfy the condition
        // rule/condition won't be apply
        if (andGroupEvaluation.TrueForAll(item => !item) &&
            orGroupEvaluation.TrueForAll(item => !item))
        {
            return false;
        }

        // AND group satisfied the conditions
        // or OR group has a satisfied condition
        // rule/condition will apply
        if (andGroupEvaluation.TrueForAll(item => item))
        {
            return true;
        }

        // This is a slightly exceptional case
        // where there more than one AND conditions and one of
        // them was satisfied but none of the OR conditions
        // were satisfied, in that case rule/condition will apply

#pragma warning disable S1135
        // TODO: We need to look into this later
#pragma warning disable S125
        // if ((andGroupEvaluation.Count > 1 && andGroupEvaluation.Contains(true)) &&
        //    (orGroupEvaluation.Count > 0 && !orGroupEvaluation.Contains(true)))
        // {
        //    return true;
        // }
#pragma warning restore S125
#pragma warning restore S1135

        // one or more OR conditions were satisfied
        // rule/condition will apply
        if (orGroupEvaluation.Contains(true))
        {
            return true;
        }

        // any other scenario
        // rule/condition won't apply
        return false;
    }
}