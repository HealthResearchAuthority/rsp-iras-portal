using Rsp.IrasPortal.Application.DTOs.Responses;

namespace Rsp.IrasPortal.Web.Areas.Admin.Models;

public class UserAuditTrailViewModel
{
    public string Name { get; set; } = null!;
    public IEnumerable<UserAuditTrailDto> Items { get; set; } = [];
}