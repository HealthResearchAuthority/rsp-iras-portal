using Rsp.Portal.Application.DTOs.Responses;

namespace Rsp.Portal.Web.Areas.Admin.Models;

public class UserAuditTrailViewModel
{
    public string Name { get; set; } = null!;
    public IEnumerable<UserAuditTrailDto> Items { get; set; } = [];
}