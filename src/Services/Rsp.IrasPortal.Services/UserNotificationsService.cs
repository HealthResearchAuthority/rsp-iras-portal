using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services.Extensions;

namespace Rsp.IrasPortal.Services;

public class UserNotificationsService(
    IUserNotificationsServiceClient userNotificationsServiceClient
) : IUserNotificationsService
{
    public async Task<ServiceResponse<int>> GetUnreadUserNotificationsCount(string userId)
    {
        var apiResponse = await userNotificationsServiceClient.GetUnreadUserNotificationsCount(userId);
        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<IEnumerable<UserNotificationResponse>>> GetUserNotifications(string userId)
    {
        var apiResponse = await userNotificationsServiceClient.GetUserNotifications(userId);
        return apiResponse.ToServiceResponse();
    }

    public async Task<ServiceResponse<IActionResult>> ReadNotifications(string userId)
    {
        var apiResponse = await userNotificationsServiceClient.ReadNotifications(userId);
        return apiResponse.ToServiceResponse();
    }
}