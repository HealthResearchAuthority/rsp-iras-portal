using FluentValidation;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class UserInfoValidator : AbstractValidator<UserViewModel>
{
    private const string OrganisationMaxCharactersErrorMessage = "Organisation must be 250 characters or less";
    private const string GivenNameMaxCharactersErrorMessage = "First name must be 250 characters or less";
    private const string FamilyNameMaxCharactersErrorMessage = "Last name must be 250 characters or less";
    private const string JobTitleMaxCharactersErrorMessage = "Job title must be 250 characters or less";
    private const string TitleMaxCharactersErrorMessage = "Title must be 250 characters or less";
    private const string TelephoneMaxCharactersErrorMessage = "Telephone must be 13 digits or less";
    private const string TelephoneNotDigitMessage = "Telephone must only contain numbers";
    private const string GivenNameMandatoryErrorMessage = "Enter a first name";
    private const string FamilyNameMandatoryErrorMessage = "Enter a last name";
    private const string EmailFormatErrorMessage = "Enter an email address in the correct format";
    private const string ConditionalCountryMandatoryErrorMessage = "Select at least one country";
    private const string ConditionalReviewBodyMandatoryErrorMessage = "Select at least one review body";
    private const string EmailMaxCharactersErrorMessage = "Email address must be 254 characters or less";
    private const string EmailMandatoryErrorMessage = "Enter an email address";

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
            .NotEmpty()
            .WithMessage(EmailMandatoryErrorMessage)
            .MaximumLength(254)
            .WithMessage(EmailMaxCharactersErrorMessage)
            .Matches(@"^(?!\.)(?!(?:(?:.*\.\.)|(?:.*\.\@)))(?!.*\.\.$)(?!.*\.\@)[\p{L}\p{N}!#$%&'*+\/=?^_`{|}~.-]{1,64}@(?:[\p{L}\p{N}](?:[\p{L}\p{N}-]{0,61}[\p{L}\p{N}])?\.)+[\p{L}]{2,}$")
            .WithMessage(EmailFormatErrorMessage);

        RuleFor(x => x.Telephone)
            .MaximumLength(13)
            .WithMessage(TelephoneMaxCharactersErrorMessage)
            .Matches(@"^\+?\d+$")
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
            .When(x => x.UserRoles.Any(role => role is { Name: Roles.TeamManager, IsSelected: true }));

        RuleFor(x => x.ReviewBodies)
            .Must(rbs => rbs != null && rbs.Any(rb => rb.IsSelected))
            .WithMessage(ConditionalReviewBodyMandatoryErrorMessage)
            .When(x => x.UserRoles.Any(role =>
                role.IsSelected &&
                (string.Equals(role.Name, Roles.StudyWideReviewer, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(role.Name, Roles.WorkflowCoordinator, StringComparison.OrdinalIgnoreCase))
            ));
    }
}