using FluentValidation;
using Rsp.IrasPortal.Web.Features.SponsorWorkspace.Authorisation.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class SponsorAuthorisationsSearchModelValidator : AbstractValidator<SponsorAuthorisationsSearchModel>
{
    public SponsorAuthorisationsSearchModelValidator()
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