using FluentValidation;
using FluentValidation.Results;
using Rsp.IrasPortal.Web.Models;
using Rsp.IrasPortal.Web.ValidatorHelpers;

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
        When(x => x.IsMandatory || QuestionRuleEvaluator.IsRuleApplicable(x, _questions), () =>
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
                .When(x => x.DataType is "Boolean" or "Radio button")
                .WithMessage(GetValidationMessage);
        });
    }
}