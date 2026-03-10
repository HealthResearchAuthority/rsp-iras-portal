namespace Rsp.IrasPortal.Application.DTOs.Responses;

public class UserNotificationsResponse
{
    public IEnumerable<UserNotificationResponse> Notifications { get; set; } = [];
    public int TotalCount { get; set; }
}