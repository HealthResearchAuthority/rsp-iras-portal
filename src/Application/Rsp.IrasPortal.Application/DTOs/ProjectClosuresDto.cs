namespace Rsp.IrasPortal.Application.DTOs;

public class ProjectClosuresDto
{
    public string Id { get; set; } = null!;
    public string ProjectRecordId { get; set; } = null!;
    public string ShortProjectTitle { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int? IrasId { get; set; } = null;
    public string UserId { get; set; } = null!;
    public string CreatedBy { get; set; } = null!;
    public string UpdatedBy { get; set; } = null!;
    public DateTime? DateActioned { get; set; } = null;
    public DateTime? SentToSponsorDate { get; set; } = null;
    public DateTime? ClosureDate { get; set; } = null;
}