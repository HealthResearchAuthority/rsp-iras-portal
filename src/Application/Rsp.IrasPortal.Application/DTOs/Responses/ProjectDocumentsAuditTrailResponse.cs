using Rsp.Portal.Application.DTOs.Requests;

namespace Rsp.IrasPortal.Application.DTOs.Responses;

public class ProjectDocumentsAuditTrailResponse
{
    public IEnumerable<ModificationDocumentsAuditTrailDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
}