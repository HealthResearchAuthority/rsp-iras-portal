using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using FluentValidation.Results;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Validators;

[ExcludeFromCodeCoverage]
public class QuestionSetValidator : AbstractValidator<QuestionnaireViewModel>
{
    private bool includeMandatoryCheck;

    protected override bool PreValidate(ValidationContext<QuestionnaireViewModel> context, ValidationResult result)
    {
        // Check if RootContextData contains the key and it's a bool
        if (context.RootContextData.TryGetValue("ValidateMandatoryOnly", out var flag) && flag is bool value)
        {
            includeMandatoryCheck = value;
        }
        else
        {
            includeMandatoryCheck = false;
        }

        return base.PreValidate(context, result);
    }

    public QuestionSetValidator()
    {
        // Validate all questions in the questionnaire
        RuleForEach(x => x.Questions)
            .SetValidator((question, context) =>
            {
                var dataValidator = new QuestionViewModelDataValidator();

                // Dynamically add mandatory rules if the flag is set
                if (includeMandatoryCheck)
                {
                    dataValidator.Include(new QuestionViewModelValidator());
                }

                return dataValidator;
            });
    }
}