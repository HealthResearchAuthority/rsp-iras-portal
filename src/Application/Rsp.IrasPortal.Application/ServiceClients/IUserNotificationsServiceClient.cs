using Microsoft.AspNetCore.Mvc;
using Refit;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.Portal.Application.DTOs.Responses;

namespace Rsp.Portal.Application.ServiceClients;

public interface IUserNotificationsServiceClient
{
    /// <summary>
    /// Returns all notifications for a user by userId, ordered by most recent first
    /// </summary>
    /// <param name="userId">The unique User's identifier.</param>
    /// <returns>An asynchronous operation that returns notifications for given User.</returns>
    [Get("/usernotifications/user/{userId}")]
    public Task<ApiResponse<IEnumerable<UserNotificationResponse>>> GetUserNotifications(string userId);

    /// <summary>
    /// Returns the count of unread notifications for a user
    /// </summary>
    /// <param name="userId">The unique User's identifier.</param>
    /// <returns>An asynchronous operation that returns number of unread notifications for given User.</returns>
    [Get("/usernotifications/notifications/{userId}")]
    public Task<ApiResponse<int>> GetUnreadUserNotificationsCount(string userId);

    /// <summary>
    /// Marks a user notification as read by setting the DateTimeSeen property to the current date and time.
    /// </summary>
    /// <param name="userId">The unique User's identifier.</param>
    /// <returns>An asynchronous operation that marks notifications as read for given User.</returns>
    [Patch("/usernotifications/read/{userId}")]
    public Task<ApiResponse<IActionResult>> ReadNotifications(string userId);
}