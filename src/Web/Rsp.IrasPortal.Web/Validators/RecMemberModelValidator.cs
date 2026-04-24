using FluentValidation;
using Rsp.IrasPortal.Web.Features.MemberManagement.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class RecMemberModelValidator : AbstractValidator<RecMemberViewModel>
{
    private const string CommitteeRoleMandatoryErrorMessage = "Select a committee role";
    private const string DesignationMandatoryErrorMessage = "Select a designation";
    private const string TelephoneNumberFormatErrorMessage = "Enter a telephone number, like 01632 960 001, 07700 900 982 or +44 808 157 0192";
    private const string TelephoneMaxCharactersErrorMessage = "Telephone must be 13 digits or less";
    private const string DateLeftInPastErrorMessage = "The date the member left this committee must be today or in the past";

    public RecMemberModelValidator()
    {
        RuleFor(x => x.CommitteeRole)
            .NotEmpty()
            .WithMessage(CommitteeRoleMandatoryErrorMessage);

        RuleFor(x => x.Designation)
            .NotEmpty()
            .WithMessage(DesignationMandatoryErrorMessage);

        RuleFor(x => x.RecTelephoneNumber)
            .MaximumLength(13)
            .WithMessage(TelephoneMaxCharactersErrorMessage)
            .Matches(@"^\+?\d+$")
            .WithMessage(TelephoneNumberFormatErrorMessage);

        RuleFor(x => x.DateTimeLeft)
            .LessThanOrEqualTo(DateTime.Today)
            .WithMessage(DateLeftInPastErrorMessage)
            .When(x => x.MemberLeftOrganisation);
    }
}