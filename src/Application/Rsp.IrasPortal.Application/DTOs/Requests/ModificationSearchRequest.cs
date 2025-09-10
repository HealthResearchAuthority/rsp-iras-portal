namespace Rsp.IrasPortal.Application.DTOs.Requests;

public class ModificationSearchRequest
{
    public string? IrasId { get; set; }
    public string? ChiefInvestigatorName { get; set; }
    public string? ShortProjectTitle { get; set; }
    public string? SponsorOrganisation { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<string> LeadNation { get; set; } = [];
    public List<string> ParticipatingNation { get; set; } = [];
    public List<string> ModificationTypes { get; set; } = [];
    public string? ReviewerId { get; set; } = null;
    public bool IncludeReviewerId { get; set; }
}