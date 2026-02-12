using FluentValidation;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.Web.Validators;

public class AddUpdateReviewBodyModelValidator : AbstractValidator<AddUpdateReviewBodyModel>
{
    private const int MaxWordCount = 250;
    private const string EmailFormatErrorMessage = "Enter an email address in the correct format, like example@test.com";
    private const string EmailMaxCharactersErrorMessage = "Email address must be 254 characters or less";
    private const string EmailMandatoryErrorMessage = "Enter an email address";

    public AddUpdateReviewBodyModelValidator()
    {
        RuleFor(x => x.RegulatoryBodyName)
            .NotEmpty()
            .WithMessage("Enter an organisation name")
            .MaximumLength(250)
            .WithMessage("Organisation name must be 250 characters or less");

        RuleFor(x => x.Countries)
            .Must(c => c?.Count > 0)
            // This error message is matching the businesss requirements but is not correct as per
            // the GDS style guide. The correct error messages are shown under the 'Error messages'
            // section on this page: https://design-system.service.gov.uk/components/checkboxes/
            .WithMessage("Select at least one country");

        RuleFor(x => x.EmailAddress)
            .NotEmpty()
            .WithMessage(EmailMandatoryErrorMessage)
            .MaximumLength(254)
            .WithMessage(EmailMaxCharactersErrorMessage)
            .Matches(@"^(?!\.)(?!(?:(?:.*\.\.)|(?:.*\.\@)))(?!.*\.\.$)(?!.*\.\@)[\p{L}\p{N}!#$%&'*+\/=?^_`{|}~.-]{1,64}@(?:[\p{L}\p{N}](?:[\p{L}\p{N}-]{0,61}[\p{L}\p{N}])?\.)+[\p{L}]{2,}$")
            .WithMessage(EmailFormatErrorMessage);

        RuleFor(x => x.Description)
            .Must(text => GetWordCount(text) <= MaxWordCount)
            .WithMessage($"The description must be {MaxWordCount} words or less");

        RuleFor(x => x.Description)
            .Custom((text, context) =>
            {
                var wordCount = GetWordCount(text);
                if (wordCount > MaxWordCount)
                {
                    var excessWords = wordCount - MaxWordCount;
                    context.AddFailure("_DescriptionExcessWordCount", $"You have {excessWords} word{(excessWords == 1 ? "" : "s")} too many");
                }
            });
    }

    private static int GetWordCount(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }
        var words = text.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        return words.Length;
    }
}