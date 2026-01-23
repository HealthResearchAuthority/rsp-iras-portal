using FluentValidation;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Validators;

public class UserSearchModelValidator : AbstractValidator<UserSearchModel>
{
    public UserSearchModelValidator()
    {
        // Only run this rule if both dates are entered
        When(x => x.FromDate.HasValue && x.ToDate.HasValue, () =>
        {
            RuleFor(x => x)
                .Must(x => x.ToDate >= x.FromDate)
                .WithMessage("'Search to' date must be after 'Search from' date")
                .WithName("Search.ToDate");
        });

        // Rules for when the day month and year arent in the correct
        When(x => !string.IsNullOrEmpty(x.FromDay) || !string.IsNullOrEmpty(x.FromMonth) || !string.IsNullOrEmpty(x.FromYear), () =>
        {
            RuleFor(x => x)
                .Must(x => x.FromDate.HasValue)
                .WithMessage("'Search from' date must be in the correct format")
                .WithName("Search.FromDate");
        });

        When(x => !string.IsNullOrEmpty(x.ToDay) || !string.IsNullOrEmpty(x.ToMonth) || !string.IsNullOrEmpty(x.ToYear), () =>
        {
            RuleFor(x => x)
                .Must(x => x.ToDate.HasValue)
                .WithMessage("'Search to' date must be in the correct format")
                .WithName("Search.ToDate");
        });
    }
}