namespace Rsp.IrasPortal.Application.DTOs.Responses;

public class ProjectRecordAuditTrailDto
{
    public Guid Id { get; set; }
    public string ProjectRecordId { get; set; } = null!;
    public DateTime DateTimeStamp { get; set; }
    public string Description { get; set; } = null!;
    public string User { get; set; } = null!;
}