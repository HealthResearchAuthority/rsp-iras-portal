using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class IrasIdViewModelValidator : AbstractValidator<IrasIdViewModel>
{
    public IrasIdViewModelValidator()
    {
        // Validate IRAS ID
        RuleFor(x => x.IrasId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Enter an IRAS ID")
            .Length(4, 7)
                .WithMessage("IRAS ID must be 4 to 7 digits")
            .Matches(@"^\d+$")
                .WithMessage("IRAS ID must only contain numbers")
            .Must(id => id == null || !id.StartsWith("0"))
                .WithMessage("IRAS ID cannot start with '0'.");
    }
}