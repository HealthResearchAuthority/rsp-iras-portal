using Rsp.IrasPortal.Application.DTOs.Responses;

namespace Rsp.IrasPortal.Web.Features.Modifications.Models;

public class AuditTrailModel
{
    public ProjectModificationAuditTrailResponse AuditTrail { get; set; } = null!;
    public string ModificationIdentifier { get; set; } = null!;
    public string ShortTitle { get; set; } = null!;
    public string ProjectRecordId { get; set; } = null!;
}