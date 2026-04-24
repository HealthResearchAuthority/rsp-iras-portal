using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.Portal.Web.Features.MemberManagement.ResearchEthicsCommittees.Models;

public class MemberManagementResearchEthicsCommitteesViewModel
{
    public MemberManagementResearchEthicsCommitteesSearchModel Search { get; set; } = new();
    public IEnumerable<ReviewBodyDto> ResearchEthicsCommittees { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
}