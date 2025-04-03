using FluentValidation;
using Rsp.IrasPortal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class UserInfoValidator : AbstractValidator<UserViewModel>
{
    private const string MaxCharactersErrorMessage = "Max 250 characters allowed";
    private const string MandatoryErrorMessage = "Field is mandatory";
    private const string ConditionalMandatoryErrorMessage = "Field is mandatory when the role 'operations' is selected";

    public UserInfoValidator()
    {
        RuleFor(x => x.Title)
           .MaximumLength(250)
           .WithMessage(MaxCharactersErrorMessage);

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage(MandatoryErrorMessage)
            .MaximumLength(250)
            .WithMessage(MaxCharactersErrorMessage);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage(MandatoryErrorMessage)
            .MaximumLength(250)
            .WithMessage(MaxCharactersErrorMessage);

        // email validation to loosley comply with RFC 5322 standard
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage(MandatoryErrorMessage)
            .MaximumLength(255)
            .WithMessage("Max 255 characters allowed")
            .Matches(@"^(?!(?:(?:.*\.\.)|(?:.*\.\@)))(?!.*\.\.$)(?!.*\.\@)[\p{L}\p{N}!#$%&'*+/=?^_`{|}~.-]+@[\p{L}\p{N}.-]+\.[\p{L}]{2,}$")
            .WithMessage("Invalid email format.");

        RuleFor(x => x.Telephone)
            .MaximumLength(11)
            .WithMessage("Max 11 characters allowed");

        RuleFor(x => x.Organisation)
            .MaximumLength(250)
            .WithMessage(MaxCharactersErrorMessage);

        RuleFor(x => x.JobTitle)
            .MaximumLength(250)
            .WithMessage(MaxCharactersErrorMessage);

        RuleFor(x => x.UserRoles)
            .NotEmpty()
            .WithMessage(MandatoryErrorMessage);

        RuleFor(x => x.Country)
            .NotEmpty()
            .WithMessage(ConditionalMandatoryErrorMessage)
            .When(x => x.UserRoles != null && x.UserRoles.Any(role => role.RoleName == "operations" && role.IsSelected));

        RuleFor(x => x.AccessRequired)
            .NotEmpty()
            .WithMessage(ConditionalMandatoryErrorMessage)
            .When(x => x.UserRoles != null && x.UserRoles.Any(role => role.RoleName == "operations" && role.IsSelected));
    }
}