using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

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
    }
}