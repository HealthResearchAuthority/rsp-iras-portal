namespace Rsp.IrasPortal.Application.DTOs.Requests;

public class ProjectRecordSearchRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public string? IrasId { get; set; }
    public string? ChiefInvestigatorName { get; set; }
    public List<string>? LeadNation { get; set; } = [];
    public List<string>? ParticipatingNation { get; set; } = [];
    public string? ShortProjectTitle { get; set; }
    public string? SponsorOrganisation { get; set; }
    public bool ActiveProjectsOnly { get; set; }
}