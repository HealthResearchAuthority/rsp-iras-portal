using FluentValidation;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;

namespace Rsp.Portal.Web.Validators;

public class AuthorisationsModificationsSearchModelValidator : AbstractValidator<AuthorisationsModificationsSearchModel>
{
    public AuthorisationsModificationsSearchModelValidator()
    {
        RuleFor(x => x.SearchTerm)
        .Cascade(CascadeMode.Stop)
        .Must(term => string.IsNullOrEmpty(term) || term.Length >= 2)
            .WithMessage("Modification ID must be at least 2 characters")
        .Must(term => string.IsNullOrEmpty(term) || term.Length <= 8)
            .WithMessage("Modification ID must be no more than 8 characters")
        .Matches(@"^[0-9/]+$")
                .When(x => !string.IsNullOrEmpty(x.SearchTerm))
                .WithMessage("Modification ID must only contain numbers and '/'");
    }
}