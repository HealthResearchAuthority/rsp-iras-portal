using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class IrasIdCheckValidator : AbstractValidator<IrasIdCheckViewModel>
{
    public IrasIdCheckValidator()
    {
        // Validate IRAS ID
        RuleFor(x => x.IrasId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Enter an IRAS ID")
            .Length(4, 7)
                .WithMessage("IRAS ID must be 4 to 7 digits")
            .Matches(@"^\d+$")
                .WithMessage("IRAS ID must only contain numbers");
    }
}