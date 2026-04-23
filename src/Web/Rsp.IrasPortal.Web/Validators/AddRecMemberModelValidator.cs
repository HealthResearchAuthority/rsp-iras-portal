using FluentValidation;
using Rsp.IrasPortal.Web.Features.MemberManagement.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class AddRecMemberModelValidator : AbstractValidator<AddRecMemberViewModel>
{
    private const string EmailMaxCharactersErrorMessage = "Email address must be 254 characters or less";
    private const string EmailMandatoryErrorMessage = "Enter an email address in the correct format, like name@example.com";
    private const string EmailFormatErrorMessage = "Enter an email address in the correct format, like name@example.com";

    public AddRecMemberModelValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage(EmailMandatoryErrorMessage)
            .MaximumLength(254)
            .WithMessage(EmailMaxCharactersErrorMessage)
            .Matches(@"^(?!\.)(?!(?:(?:.*\.\.)|(?:.*\.\@)))(?!.*\.\.$)(?!.*\.\@)[\p{L}\p{N}!#$%&'*+\/=?^_`{|}~.-]{1,64}@(?:[\p{L}\p{N}](?:[\p{L}\p{N}-]{0,61}[\p{L}\p{N}])?\.)+[\p{L}]{2,}$")
            .WithMessage(EmailFormatErrorMessage);
    }
}