namespace Rsp.IrasPortal.Web.Validators;

public static class EmailValidator
{
    public static string? GetEmailValidationError(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "email address cannot be empty";

        if (!email.Contains("@"))
            return "email address must contain @ symbol";

        var domainPart = GetDomainPart(email);
        var localPart = email[..email.IndexOf('@')];

        return email switch
        {
            _ when email.Length > 320 => "email address must not exceed 320 characters",
            _ when email.StartsWith(" ") => "email address starts with a space",
            _ when email.StartsWith(".") => "email address starts with a dot",
            _ when email.Contains("..") => "email address contains consecutive dots",
            _ when localPart.EndsWith('.') => "email address ends with a dot before the @ symbol",
            _ when email.Contains(" ") => "email address contains space",
            _ when email.Contains("\"") => "quoted strings or quote characters are not supported",
            _ when email.Contains("<") || email.Contains(">") => "email address contains angle brackets",
            _ when email.Contains("[") || email.Contains("]") => "email address contains square brackets",
            _ when email.Contains(":") => "email address contains colon",
            _ when email.Contains(";") => "email address contains semicolon",
            _ when email.Contains(",") => "email address contains comma",
            _ when email.Any(char.IsSurrogate) => "email address contains emoji or unsupported Unicode",
            _ when domainPart is null => "invalid domain format",
            _ when domainPart.Any(c => !char.IsLetterOrDigit(c) && c != '-' && c != '.') => "invalid character in domain part",
            _ when domainPart.Split('.').Any(label => label.StartsWith('-') || label.EndsWith('-')) => "domain label starts or ends with hyphen",
            _ when localPart.Length > 64 => "email address is too long before the @ symbol (maximum 64 characters allowed)",
            _ when !HasValidTopLevelDomain(domainPart) => "email address ends with an invalid domain extension",
            _ when IsReservedDomain(domainPart) => "email address uses a reserved domain name (e.g. localhost, example, test)",
            _ when domainPart.Any(c => c > 127) => "email address contains non-ASCII characters in domain (internationalized domain names are not supported)",
            _ => null
        };
    }

    private static string? GetDomainPart(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex < 0 || atIndex == email.Length - 1)
            return null;
        return email[(atIndex + 1)..];
    }

    private static bool HasValidTopLevelDomain(string domain)
    {
        var lastDotIndex = domain.LastIndexOf('.');
        if (lastDotIndex < 0 || lastDotIndex == domain.Length - 1)
            return false;
        var topLevelDomain = domain[(lastDotIndex + 1)..];
        return topLevelDomain.Length >= 2 && topLevelDomain.All(char.IsLetter);
    }

    private static bool IsReservedDomain(string domain)
    {
        var baseReserved = new[] { "localhost", "invalid" };

        // In production enviroment list of reserved domain is extended for testing examples.
        var extendedReserved = new[] { "example", "example.com", "example.org", "example.net", "test" };
        var isProduction = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";
        var reserved = isProduction
            ? baseReserved.Concat(extendedReserved)
            : baseReserved;

        return reserved.Contains(domain.ToLowerInvariant());
    }
}