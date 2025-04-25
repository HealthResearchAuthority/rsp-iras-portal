using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using FluentValidation.Results;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

[ExcludeFromCodeCoverage]
public class QuestionSetValidator : AbstractValidator<QuestionnaireViewModel>
{
    private bool includeMandatoryCheck;

    protected override bool PreValidate(ValidationContext<QuestionnaireViewModel> context, ValidationResult result)
    {
        includeMandatoryCheck = (bool)context.RootContextData["ValidateMandatoryOnly"];
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