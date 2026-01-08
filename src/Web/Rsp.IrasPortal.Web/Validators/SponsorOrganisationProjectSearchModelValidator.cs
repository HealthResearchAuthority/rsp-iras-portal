using FluentValidation;
using Rsp.IrasPortal.Web.Features.SponsorWorkspace.MyOrganisations.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class SponsorOrganisationProjectSearchModelValidator : AbstractValidator<SponsorOrganisationProjectSearchModel>
{
    private const string DateFromtErrorMessage = "'Search from' date must be in the correct format";
    private const string DateToErrorMessage = "'Search to' date must be in the correct format";
    private const string DateRangeErrorMessage = "The date you’ve selected is before the search above";

    public SponsorOrganisationProjectSearchModelValidator()
    {
        // Validate "From" date is valid if any part entered
        When(AnyFromDateFieldEntered, () =>
        {
            RuleFor(x => x.FromDate)
                .NotNull()
                .WithMessage(DateFromtErrorMessage)
                .WithName("FromDate");
        });

        // Validate "To" date is valid if any part entered
        When(AnyToDateFieldEntered, () =>
        {
            RuleFor(x => x.ToDate)
                .NotNull()
                .WithMessage(DateToErrorMessage)
                .WithName("ToDate");
        });

        // Validate date order only if both valid
        When(x => x.FromDate.HasValue && x.ToDate.HasValue, () =>
        {
            RuleFor(x => x)
                .Must(x => x.ToDate >= x.FromDate)
                .WithMessage(DateRangeErrorMessage)
                .WithName("ToDate");
        });
    }

    private static bool AnyFromDateFieldEntered(SponsorOrganisationProjectSearchModel model) =>
      !string.IsNullOrWhiteSpace(model.FromDay) ||
      !string.IsNullOrWhiteSpace(model.FromMonth) ||
      !string.IsNullOrWhiteSpace(model.FromYear);

    private static bool AnyToDateFieldEntered(SponsorOrganisationProjectSearchModel model) =>
        !string.IsNullOrWhiteSpace(model.ToDay) ||
        !string.IsNullOrWhiteSpace(model.ToMonth) ||
        !string.IsNullOrWhiteSpace(model.ToYear);
}