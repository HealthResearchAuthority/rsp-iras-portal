using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class AddUpdateReviewBodyModelValidator : AbstractValidator<AddUpdateReviewBodyModel>
{
    private const int MaxWordCount = 250;

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
            .WithMessage("Enter a country");

        RuleFor(x => x.EmailAddress)
            .NotEmpty()
            .WithMessage("Enter an email address")
            .MaximumLength(250)
            .WithMessage("Email address must be 250 characters or less")
            .Matches(@"^(?!(?:(?:.*\.\.)|(?:.*\.\@)))(?!.*\.\.$)(?!.*\.\@)[\p{L}\p{N}!#$%&'*+/=?^_`{|}~.-]+@[\p{L}\p{N}.-]+\.[\p{L}]{2,}$")
            .WithMessage("Enter an email address in the correct format");

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