using FluentValidation;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;

namespace Rsp.Portal.Web.Validators;

public class AuthoriseModificationsOutcomeViewModelValidator : AbstractValidator<AuthoriseModificationsOutcomeViewModel>
{
    private const int MaxCharactersCount = 1000;
    private const int ReasonMaxCharactersCount = 500;
    private readonly string RevisionDescriptionMandatoryErrorMessage = "Enter a description of revisions you want to request";
    private readonly string RevisionDescriptionMaxCharactersErrorMessage = $"The description must be {MaxCharactersCount} characters or less";
    private readonly string ModificationsNotAuthorisedErrorMessage = "Enter a reason for not authorising the modification";
    private readonly string ModificationsNotAuthorisedCharactersErrorMessage = $"The reason must be {ReasonMaxCharactersCount} characters or less";

    public AuthoriseModificationsOutcomeViewModelValidator()
    {
        RuleFor(x => x.RevisionDescription)
        .Cascade(CascadeMode.Stop)
        .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage(RevisionDescriptionMandatoryErrorMessage)
        .MaximumLength(MaxCharactersCount)
            .WithMessage(RevisionDescriptionMaxCharactersErrorMessage)
            .When(x => x.Outcome == "RequestRevisions");

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

        RuleFor(x => x.ReasonNotApproved)
        .Cascade(CascadeMode.Stop)
        .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage(ModificationsNotAuthorisedErrorMessage)
        .MaximumLength(ReasonMaxCharactersCount)
            .WithMessage(ModificationsNotAuthorisedCharactersErrorMessage)
            .When(x => x.Outcome == "NotAuthorised");

        RuleFor(x => x.ReasonNotApproved)
            .Custom((text, context) =>
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return;
                }

                var characterCount = text.Length;
                if (characterCount > ReasonMaxCharactersCount)
                {
                    var excessChars = characterCount - ReasonMaxCharactersCount;
                    context.AddFailure("_DescriptionExcessCharacterCount", $"You have exceeded the characters limits by {excessChars}");
                }
            });
    }
}