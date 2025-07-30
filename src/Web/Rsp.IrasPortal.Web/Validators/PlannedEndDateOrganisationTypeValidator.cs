using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class PlannedEndDateOrganisationTypeValidator : AbstractValidator<PlannedEndDateOrganisationTypeViewModel>
{
    public PlannedEndDateOrganisationTypeValidator()
    {
        RuleFor(x => x.SelectedOrganisationTypes)
            .NotEmpty()
            .WithMessage("Select at least one organisation type.");
    }
}