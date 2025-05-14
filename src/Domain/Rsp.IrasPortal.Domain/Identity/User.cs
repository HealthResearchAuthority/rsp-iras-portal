namespace Rsp.IrasPortal.Domain.Identity;

public record User(string? Id,
    string? Title,
    string FirstName,
    string LastName,
    string Email,
    string? JobTitle,
    string? Organisation,
    string? Telephone,
    string? Country,
    string Status,
    DateTime? LastUpdated,
    DateTime? LastLogin = null,
    DateTime? CurrentLogin = null);