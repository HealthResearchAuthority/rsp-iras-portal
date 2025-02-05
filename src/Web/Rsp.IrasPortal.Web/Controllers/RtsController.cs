using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Services;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]")]
[Authorize(Policy = "IsAdmin")]
public class RtsController : Controller
{
    private readonly IRtsService _rtsService;

    public RtsController(IRtsService rtsService)
    {
        _rtsService = rtsService;
    }

    /// <summary>
    ///     Fetches search results for project-related queries (Name, Description, Sponsor) dynamically.
    ///     This is called via AJAX as the user types.
    /// </summary>
    /// <param name="searchTerm">The query string entered by the user</param>
    /// <returns>A JSON response with matched results</returns>
    [HttpGet]
    public async Task<IActionResult> SearchByName([FromQuery] string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Json(new { success = false, message = "Invalid input" });
        }

        // Call the service method to get search results
        var response = await _rtsService.SearchByName(searchTerm);

        if (response?.Content == null || !response.Content.Any())
        {
            return Json(new { success = true, data = new List<string>() });
        }

        // Extract only the 'Name' property from the DTO
        var names = response.Content.Select(x => x.Name).ToList();

        return Json(new { success = true, data = names });
    }
}