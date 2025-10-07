namespace Rsp.IrasPortal.Application.DTOs.Responses;

public class RankingOfChangeRequest
{
    public string SpecificAreaOfChangeId { get; set; } = null!;
    public string Applicability { get; set; } = null!;
    public string ProjectType { get; set; } = "non-REC";
    public bool IsNHSInvolved { get; set; } = false;
    public bool IsNonNHSInvolved { get; set; } = false;
    public string? NhsOrganisationsAffected { get; set; }
    public bool NhsResourceImplicaitons { get; set; } = false;
    public string? Version { get; set; }
}