namespace Rsp.Portal.Application.DTOs.Responses;

public class UserAuditTrailResponse
{
    public string Name { get; set; } = null!;
    public IEnumerable<UserAuditTrailDto> Items { get; set; } = [];
}