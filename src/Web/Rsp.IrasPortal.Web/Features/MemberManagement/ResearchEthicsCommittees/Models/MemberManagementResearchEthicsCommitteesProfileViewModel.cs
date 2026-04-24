namespace Rsp.IrasPortal.Web.Features.MemberManagement.ResearchEthicsCommittees.Models;

public class MemberManagementResearchEthicsCommitteesProfileViewModel
{
    public Guid Id { get; set; }
    public string? RegulatoryBodyName { get; set; }
    public string? EmailAddress { get; set; }
    public List<string> Countries { get; set; } = new();
    public DateTime? UpdatedDate { get; set; }
}