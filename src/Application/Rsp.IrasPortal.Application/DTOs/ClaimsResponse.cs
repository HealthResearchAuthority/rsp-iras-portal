using Rsp.IrasPortal.Domain.Identity;

namespace Rsp.IrasPortal.Application.DTOs;
public record ClaimsResponse
{
    public IEnumerable<Claim> Claims { get; set; } = [];
}