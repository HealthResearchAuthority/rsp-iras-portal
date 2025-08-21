using Rsp.IrasPortal.Application.Constants;

namespace Rsp.IrasPortal.Application.DTOs.Requests.UserManagement;

public class CreateUserRequest
{
    public string? Title { get; set; } = null;
    public string GivenName { get; set; } = null!;
    public string FamilyName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? JobTitle { get; set; } = null;
    public string? Organisation { get; set; } = null;
    public string? Telephone { get; set; } = null;
    public string? Country { get; set; } = null;
    public string? IdentityProviderId { get; set; }
    private string? _status;

    public string Status
    {
        get
        {
            return _status ?? string.Empty;
        }
        set => _status = string.IsNullOrEmpty(value) ? IrasUserStatus.Active : value.ToLower();
    }

    public DateTime? LastUpdated { get; set; } = null;
}