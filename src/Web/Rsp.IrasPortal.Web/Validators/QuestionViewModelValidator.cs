using FluentValidation;
using FluentValidation.Results;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class QuestionViewModelValidator : AbstractValidator<QuestionViewModel>
{
    private List<QuestionViewModel> _questions;

    protected override bool PreValidate(ValidationContext<QuestionViewModel> context, ValidationResult result)
    {
        _questions = context.RootContextData["questions"] as List<QuestionViewModel>;

        return base.PreValidate(context, result);
    }

    public QuestionViewModelValidator()
    {
        // Validate mandatory questions
        RuleFor(x => x.AnswerText)
            .NotEmpty()
            .When(x => x.IsMandatory && x.DataType is "Date" or "Email" or "Text")
            .WithMessage(q => $"Question {q.Heading} under {q.Section} section");

        // Validate answers (apply AnswerViewModelValidator)
        RuleFor(x => x.Answers)
            .Must(ans => ans?.Exists(a => a.IsSelected) == true)
            .When(x => x.IsMandatory && x.DataType is "Boolean" or "Radio button" or "Checkbox")
            //.SetValidator(new AnswerViewModelValidator())
            .WithMessage(q => $"Question {q.Heading} under {q.Section} section");

        When(x => !x.IsMandatory && IsValidConditionalQuestion(x), () =>
        {
            RuleFor(x => x.AnswerText)
                .Must(x => !string.IsNullOrWhiteSpace(x))
                .When(x => x.DataType is "Text" or "Email" or "Date")
                .WithMessage(x => $"Question {x.Heading} under {x.Section} section");

            //.Custom((answer, context) =>
            //{
            //    var question = context.InstanceToValidate;

            //    var (isValid, property) = question.DataType switch
            //    {
            //        "Date" or "Email" or "Text" => (!string.IsNullOrEmpty(answer), $"Questions[{question.Index}].AnswerText"),
            //        _ => (true, "")
            //    };

            //    if (!isValid)
            //    {
            //        context.AddFailure(property, $"Question {question.Heading} under {question.Section} section");
            //    }
            //});

            RuleFor(x => x.Answers)
                .Must(x => x.Exists(a => a.IsSelected))
                .When(x => x.DataType is "Boolean" or "Radio button" or "Checkbox")
                .WithMessage(x => $"Question {x.Heading} under {x.Section} section");

            //.Custom((answers, context) =>
            //{
            //    var question = context.InstanceToValidate;

            //    var (isValid, property) = question.DataType switch
            //    {
            //        "Boolean" or "Radio button" or "Checkbox" => (answers.Exists(ans => ans.IsSelected), $"Questions[{question.Index}].Answers"),
            //        _ => (true, "")
            //    };

            //    if (!isValid)
            //    {
            //        context.AddFailure(property, $"Question {question.Heading} under {question.Section} section");
            //    }
            //});
        });
    }

    // Check if conditional question is valid based on its rules
    private bool IsValidConditionalQuestion(QuestionViewModel question)
    {
        foreach (var rule in question.Rules)
        {
            var parentQuestion = _questions.Find(q => q.QuestionId == rule.ParentQuestionId);

            if (parentQuestion == null)
            {
                continue;
            }

            var selectedOptions = rule.Condition.ParentOptions.Intersect(parentQuestion.Answers.Where(a => a.IsSelected).Select(a => a.AnswerId));

            // Check if parent question answer meets the rule's condition
            var isRuleValid = selectedOptions.Count() == rule.Condition.ParentOptionsCount;

            // rule is valid, so check if the dependent question was answered
            if (isRuleValid)
            {
                //return question.DataType switch
                //{
                //    "Boolean" or "Radio button" or "Checkbox" => question.Answers.Exists(ans => ans.IsSelected),
                //    "Date" or "Email" or "Text" => !string.IsNullOrEmpty(question.AnswerText),
                //    _ => false
                //};
                return true;
            }

            // rule is not applicable
            return false;
        }

        return false;
    }
}