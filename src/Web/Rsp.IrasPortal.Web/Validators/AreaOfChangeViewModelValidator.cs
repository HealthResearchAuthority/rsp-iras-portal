using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class AreaOfChangeViewModelValidator : AbstractValidator<AreaOfChangeViewModel>
{
    public AreaOfChangeViewModelValidator()
    {
        RuleFor(x => x.AreaOfChangeId)
            .Must(id => id.HasValue && id.Value != 0)
            .WithMessage("Select area of change");

        RuleFor(x => x.SpecificChangeId)
            .Must(id => id.HasValue && id.Value != 0)
            .WithMessage("Select specific change");
    }
}