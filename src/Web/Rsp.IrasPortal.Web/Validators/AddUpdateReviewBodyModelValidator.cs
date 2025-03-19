using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class AddUpdateReviewBodyModelValidator : AbstractValidator<AddUpdateReviewBodyModel>
{
    public AddUpdateReviewBodyModelValidator()
    {
        RuleFor(x => x.OrganisationName)
            .NotEmpty().WithMessage("Enter the organisation name");

        RuleFor(x => x.EmailAddress)
            .NotEmpty().WithMessage("Enter an email address")
            .EmailAddress().WithMessage("Enter a valid email address");
        RuleFor(x => x.Description)
            .Must(text => HaveMaxWords(text, 250))
            .WithMessage("The description cannot exceed 250 words.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Countries)
            .Must(c => c != null && c.Any()).WithMessage("Select at least one country.");
    }

    private static bool HaveMaxWords(string? text, int maxWords)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var words = text.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        return words.Length <= maxWords;
    }
}