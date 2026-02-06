namespace Rsp.Portal.Application.DTOs.Responses;

public class ReviewBodyAuditTrailDto
{
    public string Id { get; set; } = null!;
    public string RegulatoryBodyId { get; set; } = null!;
    public DateTime DateTimeStamp { get; set; }
    public string Description { get; set; } = null!;
    public string User { get; set; } = null!;
}