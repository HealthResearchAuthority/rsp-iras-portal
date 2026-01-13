using FluentValidation;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;

namespace Rsp.Portal.Web.Validators;

public class ProjectClosuresSearchModelValidator : AbstractValidator<ProjectClosuresSearchModel>
{
    public ProjectClosuresSearchModelValidator()
    {
        RuleFor(x => x.SearchTerm)
            .Cascade(CascadeMode.Stop)
            .Must(term => string.IsNullOrEmpty(term) || term.Length >= 2)
                .WithMessage("IRAS ID must be at least 2 characters")
            .Must(term => string.IsNullOrEmpty(term) || term.Length <= 7)
                .WithMessage("IRAS ID must be no more than 7 characters")
            .Must(term => string.IsNullOrEmpty(term) || !term.StartsWith('0'))
                .WithMessage("IRAS ID cannot start with '0'")
            .Matches(@"^\d+$")
                    .When(x => !string.IsNullOrEmpty(x.SearchTerm))
                    .WithMessage("IRAS ID must only contain numbers");
    }
}