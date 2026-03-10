using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.UserNotifications.Models;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.Services;

namespace Rsp.IrasPortal.Web.Features.UserNotifications.Controllers;

/// <summary>
/// Controller responsible for handling Users Notifications actions.
/// </summary>
[Route("[controller]/[action]", Name = "unc:[action]")]
public class UserNotificationsController(
    IUserNotificationsService userNotificationsService,
    IUserManagementService userManagementService
) : Controller
{
    [HttpGet]
    public async Task<IActionResult> GetUserNotifications()
    {
        // service retrieving, TEMP:
        var currentUserId = User?.FindFirst(CustomClaimTypes.UserId)?.Value ?? Guid.NewGuid().ToString();
        //var notifications = await userNotificationsService.GetUserNotifications(currentUserId);

        // TEMP: TESTING DATA
        var model = new UserNotificationsViewModel()
        {
            Notifications = new List<UserNotificationModel>()
            {
                new UserNotificationModel()
                {
                    Id = "123456",
                    UserId = currentUserId,
                    Text = "Lorem ipsum 111",
                    Type = UserNotificationTypes.Action,
                    DateTimeCreated = DateTime.Now,
                },
                new UserNotificationModel()
                {
                    Id = "234567",
                    UserId = currentUserId,
                    Text = "Lorem ipsum 222",
                    Type = UserNotificationTypes.Information,
                    DateTimeCreated = DateTime.Now,
                },
                new UserNotificationModel()
                {
                    Id = "345678",
                    UserId = currentUserId,
                    Text = "Lorem ipsum 333",
                    Type = UserNotificationTypes.Action,
                    DateTimeCreated = DateTime.Now,
                },
                new UserNotificationModel()
                {
                    Id = "456789",
                    UserId = currentUserId,
                    Text = "Lorem ipsum 444",
                    Type = UserNotificationTypes.Information,
                    DateTimeCreated = DateTime.Now,
                }
            }
        };

        return View("UserNotifications", model);
    }
}