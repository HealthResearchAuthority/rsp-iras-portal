using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class ApplicationSearchModelValidator : AbstractValidator<ApplicationSearchModel>
{
    public ApplicationSearchModelValidator()
    {
        // Validate "From" date is valid if any part entered
        When(AnyFromDateFieldEntered, () =>
        {
            RuleFor(d => d.FromDate)
                .NotNull()
                .WithMessage("'Search from' date must be in the correct format")
                .WithName("FromDate");
        });

        // Validate "To" date is valid if any part entered
        When(AnyToDateFieldEntered, () =>
        {
            RuleFor(d => d.ToDate)
                .NotNull()
                .WithMessage("'Search to' date must be in the correct format")
                .WithName("ToDate");
        });

        // Validate date order only if both valid
        When(d => d.FromDate.HasValue && d.ToDate.HasValue, () =>
        {
            RuleFor(d => d)
                .Must(d => d.ToDate >= d.FromDate)
                .WithMessage("'Search to' date must be after 'Search from' date")
                .WithName("ToDate");
        });
    }

    private bool AnyFromDateFieldEntered(ApplicationSearchModel searchModel) =>
        !string.IsNullOrWhiteSpace(searchModel.FromDay) ||
        !string.IsNullOrWhiteSpace(searchModel.FromMonth) ||
        !string.IsNullOrWhiteSpace(searchModel.FromYear);

    private bool AnyToDateFieldEntered(ApplicationSearchModel searchModel) =>
        !string.IsNullOrWhiteSpace(searchModel.ToDay) ||
        !string.IsNullOrWhiteSpace(searchModel.ToMonth) ||
        !string.IsNullOrWhiteSpace(searchModel.ToYear);
}