using FluentValidation;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Validators;

public class SearchOrganisationViewModelValidator : AbstractValidator<SearchOrganisationViewModel>
{
    public SearchOrganisationViewModelValidator()
    {
        RuleFor(x => x.Search.SearchNameTerm)
            .Must(term => string.IsNullOrEmpty(term) || term.Length >= 3)
            .WithMessage("Provide 3 or more characters to search");
    }
}