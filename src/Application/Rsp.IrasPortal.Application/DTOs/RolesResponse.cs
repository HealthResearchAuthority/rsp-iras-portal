using Rsp.IrasPortal.Domain.Identity;

namespace Rsp.IrasPortal.Application.DTOs;

public class RolesResponse
{
    public IEnumerable<Role> Roles { get; set; } = [];

    public int TotalCount { get; set; }
}