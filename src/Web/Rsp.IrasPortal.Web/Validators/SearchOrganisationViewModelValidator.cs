using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class SearchOrganisationViewModelValidator : AbstractValidator<SearchOrganisationViewModel>
{
    public SearchOrganisationViewModelValidator()
    {
        RuleFor(x => x.Search.SearchNameTerm)
            .Must(term => string.IsNullOrEmpty(term) || term.Length >= 3)
            .WithMessage("Provide 3 or more characters to search");
    }
}