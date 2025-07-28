using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class SearchOrganisationViewModelValidator : AbstractValidator<SearchOrganisationViewModel>
{
    public SearchOrganisationViewModelValidator()
    {
        RuleFor(x => x.Search.SearchNameTerm)
            .MinimumLength(3)
            .WithMessage("Provide 3 or more characters to search");
    }
}