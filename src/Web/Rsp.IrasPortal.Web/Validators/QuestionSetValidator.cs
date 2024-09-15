using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class QuestionSetValidator : AbstractValidator<QuestionnaireViewModel>
{
    public QuestionSetValidator()
    {
        // Validate all questions in the questionnaire
        RuleForEach(x => x.Questions)
            .SetValidator(new QuestionViewModelValidator());

        //RuleForEach(x => x.Questions)
        //    .ChildRules(question =>
        //    {
        //        //question.When(q => q.IsOptional && q.Rules.Count > 0, () =>
        //        //{
        //        //    question
        //        //        .RuleForEach(q => q.Answers)
        //        //        .ChildRules(answer =>
        //        //        {
        //        //            answer.When(a => a.ParentQuestion.QuestionId)
        //        //        });
        //        //});

        //        question.When(q => q.IsMandatory, () =>
        //        {
        //            question.When(q => q.QuestionType == "Text", () =>
        //            {
        //                question
        //                    .RuleFor(ans => ans.AnswerText)
        //                    .NotEmpty()
        //                    .WithMessage(ans => $"Please provide an answer for question {ans.Heading} under {ans.Section} section");
        //            });

        //            question.When(q => q.QuestionType == "Look-up list" && q.DataType == "Checkbox", () =>
        //            {
        //                question
        //                    .RuleFor(ans => ans.Answers)
        //                    .Must(ans => ans?.Exists(a => a.IsSelected) == true)
        //                    .WithMessage(ans => $"Please select at least one option for question {ans.Heading} under {ans.Section} section");
        //            });

        //            question.When(q => (q.QuestionType == "Look-up list" && q.DataType == "Radio button") || q.DataType == "Boolean", () =>
        //            {
        //                question
        //                    .RuleFor(ans => ans.Answers)
        //                    .Must(ans => ans?.Exists(a => a.IsSelected) == true)
        //                    .WithMessage(ans => $"Please select one option for question {ans.Heading} under {ans.Section} section");
        //            });
        //        });
        //    });

        // Conditional question validation
        //RuleFor(x => x.Questions)
        //    .Must(IsValidConditionalQuestion)
        //    .When(x => !x.IsMandatory)
        //    .WithMessage("The condition for this question is not met based on the parent question answers.");

        //RuleForEach(x => x.Questions)
        //.Must((x, y, z) => IsValidConditionalQuestion(x, y, z))
        //.When(x => !x.IsMandatory)
        //.WithMessage("The condition for this question is not met based on the parent question answers.");
    }

    // Check if conditional question is valid based on its rules
    //private bool IsValidConditionalQuestion(QuestionnaireViewModel instance, List<QuestionViewModel> questions, ValidationContext<QuestionnaireViewModel> context)
    //{
    //    foreach (var rule in question.Rules)
    //    {
    //        var parentQuestion = context.InstanceToValidate // Root instance
    //            .Questions.FirstOrDefault(q => q.QuestionId == rule.ParentQuestionId);

    //        if (parentQuestion != null)
    //        {
    //            // Check if parent question answer meets the rule's condition
    //            var isRuleValid = rule.Condition.ParentOptions.Contains(parentQuestion.AnswerText);

    //            // If the rule is not met, return false (indicating validation failure)
    //            if (!isRuleValid)
    //            {
    //                return false;
    //            }
    //        }
    //    }
    //    return true;
    //}
}