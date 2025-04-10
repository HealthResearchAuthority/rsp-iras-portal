using FluentValidation;
using Rsp.IrasPortal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class UserInfoValidator : AbstractValidator<UserViewModel>
{
    private const string MaxCharactersErrorMessage = "Max 250 characters allowed";
    private const string FirstNameMandatoryErrorMessage = "Enter a first name";
    private const string LastNameMandatoryErrorMessage = "Enter a last name";
    private const string EmailMandatoryErrorMessage = "Enter an email address in the correct format, like name@example.com";
    private const string ConditionalMandatoryErrorMessage = "Field is mandatory when the role 'operations' is selected";
    private const string ConditionalCountryMandatoryErrorMessage = "Enter a country";
    private const string ConditionalReviewBodyMandatoryErrorMessage = "Enter a review body";
    private const string OperationsRole = "operations";

    public UserInfoValidator()
    {
        RuleFor(x => x.Title)
           .MaximumLength(250)
           .WithMessage(MaxCharactersErrorMessage);

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage(FirstNameMandatoryErrorMessage)
            .MaximumLength(250)
            .WithMessage(MaxCharactersErrorMessage);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage(LastNameMandatoryErrorMessage)
            .MaximumLength(250)
            .WithMessage(MaxCharactersErrorMessage);

        // email validation to loosley comply with RFC 5322 standard
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage(EmailMandatoryErrorMessage)
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
            .WithMessage(ConditionalCountryMandatoryErrorMessagee)
            .When(x => x.UserRoles != null && x.UserRoles.Any(role => role.Name == OperationsRole && role.IsSelected));

        RuleFor(x => x.AccessRequired)
            .NotEmpty()
            .WithMessage(ConditionalReviewBodyMandatoryErrorMessage)
            .When(x => x.UserRoles != null && x.UserRoles.Any(role => role.Name == OperationsRole && role.IsSelected));
    }
}