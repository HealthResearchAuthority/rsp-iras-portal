namespace Rsp.IrasPortal.Application.DTOs.Responses;

public class UserAuditTrailDto
{
    public DateTime DateTimeStamp { get; set; }
    public string Description { get; set; } = null!;
    public string SystemAdmin { get; set; } = null!;
}