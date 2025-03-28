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
            .NotEmpty().WithMessage(MandatoryErrorMessage)
            .Must(IsValidEmail).WithMessage("Invalid email format");


        RuleFor(x => x.Description)
            .Must(text => HaveMaxWords(text, 500))
            .WithMessage("The description cannot exceed 500 words.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Countries)
            .Must(c => c != null && c.Any())
            .WithMessage("Select at least one country.");
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || email.All(char.IsWhiteSpace))
        {
            return false;
        }

        var trimmedEmail = email.TrimEnd();

        if (trimmedEmail.Length > 320)
        {
            return false;
        }

        var atIndex = trimmedEmail.IndexOf('@');
        if (atIndex <= 0 || atIndex != trimmedEmail.LastIndexOf('@'))
        {
            return false;
        }

        var local = trimmedEmail[..atIndex];
        var domain = trimmedEmail[(atIndex + 1)..];

        if (local.Length > 64)
        {
            return false;
        }

        // Split domain into labels and punycode each one safely
        var domainLabels = domain.Split('.');
        var idn = new IdnMapping();
        var asciiLabels = new List<string>();

        foreach (var label in domainLabels)
        {
            if (string.IsNullOrEmpty(label))
            {
                return false;
            }

            string asciiLabel;
            try
            {
                asciiLabel = idn.GetAscii(label);
            }
            catch
            {
                return false; // Invalid Unicode or punycode error
            }

            if (asciiLabel.Length > 63)
            {
                return false;
            }

            asciiLabels.Add(asciiLabel);
        }

        var asciiDomain = string.Join(".", asciiLabels);

        if (asciiDomain.Length > 255)
        {
            return false;
        }

        var normalizedEmail = $"{local}@{asciiDomain}";

        if (normalizedEmail.Contains(" ") || normalizedEmail.Any(c => "<>[](),;:\"".Contains(c)))
        {
            return false;
        }

        if (local.StartsWith('.') || local.EndsWith('.') || local.Contains("..") || local.StartsWith('-'))
        {
            return false;
        }

        if (asciiDomain.Contains(".."))
        {
            return false;
        }

        var tld = asciiLabels.LastOrDefault();
        if (string.IsNullOrEmpty(tld) || tld.Length < 2)
        {
            return false; // TLD must be at least 2 characters
        }

        if (!tld.All(char.IsAsciiLetter) && !tld.StartsWith("xn--", StringComparison.OrdinalIgnoreCase))
        {
            return false; // TLD must be alphabetic or an IDN (Punycode)
        }

        var reservedDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "localhost", "test", "example", "invalid", "local"
        };

        if (reservedDomains.Contains(asciiDomain) || reservedDomains.Contains(tld))
        {
            return false;
        }

        if (IPAddress.TryParse(asciiDomain, out _))
        {
            return false;
        }

        // Regex for Unicode local part + ASCII (Punycode) domain
        var pattern = @"^[\p{L}\p{M}\p{N}\p{P}\p{S}]+@[a-zA-Z0-9-]+(\.[a-zA-Z0-9-]+)+$";
        return Regex.IsMatch(normalizedEmail, pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
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