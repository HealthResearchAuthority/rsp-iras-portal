using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class ProjectClosuresModelValidator : AbstractValidator<ProjectClosuresModel>
{
    public ProjectClosuresModelValidator()
    {
        RuleFor(m => m.ActualClosureDate).SetValidator(new DateViewModelValidator());
    }
}