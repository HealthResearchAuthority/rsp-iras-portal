using FluentValidation;
using Rsp.IrasPortal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class UserInfoValidator : AbstractValidator<UserViewModel>
{
    public UserInfoValidator()
    {
        RuleFor(x => x.Title)
           .MaximumLength(250)
           .WithMessage("Max 250 characters allowed");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("Field is mandatory.")
            .MaximumLength(250)
            .WithMessage("Max 250 characters allowed");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Field is mandatory.")
            .MaximumLength(250)
            .WithMessage("Max 250 characters allowed");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Field is mandatory.")
            .EmailAddress()
            .WithMessage("Incorrect email format");

        RuleFor(x => x.Telephone)
            .MaximumLength(11)
            .WithMessage("Max 11 characters allowed");

        RuleFor(x => x.Organisation)
            .MaximumLength(250)
            .WithMessage("Max 250 characters allowed");

        RuleFor(x => x.JobTitle)
            .MaximumLength(250)
            .WithMessage("Max 250 characters allowed");

        RuleFor(x => x.Role)
            .NotEmpty()
            .WithMessage("Field is mandatory.");

        RuleFor(x => x.Country)
            .NotEmpty()
            .WithMessage("Field is mandatory when the role 'operations' is selected")
            .When(x => x.Role == "operations");

        RuleFor(x => x.AccessRequired)
            .NotEmpty()
            .WithMessage("Field is mandatory when the role 'operations' is selected")
            .When(x => x.Role == "operations");
    }
}