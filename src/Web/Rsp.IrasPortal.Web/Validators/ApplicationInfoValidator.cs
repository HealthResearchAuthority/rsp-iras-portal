using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class ApplicationInfoValidator : AbstractValidator<ApplicationInfoViewModel>
{
    public ApplicationInfoValidator()
    {
        // Validate all questions in the questionnaire
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required");
    }
}