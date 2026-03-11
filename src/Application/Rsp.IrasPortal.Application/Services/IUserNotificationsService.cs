using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.Logging.Interceptors;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.Responses;

namespace Rsp.IrasPortal.Application.Services;

public interface IUserNotificationsService : IInterceptable
{
    public Task<ServiceResponse<UserNotificationsResponse>> GetUserNotification
    (
        string userId,
        int pageNumber = 1,
        int pageSize = 20,
        string sortField = nameof(UserNotificationResponse.DateTimeCreated),
        string sortDirection = SortDirections.Descending,
        string? type = null
    );

    public Task<ServiceResponse> ReadUserNotifications(string userId);

    public Task<ServiceResponse<int>> GetUnreadUserNotificationsCount(string userId);
}