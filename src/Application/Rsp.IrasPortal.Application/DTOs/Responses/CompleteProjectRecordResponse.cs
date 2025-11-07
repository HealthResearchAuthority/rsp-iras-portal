namespace Rsp.IrasPortal.Application.DTOs.Responses;

public class CompleteProjectRecordResponse
{
    public string Id { get; set; } = null!;
    public string ShortProjectTitle { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public string? Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string UpdatedBy { get; set; } = null!;
    public int? IrasId { get; set; }
    public string ChiefInvestigator { get; set; } = null!;
    public string LeadNation { get; set; } = null!;
    public string ParticipatingNation { get; set; } = null!;
    public string SponsorOrganisation { get; set; } = null!;
}