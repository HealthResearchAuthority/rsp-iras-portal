namespace Rsp.IrasPortal.Application.DTOs.Requests;

public class ModificationSearchRequest
{
    public string? IrasId { get; set; }
    public string? ChiefInvestigatorName { get; set; }
    public string? ShortProjectTitle { get; set; }
    public string? SponsorOrganisation { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<string> Country { get; set; } = [];
    public List<string> ModificationTypes { get; set; } = [];
    public bool UnassignedOnly { get; set; }
}