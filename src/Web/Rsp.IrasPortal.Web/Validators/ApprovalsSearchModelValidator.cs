using FluentValidation;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Validators;

public class ApprovalsSearchModelValidator : AbstractValidator<ApprovalsSearchModel>
{
    public ApprovalsSearchModelValidator()
    {
        // Validate when both Date Submitted and Days Since Submission ranges are entered
        When(x => (x.FromDate.HasValue || x.ToDate.HasValue) && (x.FromSubmission.HasValue || x.ToSubmission.HasValue), () =>
        {
            RuleFor(x => x)
                .Must(_ => false) // Always false to trigger the message when both filters are applied
                .WithMessage("'Date submitted' and 'Days since submission' filters can't be applied at the same time - remove one to continue.")
                .WithName("ToDaysSinceSubmission");
        });

        // Validate "From" date is valid if any part entered
        When(AnyFromDateFieldEntered, () =>
        {
            RuleFor(x => x.FromDate)
                .NotNull()
                .WithMessage("'Search from' date must be in the correct format")
                .WithName("FromDate");
        });

        // Validate "To" date is valid if any part entered
        When(AnyToDateFieldEntered, () =>
        {
            RuleFor(x => x.ToDate)
                .NotNull()
                .WithMessage("'Search to' date must be in the correct format")
                .WithName("ToDate");
        });

        // Validate date order only if both valid
        When(x => x.FromDate.HasValue && x.ToDate.HasValue, () =>
        {
            RuleFor(x => x)
                .Must(x => x.ToDate >= x.FromDate)
                .WithMessage("'Search from' date must be the same as or before 'Search to' date")
                .WithName("ToDate");
        });
    }

    private bool AnyFromDateFieldEntered(ApprovalsSearchModel model) =>
        !string.IsNullOrWhiteSpace(model.FromDay) ||
        !string.IsNullOrWhiteSpace(model.FromMonth) ||
        !string.IsNullOrWhiteSpace(model.FromYear);

    private bool AnyToDateFieldEntered(ApprovalsSearchModel model) =>
        !string.IsNullOrWhiteSpace(model.ToDay) ||
        !string.IsNullOrWhiteSpace(model.ToMonth) ||
        !string.IsNullOrWhiteSpace(model.ToYear);
}