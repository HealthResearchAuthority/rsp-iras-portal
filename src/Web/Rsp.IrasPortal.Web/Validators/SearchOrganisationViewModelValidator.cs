using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class SearchOrganisationViewModelValidator : AbstractValidator<SearchOrganisationViewModel>
{
    public SearchOrganisationViewModelValidator()
    {
        RuleFor(x => x.SearchTerm)
            .NotEmpty()
            .WithMessage("No participant organisations have been selected. Select at least one participant organisation before clicking 'Save and continue'.");
    }
}