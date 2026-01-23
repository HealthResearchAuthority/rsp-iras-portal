namespace Rsp.Portal.Application.DTOs.Requests;

public class ModificationDocumentsAuditTrailDto
{
    public Guid Id { get; set; }
    public Guid ProjectModificationId { get; set; }
    public DateTime DateTimeStamp { get; set; }
    public string Description { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string User { get; set; } = null!;
}