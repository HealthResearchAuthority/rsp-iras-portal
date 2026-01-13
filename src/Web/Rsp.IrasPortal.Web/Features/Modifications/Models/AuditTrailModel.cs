using Rsp.Portal.Application.DTOs.Responses;

namespace Rsp.Portal.Web.Features.Modifications.Models;

public class AuditTrailModel
{
    public ProjectModificationAuditTrailResponse AuditTrail { get; set; } = null!;
    public string ModificationIdentifier { get; set; } = null!;
    public string ShortTitle { get; set; } = null!;
    public string ProjectRecordId { get; set; } = null!;
}