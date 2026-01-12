using Rsp.Portal.Domain.Identity;

namespace Rsp.Portal.Application.DTOs;
public record ClaimsResponse
{
    public IEnumerable<Claim> Claims { get; set; } = [];
}