namespace Rsp.IrasPortal.Domain.Identity;

public record User(string? Id,
    string? IdentityProviderId,
    string? Title,
    string GivenName,
    string FamilyName,
    string Email,
    string? JobTitle,
    string? Organisation,
    string? Telephone,
    string? Country,
    string Status,
    DateTime? LastUpdated,
    DateTime? LastLogin = null,
    DateTime? CurrentLogin = null);