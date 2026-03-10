using Refit;
using Rsp.IrasPortal.Application.DTOs.Responses;

namespace Rsp.IrasPortal.Application.ServiceClients;

public interface IUserNotificationsServiceClient
{
    /// <summary>
    /// Returns all notifications for a user
    /// </summary>
    [Get("/UserNotifications/user/{userId}")]
    public Task<IApiResponse<UserNotificationsResponse>> GetUserNotifications
    (
        string userId,
        int pageNumber,
        int pageSize,
        string sortField,
        string sortDirection,
        string? type = null
    );

    /// <summary>
    /// Mark user notifications as read
    /// </summary>
    [Patch("/UserNotifications/read/{userId}")]
    public Task<IApiResponse> ReadUserNotifications(string userId);

    /// <summary>
    /// Get count of unread notifications for a user
    /// </summary>
    [Get("/UserNotifications/notifications/{userId}")]
    public Task<IApiResponse<int>> GetUnreadUserNotificationsCount(string userId);
}