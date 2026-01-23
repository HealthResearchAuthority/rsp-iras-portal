namespace Rsp.Portal.Application.DTOs.Responses;

public record ProjectModificationAuditTrailDto
{
    public string Id { get; set; } = null!;
    public string ProjectModificationId { get; set; } = null!;
    public DateTime DateTimeStamp { get; set; }
    public string Description { get; set; } = null!;
    public string User { get; set; } = null!;
}