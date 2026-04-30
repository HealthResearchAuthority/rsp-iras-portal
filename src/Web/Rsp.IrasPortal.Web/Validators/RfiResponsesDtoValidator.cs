namespace Rsp.Portal.Web.Validators;

using FluentValidation;
using Rsp.Portal.Application.DTOs.Responses;

public class RfiResponsesDtoValidator : AbstractValidator<RfiResponsesDTO>
{
    private const int MaxCharactersCount = 300;

    private const string InitialResponseRequiredMessage =
        "You have not provided a response to reason. Enter the response to request for further information before you continue.";

    private const string ReviseAndAuthoriseRequiredMessage =
        "You have not revised response to reason. Enter the revision to response before you continue.";

    private const string ReviseAndAuthoriseReasonRequiredMessage =
       "You have not provided a reason. Enter the reason for revised response to reason before you continue.";

    private static readonly string MaxLengthErrorMessage =
        $"The description must be between 1 and {MaxCharactersCount} characters";

    private const string RequestRevisionRequiredMessage =
        "You have not provided a revision to response. Enter a response before you continue.";

    public RfiResponsesDtoValidator()
    {
        // INITIAL RESPONSE
        When(x => x.InitialResponse.Count > 0, () =>
        {
            RuleFor(x => x.InitialResponse[0])
                .NotEmpty()
                .WithMessage(InitialResponseRequiredMessage)
                .MaximumLength(MaxCharactersCount)
                .WithMessage(MaxLengthErrorMessage)
                .OverridePropertyName("InitialResponse_0");
        });

        // REVISE AND AUTHORISE
        When(x => x.ReviseAndAuthorise.Count > 0, () =>
        {
            RuleFor(x => x.ReviseAndAuthorise[0])
                .NotEmpty()
                .WithMessage(ReviseAndAuthoriseRequiredMessage)
                .MaximumLength(MaxCharactersCount)
                .WithMessage(MaxLengthErrorMessage)
                .OverridePropertyName("ReviseAndAuthorise_0");
        });

        // REASON FOR REVISE AND AUTHORISE
        When(x => x.ReasonForReviseAndAuthorise.Count > 0, () =>
        {
            RuleFor(x => x.ReasonForReviseAndAuthorise[0])
                .NotEmpty()
                .WithMessage(ReviseAndAuthoriseReasonRequiredMessage)
                .MaximumLength(MaxCharactersCount)
                .WithMessage(MaxLengthErrorMessage)
                .OverridePropertyName("ReasonForReviseAndAuthorise_0");
        });
        // REQUEST REVISION
        When(x => x.RequestRevisionsBySponsor.Count > 0, () =>
        {
            RuleFor(x => x.RequestRevisionsBySponsor[0])
                .NotEmpty()
                .WithMessage(RequestRevisionRequiredMessage)
                .MaximumLength(MaxCharactersCount)
                .WithMessage(MaxLengthErrorMessage)
                .OverridePropertyName("RequestRevisionsBySponsor_0");
        });
    }
}