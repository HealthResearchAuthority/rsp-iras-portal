using FluentValidation;
using Rsp.IrasPortal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class UserInfoValidator : AbstractValidator<UserViewModel>
{
    private const string OrganisationMaxCharactersErrorMessage = "Organisation must be 250 characters or less";
    private const string FirstNameMaxCharactersErrorMessage = "First Name must be 250 characters or less";
    private const string LastNameMaxCharactersErrorMessage = "Last Name must be 250 characters or less";
    private const string JobTitleMaxCharactersErrorMessage = "Job title must be 250 characters or less";
    private const string TitleMaxCharactersErrorMessage = "Title must be 250 characters or less";
    private const string EmailMaxCharactersErrorMessage = "Email must be 250 characters or less";
    private const string TelephoneMaxCharactersErrorMessage = "Telephone must be 11 characters or less";
    private const string FirstNameMandatoryErrorMessage = "Enter a first name";
    private const string LastNameMandatoryErrorMessage = "Enter a last name";
    private const string EmailFormatErrorMessage = "Enter an email address in the correct format, like name@example.com";
    private const string EmailMandatoryErrorMessage = "Enter an email address";
    private const string ConditionalCountryMandatoryErrorMessage = "You must provide a country";
    private const string ConditionalAccessRequiredMandatoryErrorMessage = "You must provide the access required";
    private const string OperationsRole = "operations";

    public UserInfoValidator()
    {
        RuleFor(x => x.Title)
           .MaximumLength(250)
           .WithMessage(TitleMaxCharactersErrorMessage);

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage(FirstNameMandatoryErrorMessage)
            .MaximumLength(250)
            .WithMessage(FirstNameMaxCharactersErrorMessage);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage(LastNameMandatoryErrorMessage)
            .MaximumLength(250)
            .WithMessage(LastNameMaxCharactersErrorMessage);

        // email validation to loosley comply with RFC 5322 standard
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage(EmailMandatoryErrorMessage)
            .MaximumLength(255)
            .WithMessage(EmailMaxCharactersErrorMessage)
            .Matches(@"^(?!(?:(?:.*\.\.)|(?:.*\.\@)))(?!.*\.\.$)(?!.*\.\@)[\p{L}\p{N}!#$%&'*+/=?^_`{|}~.-]+@[\p{L}\p{N}.-]+\.[\p{L}]{2,}$")
            .WithMessage(EmailFormatErrorMessage);

        RuleFor(x => x.Telephone)
            .MaximumLength(11)
            .WithMessage(TelephoneMaxCharactersErrorMessage);

        RuleFor(x => x.Organisation)
            .MaximumLength(250)
            .WithMessage(OrganisationMaxCharactersErrorMessage);

        RuleFor(x => x.JobTitle)
            .MaximumLength(250)
            .WithMessage(JobTitleMaxCharactersErrorMessage);

        RuleFor(x => x.Country)
            .NotEmpty()
            .WithMessage(ConditionalCountryMandatoryErrorMessage)
            .When(x => x.UserRoles != null && x.UserRoles.Any(role => role.Name == OperationsRole && role.IsSelected));

        RuleFor(x => x.AccessRequired)
            .NotEmpty()
            .WithMessage(ConditionalAccessRequiredMandatoryErrorMessage)
            .When(x => x.UserRoles != null && x.UserRoles.Any(role => role.Name == OperationsRole && role.IsSelected));
    }
}