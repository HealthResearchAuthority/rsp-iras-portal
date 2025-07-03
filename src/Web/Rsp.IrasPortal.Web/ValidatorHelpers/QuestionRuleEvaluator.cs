using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.ValidatorHelpers;

/// <summary>
/// Helper class to evaluate rules and conditions for dynamic question rendering.
/// </summary>
public static class QuestionRuleEvaluator
{
    /// <summary>
    /// Determines if a rule is applicable for a question based on its dependencies.
    /// </summary>
    public static bool IsRuleApplicable(QuestionViewModel question, List<QuestionViewModel> allQuestions)
    {
        var evaluations = new Dictionary<string, List<bool>> { { "AND", [] }, { "OR", [] } };

        var rules = question.Rules
            .Where(r => r.ParentQuestionId != null)
            .OrderBy(r => r.Sequence);

        foreach (var ruleGroup in rules.ToLookup(r => r.Mode))
        {
            foreach (var rule in ruleGroup)
            {
                var evaluationResult = EvaluateRule(rule, allQuestions);
                evaluations[ruleGroup.Key].Add(evaluationResult);
            }
        }

        return ProcessEvaluations(evaluations);
    }

    /// <summary>
    /// Determines if a question should have its answers cleared due to unmet parent rule conditions.
    /// Use this to conditionally reset a question's answer fields when its visibility or relevance depends on another question.
    /// </summary>
    /// <param name="question">The current question to evaluate.</param>
    /// <param name="allQuestions">The complete list of all questions in the form.</param>
    /// <returns>True if the question should be reset (i.e., its rules are not applicable); otherwise, false.</returns>
    public static bool ShouldResetQuestionAnswers(QuestionViewModel question, List<QuestionViewModel> allQuestions)
    {
        // If the question has no parent-based rules, it doesn't need to be conditionally reset
        if (!question.Rules.Any(r => r.ParentQuestionId != null))
            return false;

        // If parent conditions are not met, the question is not applicable — return true to indicate reset is needed
        if (!IsRuleApplicable(question, allQuestions))
        {
            return true;
        }

        // Parent conditions are met; no reset required
        return false;
    }

    private static bool EvaluateRule(RuleDto rule, List<QuestionViewModel> questions)
    {
        var question = questions.Find(q => q.QuestionId == rule.ParentQuestionId);
        if (question == null) return false;

        var evaluations = new Dictionary<string, List<bool>> { { "AND", [] }, { "OR", [] } };

        var conditions = rule.Conditions.Where(c => c.Operator == "IN");

        foreach (var conditionGroup in conditions.ToLookup(c => c.Mode))
        {
            foreach (var condition in conditionGroup)
            {
                var (EvaluationResult, NoSelection) = EvaluateCondition(condition, question);

                if (!NoSelection)
                {
                    EvaluationResult ^= condition.Negate;
                }

                condition.IsApplicable = EvaluationResult;
                evaluations[conditionGroup.Key].Add(EvaluationResult);
            }
        }

        return ProcessEvaluations(evaluations);
    }

    private static (bool EvaluationResult, bool NoSelection) EvaluateCondition(ConditionDto condition, QuestionViewModel question)
    {
        switch (condition.Operator)
        {
            case "IN":
                if (question.DataType is "Boolean" or "Radio button")
                {
                    if (string.IsNullOrWhiteSpace(question.SelectedOption))
                        return (false, true);

                    return (condition.ParentOptions.Contains(question.SelectedOption), false);
                }

                if (question.DataType is "Checkbox")
                {
                    var selectedAnswers = question.Answers.Where(a => a.IsSelected).Select(a => a.AnswerId).ToList();

                    if (!selectedAnswers.Any())
                        return (false, true);

                    var selectedOptions = condition.ParentOptions.Intersect(selectedAnswers);

                    if (condition.OptionType == "Single")
                        return (selectedAnswers.Count == 1 && selectedOptions.Count() == 1, false);

                    if (condition.OptionType == "Exact")
                        return (selectedOptions.Count() == selectedAnswers.Count(), false);

                    return (selectedOptions.Any(), false);
                }
                break;
        }

        return (false, false);
    }

    private static bool ProcessEvaluations(Dictionary<string, List<bool>> evaluations)
    {
        var andGroup = evaluations["AND"];
        var orGroup = evaluations["OR"];

        if (andGroup.Count == 0 && orGroup.Count == 0)
            return false;

        if (andGroup.TrueForAll(x => !x) && orGroup.TrueForAll(x => !x))
            return false;

        if (andGroup.TrueForAll(x => x))
            return true;

        if (orGroup.Contains(true))
            return true;

        return false;
    }
}