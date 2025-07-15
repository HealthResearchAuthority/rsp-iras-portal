using FluentValidation;
using Rsp.IrasPortal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class UserInfoValidator : AbstractValidator<UserViewModel>
{
    private const string OrganisationMaxCharactersErrorMessage = "Organisation must be 250 characters or less";
    private const string GivenNameMaxCharactersErrorMessage = "First name must be 250 characters or less";
    private const string FamilyNameMaxCharactersErrorMessage = "Last name must be 250 characters or less";
    private const string JobTitleMaxCharactersErrorMessage = "Job title must be 250 characters or less";
    private const string TitleMaxCharactersErrorMessage = "Title must be 250 characters or less";
    private const string TelephoneMaxCharactersErrorMessage = "Telephone must be 11 digits or less";
    private const string TelephoneNotDigitMessage = "Telephone must only contain numbers";
    private const string GivenNameMandatoryErrorMessage = "Enter a first name";
    private const string FamilyNameMandatoryErrorMessage = "Enter a last name";
    private const string EmailFormatErrorMessage = "The email address format is invalid";
    private const string ConditionalCountryMandatoryErrorMessage = "You must provide a country";
    private const string ConditionalAccessRequiredMandatoryErrorMessage = "You must provide the access required";
    private const string OperationsRole = "operations";

    public UserInfoValidator()
    {
        RuleFor(x => x.Title)
           .MaximumLength(250)
           .WithMessage(TitleMaxCharactersErrorMessage);

        RuleFor(x => x.GivenName)
            .NotEmpty()
            .WithMessage(GivenNameMandatoryErrorMessage)
            .MaximumLength(250)
            .WithMessage(GivenNameMaxCharactersErrorMessage);

        RuleFor(x => x.FamilyName)
            .NotEmpty()
            .WithMessage(FamilyNameMandatoryErrorMessage)
            .MaximumLength(250)
            .WithMessage(FamilyNameMaxCharactersErrorMessage);

        RuleFor(x => x.Email)
            .Must(email => EmailValidator.GetEmailValidationError(email?.TrimEnd()) == null)
            .WithMessage(x => $"{EmailFormatErrorMessage} – reason: {EmailValidator.GetEmailValidationError(x.Email?.TrimEnd())}");

        RuleFor(x => x.Telephone)
            .MaximumLength(11)
            .WithMessage(TelephoneMaxCharactersErrorMessage)
            .Must(x => x.All(char.IsDigit))
            .When(x => !string.IsNullOrEmpty(x.Telephone))
            .WithMessage(TelephoneNotDigitMessage);

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