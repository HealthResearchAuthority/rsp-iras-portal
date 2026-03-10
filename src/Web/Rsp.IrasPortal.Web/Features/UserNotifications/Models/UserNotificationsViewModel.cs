using Rsp.Portal.Web.Areas.Admin.Models;

namespace Rsp.IrasPortal.Web.Features.UserNotifications.Models;

public class UserNotificationsViewModel
{
    public IEnumerable<UserNotificationModel> Notifications { get; set; } = [];
    public PaginationViewModel? Pagination { get; set; }
}