using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public sealed class EmailAddressValidator : AbstractValidator<EmailModel>
{
    private const string EmailFormatErrorMessage = "Enter an email address in the correct format";
    private const string EmailMaxCharactersErrorMessage = "Email address must be 254 characters or less";
    private const string EmailMandatoryErrorMessage = "Enter an email address";

    public EmailAddressValidator()
    {
        RuleFor(email => email.EmailAddress)
            .NotEmpty()
            .WithMessage(EmailMandatoryErrorMessage)
            .MaximumLength(254)
            .WithMessage(EmailMaxCharactersErrorMessage)
            .Matches(
                @"^(?!\.)(?!(?:(?:.*\.\.)|(?:.*\.\@)))(?!.*\.\.$)(?!.*\.\@)[\p{L}\p{N}!#$%&'*+\/=?^_`{|}~.-]{1,64}@(?:[\p{L}\p{N}](?:[\p{L}\p{N}-]{0,61}[\p{L}\p{N}])?\.)+[\p{L}]{2,}$")
            .WithMessage(EmailFormatErrorMessage);
    }
}