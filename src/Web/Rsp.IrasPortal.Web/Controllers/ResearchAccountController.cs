using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;

namespace Rsp.IrasPortal.Web.Controllers;

// Route attribute sets the base route for all actions in this controller
[Route("/", Name = "acc:[action]")]
public class ResearchAccountController : Controller
{
    // Home action clears session and temp data, then displays last login info if available
    public IActionResult Home()
    {
        // after signing in, this is where user lands first
        // if the user is disabled, block access
        if (User.Identity?.IsAuthenticated is true)
        {
            if (User.FindFirst(CustomClaimTypes.UserStatus)?.Value is IrasUserStatus.Disabled)
            {
                return Forbid();
            }
        }

        // check if notification banner needs to be shown before clearing session
        var notificationBannerTemp = TempData[TempDataKeys.ShowNotificationBanner];
        var cookiesSavedBannerTemp = TempData[TempDataKeys.ShowCookiesSavedHeaderBanner];

        // Clear session and TempData to ensure a fresh state
        HttpContext.Session.Clear();
        TempData.Clear();

        // re-add notification banner temp key so it displays in the view
        TempData[TempDataKeys.ShowNotificationBanner] = notificationBannerTemp;
        TempData[TempDataKeys.ShowCookiesSavedHeaderBanner] = cookiesSavedBannerTemp;

        // Retrieve the last login time from HttpContext items
        var lastLogin = HttpContext.Items[ContextItemKeys.LastLogin];

        // If last login is not available, return the Index view with no model
        if (lastLogin == null)
        {
            return View(nameof(Index), null);
        }

        // Convert the last login time from UTC to UK time zone
        var ukTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        var ukDateTime = TimeZoneInfo.ConvertTimeFromUtc((DateTime)lastLogin, ukTimeZone);

        // Format the date and time for display
        var formattedDate = ukDateTime.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
        var formattedTime = ukDateTime.ToString("h:mmtt", CultureInfo.InvariantCulture).ToLowerInvariant();

        // Return the Index view with the formatted last login string as the model
        return View(nameof(Index), $"{formattedDate} at {formattedTime} UK time");
    }
}