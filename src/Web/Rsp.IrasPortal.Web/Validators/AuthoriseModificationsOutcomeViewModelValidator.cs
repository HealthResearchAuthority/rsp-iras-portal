using FluentValidation;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;

namespace Rsp.Portal.Web.Validators;

public class AuthoriseModificationsOutcomeViewModelValidator : AbstractValidator<AuthoriseModificationsOutcomeViewModel>
{
    private const int MaxCharactersCount = 1000;

    private readonly string RevisionDescriptionMandatoryErrorMessage = "Enter a description of revisions you want to request";
    private readonly string RevisionDescriptionMaxCharactersErrorMessage = $"The description must be {MaxCharactersCount} characters or less";

    public AuthoriseModificationsOutcomeViewModelValidator()
    {
        RuleFor(x => x.RevisionDescription)
        .Cascade(CascadeMode.Stop)
        .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage(RevisionDescriptionMandatoryErrorMessage)
        .MaximumLength(MaxCharactersCount)
            .WithMessage(RevisionDescriptionMaxCharactersErrorMessage);

        RuleFor(x => x.RevisionDescription)
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