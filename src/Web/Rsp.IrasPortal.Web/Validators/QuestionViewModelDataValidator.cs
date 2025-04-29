using FluentValidation;

namespace Rsp.IrasPortal.Web.Validators;

public class QuestionViewModelDataValidator : QuestionViewModelValidatorBase
{
    public QuestionViewModelDataValidator()
    {
        // Only validate if the user provided an answer
        When(x => !string.IsNullOrWhiteSpace(x.AnswerText), () =>
        {
            RuleFor(x => x.AnswerText)
                .NotEmpty()
                .When(x => x.DataType is "Text" or "Email" or "Date")
                .WithMessage(GetValidationMessage)
                .DependentRules(() =>
                {
                    ConfigureLengthRule();
                    ConfigureRegExRule();
                    ConfigureDateRule();
                });

            RuleFor(x => x.Answers)
                .Must(x => x.Exists(a => a.IsSelected))
                .When(x => x.DataType is "Checkbox")
                .WithMessage(GetValidationMessage);

            RuleFor(x => x.SelectedOption)
                .Must(x => !string.IsNullOrWhiteSpace(x))
                .When(x => x.DataType is "Boolean" or "Radio button")
                .WithMessage(GetValidationMessage);
        });
    }
}