using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("/", Name = "acc:[action]")]
public class ResearchAccountController : Controller
{
    public IActionResult Home()
    {
        var lastLogin = HttpContext.Items[ContextItemKeys.LastLogin];

        if (lastLogin == null)
        {
            return View(nameof(Index), null);
        }

        var ukTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        var ukDateTime = TimeZoneInfo.ConvertTimeFromUtc((DateTime)lastLogin, ukTimeZone);
        var formattedDate = ukDateTime.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
        var formattedTime = ukDateTime.ToString("h:mmtt", CultureInfo.InvariantCulture).ToLowerInvariant();

        return View(nameof(Index), $"{formattedDate} at {formattedTime} UK time");
    }
}