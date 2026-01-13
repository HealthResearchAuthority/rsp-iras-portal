using Rsp.Portal.Domain.Identity;

namespace Rsp.Portal.Application.DTOs;

public class UserResponse
{
    public User User { get; set; } = null!;

    public IEnumerable<string> Roles { get; set; } = [];

    public IEnumerable<string> AccessRequired { get; set; } = [];
}