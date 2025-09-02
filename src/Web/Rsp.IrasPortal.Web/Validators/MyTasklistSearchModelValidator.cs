using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class MyTasklistSearchModelValidator : AbstractValidator<MyTasklistSearchModel>
{
    public MyTasklistSearchModelValidator()
    {
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
                .WithMessage("The date you’ve selected is before the search above")
                .WithName("ToDate");
        });
    }

    private bool AnyFromDateFieldEntered(MyTasklistSearchModel model) =>
        !string.IsNullOrWhiteSpace(model.FromDay) ||
        !string.IsNullOrWhiteSpace(model.FromMonth) ||
        !string.IsNullOrWhiteSpace(model.FromYear);

    private bool AnyToDateFieldEntered(MyTasklistSearchModel model) =>
        !string.IsNullOrWhiteSpace(model.ToDay) ||
        !string.IsNullOrWhiteSpace(model.ToMonth) ||
        !string.IsNullOrWhiteSpace(model.ToYear);
}
