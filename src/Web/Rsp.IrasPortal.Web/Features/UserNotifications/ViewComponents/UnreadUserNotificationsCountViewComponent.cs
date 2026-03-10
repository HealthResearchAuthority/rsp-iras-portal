using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Services;

namespace Rsp.IrasPortal.Web.Features.UserNotifications.ViewComponents;

public class UnreadUserNotificationsCountViewComponent(IUserNotificationsService notificationService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var viewName = "~/Views/Shared/Components/UnreadUserNotificationsCount.cshtml";
        var userId = HttpContext?.User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;

        var response = await notificationService.GetUnreadUserNotificationsCount(userId!);
        if (!response.IsSuccessStatusCode)
        {
            return View(viewName, 0);
        }

        return View(viewName, response.Content);
    }
}