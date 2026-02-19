using FluentValidation;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;

namespace Rsp.Portal.Web.Validators;

public class AuthoriseModificationsOutcomeViewModelValidator : AbstractValidator<AuthoriseModificationsOutcomeViewModel>
{
    private const int MaxCharactersCount = 1000;
    private const int ReviseAndAuthoriseCharactersCount = 500;
    private const int ReasonMaxCharactersCount = 500;

    private readonly string RevisionDescriptionMandatoryErrorMessage = "Enter a description of revisions you want to request";
    private readonly string RevisionDescriptionMaxCharactersErrorMessage = $"The description must be between 1 and {MaxCharactersCount} characters";
    private readonly string ReviseAndAuthoriseMaxCharactersErrorMessage = $"The description must be between 1 and {ReviseAndAuthoriseCharactersCount} characters";

    private readonly string ModificationsNotAuthorisedErrorMessage = "Enter a reason for not authorising the modification";
    private readonly string ModificationsNotAuthorisedCharactersErrorMessage = $"The reason must be between 1 and {ReasonMaxCharactersCount} characters";

    public AuthoriseModificationsOutcomeViewModelValidator()
    {
        RuleFor(x => x.RevisionDescription)
                   .Cascade(CascadeMode.Stop)
                   .Must(s => !string.IsNullOrWhiteSpace(s))
                       .WithMessage(RevisionDescriptionMandatoryErrorMessage)
                   .When(x => x.Outcome == "RequestRevisions" || x.Outcome == "ReviseAndAuthorise");

        RuleFor(x => x.RevisionDescription)
                    .MaximumLength(MaxCharactersCount)
                        .WithMessage(RevisionDescriptionMaxCharactersErrorMessage)
                    .When(x => x.Outcome == "RequestRevisions");

        RuleFor(x => x.RevisionDescription)
                    .MaximumLength(ReviseAndAuthoriseCharactersCount)
                        .WithMessage(ReviseAndAuthoriseMaxCharactersErrorMessage)
                    .When(x => x.Outcome == "ReviseAndAuthorise");

        RuleFor(x => x.ReasonNotApproved)
        .Cascade(CascadeMode.Stop)
        .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage(ModificationsNotAuthorisedErrorMessage)
        .MaximumLength(ReasonMaxCharactersCount)
            .WithMessage(ModificationsNotAuthorisedCharactersErrorMessage)
            .When(x => x.Outcome == "NotAuthorised");
    }
}