using FluentValidation;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class AuthoriseModificationsNotAuthorisedValidator : AbstractValidator<AuthoriseModificationsOutcomeViewModel>
{
    private const int MaxCharactersCount = 500;

    private readonly string ModificationsNotAuthorisedErrorMessage = "Enter a reason for not authorising the modification";
    private readonly string ModificationsNotAuthorisedCharactersErrorMessage = $"The reason must be {MaxCharactersCount} characters or less";

    public AuthoriseModificationsNotAuthorisedValidator()
    {
        RuleFor(x => x.ReasonNotApproved)
        .Cascade(CascadeMode.Stop)
        .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage(ModificationsNotAuthorisedErrorMessage)
        .MaximumLength(MaxCharactersCount)
            .WithMessage(ModificationsNotAuthorisedCharactersErrorMessage);

        RuleFor(x => x.ReasonNotApproved)
            .Custom((text, context) =>
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return;
                }

                var characterCount = text.Length;
                if (characterCount > MaxCharactersCount)
                {
                    var excessChars = characterCount - MaxCharactersCount;
                    context.AddFailure("_DescriptionExcessCharacterCount", $"You have exceeded the characters limits by {excessChars}");
                }
            });
    }
}