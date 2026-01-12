namespace Rsp.Portal.Web.Models;

public class NotificationBannerModel
{
    public string Title { get; init; } = null!;
    public string Heading { get; init; } = null!;
    public bool IsImportant { get; init; } = false;
    public string? Message { get; init; }
}