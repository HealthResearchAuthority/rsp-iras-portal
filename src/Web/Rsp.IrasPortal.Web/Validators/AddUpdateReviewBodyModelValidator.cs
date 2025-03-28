using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using FluentValidation;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Validators;

public class AddUpdateReviewBodyModelValidator : AbstractValidator<AddUpdateReviewBodyModel>
{
    private const string MandatoryErrorMessage = "Field is mandatory";

    public AddUpdateReviewBodyModelValidator()
    {
        RuleFor(x => x.OrganisationName)
            .NotEmpty().WithMessage(MandatoryErrorMessage)
            .MaximumLength(250).WithMessage("Max 250 characters allowed");

        RuleFor(x => x.EmailAddress)
            .NotEmpty()
            .WithMessage(MandatoryErrorMessage)
            .MaximumLength(255)
            .WithMessage("Max 255 characters allowed")
            .Matches(@"^(?!(?:(?:.*\.\.)|(?:.*\.\@)))(?!.*\.\.$)(?!.*\.\@)[\p{L}\p{N}!#$%&'*+/=?^_`{|}~.-]+@[\p{L}\p{N}.-]+\.[\p{L}]{2,}$")
            .WithMessage("Invalid email format");


        RuleFor(x => x.Description)
            .Must(text => HaveMaxWords(text, 500))
            .WithMessage("The description cannot exceed 500 words.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Countries)
            .Must(c => c != null && c.Any())
            .WithMessage("Select at least one country.");
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