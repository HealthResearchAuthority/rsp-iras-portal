using FluentValidation;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Validators;

public class AreaOfChangeViewModelValidator : AbstractValidator<AreaOfChangeViewModel>
{
    public AreaOfChangeViewModelValidator()
    {
        RuleFor(x => x.AreaOfChangeId)
            .Must(id => id != null && id != Guid.Empty.ToString())
            .WithMessage("Select area of change");

        RuleFor(x => x.SpecificChangeId)
            .Must(id => id != null && id != Guid.Empty.ToString())
            .WithMessage("Select specific change");

        RuleFor(x => x)
                    .Must(model =>
                    {
                        if (!string.IsNullOrEmpty(model.SpecificChangeId) && model.SpecificChangeId != Guid.Empty.ToString())
                        {
                            return model.SpecificChangeOptions.Any(opt => opt.Value == model.SpecificChangeId);
                        }
                        return true;
                    })
                    .WithMessage("Select ‘Apply selection' to confirm the area of change, then select a specific change");
    }
}