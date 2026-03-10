using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.UserNotifications.Models;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Extensions;

namespace Rsp.IrasPortal.Web.Features.UserNotifications.Controllers;

[Authorize]
[Route("[controller]/[action]", Name = "notifications:[action]")]
[FeatureGate(FeatureFlags.UserNotifications)]
public class UserNotificationsController(IUserNotificationsService notificationsService) : Controller
{
    public async Task<IActionResult> UserNotificationsDashboard
    (
        string? type = null,
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(UserNotificationResponse.DateTimeCreated),
        string sortDirection = SortDirections.Descending
    )
    {
        var model = new UserNotificationsViewModel
        {
            NotificationType = type
        };

        // Get the user ID from the claims and retrieve user notifications
        var userId = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
        var userNotifications = await notificationsService.GetUserNotification(userId!, pageNumber, pageSize, sortField, sortDirection, type);

        if (!userNotifications.IsSuccessStatusCode && userNotifications.Content == null)
        {
            return this.ServiceError(userNotifications);
        }

        model.Notifications = userNotifications?.Content?.Notifications!;

        model.Pagination = new PaginationViewModel(pageNumber, pageSize, userNotifications?.Content?.TotalCount ?? 0)
        {
            RouteName = "notifications:UserNotificationsDashboard",
            SortField = sortField,
            SortDirection = sortDirection,
            AdditionalParameters =              {
                 { "type", type ?? string.Empty }
             }
        };

        // Mark all notifications as read after retrieving them
        await notificationsService.ReadUserNotifications(userId!);

        return View(model);
    }
}