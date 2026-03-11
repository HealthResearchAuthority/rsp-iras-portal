using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Services.Extensions;

namespace Rsp.IrasPortal.Services;

public class UserNotificationsService(IUserNotificationsServiceClient client) : IUserNotificationsService
{
    public async Task<ServiceResponse<UserNotificationsResponse>> GetUserNotification
     (
         string userId,
         int pageNumber = 1,
         int pageSize = 20,
         string sortField = nameof(UserNotificationResponse.DateTimeCreated),
         string sortDirection = SortDirections.Descending,
         string? type = null
     )
    {
        var response = await client.GetUserNotifications(userId, pageNumber, pageSize, sortField, sortDirection, type);

        return response.ToServiceResponse();
    }

    public async Task<ServiceResponse> ReadUserNotifications(string userId)
    {
        var response = await client.ReadUserNotifications(userId);

        return response.ToServiceResponse();
    }

    public async Task<ServiceResponse<int>> GetUnreadUserNotificationsCount(string userId)
    {
        var response = await client.GetUnreadUserNotificationsCount(userId);

        return response.ToServiceResponse();
    }
}