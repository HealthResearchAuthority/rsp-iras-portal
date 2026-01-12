using Rsp.Portal.Domain.Identity;

namespace Rsp.Portal.Application.DTOs;

public class RolesResponse
{
    public IEnumerable<Role> Roles { get; set; } = [];

    public int TotalCount { get; set; }
}