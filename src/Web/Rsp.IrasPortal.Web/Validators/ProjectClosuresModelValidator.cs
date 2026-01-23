using FluentValidation;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Validators;

public class ProjectClosuresModelValidator : AbstractValidator<ProjectClosuresModel>
{
    public ProjectClosuresModelValidator()
    {
        RuleFor(m => m.ActualClosureDate).SetValidator(new DateViewModelValidator());
    }
}