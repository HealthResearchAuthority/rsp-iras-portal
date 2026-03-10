using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Features.UserNotifications.Models;

public class UserNotificationsViewModel
{
    public IEnumerable<UserNotificationResponse> Notifications { get; set; } = [];
    public string? NotificationType { get; set; }
    public PaginationViewModel? Pagination { get; set; }
}