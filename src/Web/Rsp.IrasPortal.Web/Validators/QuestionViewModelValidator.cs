using System.Globalization;
using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Results;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class QuestionViewModelValidator : QuestionViewModelValidatorBase
{
    protected override bool PreValidate(ValidationContext<QuestionViewModel> context, ValidationResult result)
    {
        return base.PreValidate(context, result);
    }

    public QuestionViewModelValidator()
    {
        When(x => x.IsMandatory, () =>
        {
            RuleFor(x => x.AnswerText)
                .NotEmpty()
                .When(x => x.DataType is "Date" or "Email" or "Text")
                .WithMessage(q => $"Question {q.Heading} under {q.Section} section")
                .DependentRules(() =>
                {
                    ConfigureLengthRule();
                    ConfigureRegExRule();
                    ConfigureDateRule();
                });

            RuleFor(x => x.Answers)
                .Must(ans => ans.Exists(a => a.IsSelected))
                .When(x => x.DataType is "Checkbox")
                .WithMessage(q => $"Question {q.Heading} under {q.Section} section");

            RuleFor(x => x.SelectedOption)
                .Must(option => !string.IsNullOrWhiteSpace(option))
                .When(x => x.DataType is "Boolean" or "Radio button")
                .WithMessage(q => $"Question {q.Heading} under {q.Section} section");
        });
    }
}