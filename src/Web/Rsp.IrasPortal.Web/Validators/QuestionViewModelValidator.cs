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
        When(x => x.IsMandatory, () =>
        {
            // Validate mandatory questions
            RuleFor(x => x.AnswerText)
                .NotEmpty()
                .When(x => x.DataType is "Date" or "Email" or "Text")
                .WithMessage(q => $"Question {q.Heading} under {q.Section} section");

            // Validate answers (apply AnswerViewModelValidator)
            RuleFor(x => x.Answers)
                .Must(ans => ans.Exists(a => a.IsSelected))
                .When(x => x.DataType is "Checkbox")
                .WithMessage(q => $"Question {q.Heading} under {q.Section} section");

            RuleFor(x => x.SelectedOption)
                .Must(option => !string.IsNullOrWhiteSpace(option))
                .When(x => x.DataType is "Boolean" or "Radio button")
                .WithMessage(q => $"Question {q.Heading} under {q.Section} section");
        });

        When(x => !x.IsMandatory && IsValidConditionalQuestion(x), () =>
        {
            RuleFor(x => x.AnswerText)
                .Must(x => !string.IsNullOrWhiteSpace(x))
                .When(x => x.DataType is "Text" or "Email" or "Date")
                .WithMessage(x => $"Question {x.Heading} under {x.Section} section");

            RuleFor(x => x.Answers)
                .Must(x => x.Exists(a => a.IsSelected))
                .When(x => x.DataType is "Checkbox")
                .WithMessage(x => $"Question {x.Heading} under {x.Section} section");

            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.SelectedOption))
                .When(x => x.DataType is "Boolean" or "Radio button")
                .WithMessage(x => $"Question {x.Heading} under {x.Section} section");
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

            if (parentQuestion.DataType is "Boolean" or "Radio button")
            {
                if (!string.IsNullOrWhiteSpace(parentQuestion.SelectedOption))
                {
                    return rule.Condition.ParentOptions.Exists(opt => opt == parentQuestion.SelectedOption);
                }
            }

            if (parentQuestion.DataType is "Checkbox")
            {
                var selectedOptions = rule.Condition.ParentOptions.Intersect(parentQuestion.Answers.Where(a => a.IsSelected).Select(a => a.AnswerId));

                // Check if parent question answer meets the rule's condition
                return selectedOptions.Count() == rule.Condition.ParentOptionsCount;
            }
        }

        return false;
    }
}