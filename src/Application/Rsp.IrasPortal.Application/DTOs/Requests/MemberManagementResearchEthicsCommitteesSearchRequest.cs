namespace Rsp.Portal.Application.DTOs.Requests;

public class MemberManagementResearchEthicsCommitteesSearchRequest
{
    public string? SearchQuery { get; set; }
    public List<string> Country { get; set; } = [];
}