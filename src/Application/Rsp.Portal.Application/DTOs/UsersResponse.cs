using Rsp.Portal.Domain.Identity;

namespace Rsp.Portal.Application.DTOs;

public class UsersResponse
{
    public IEnumerable<User> Users { get; set; } = [];

    public IEnumerable<string> UserIds { get; set; } = [];

    public int TotalCount { get; set; }
}