using FluentValidation;
using FluentValidation.Results;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class AffectingOrganisationsViewModelValidator : AbstractValidator<AffectingOrganisationsViewModel>
{
    private bool validateAffectedOrgsData;

    protected override bool PreValidate(ValidationContext<AffectingOrganisationsViewModel> context, ValidationResult result)
    {
        if (context.RootContextData.TryGetValue(ValidationKeys.ProjectModificationPlannedEndDate.AffectedOrganisations, out var validate))
        {
            validateAffectedOrgsData = (bool)validate;
        }

        return base.PreValidate(context, result);
    }

    public AffectingOrganisationsViewModelValidator()
    {
        RuleFor(x => x.SelectedLocations)
            .NotEmpty()
            .WithMessage("Select at least one location");

        When(_ => validateAffectedOrgsData, () =>
        {
            RuleFor(x => x.SelectedAffectedOrganisations)
                .Must(x => !string.IsNullOrWhiteSpace(x))
                .WithMessage("Select one option for affected organisations");

            RuleFor(x => x.SelectedAdditionalResources)
                .Must(x => !string.IsNullOrWhiteSpace(x))
                .WithMessage("Select one option for additional resources");
        });
    }
}