using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "qnc:[action]")]
[Authorize(Policy = "IsUser")]
public class ReviewBodyController : Controller
{
    [HttpGet]
    public IActionResult AddReviewBody()
    {
        if (TempData["ReviewBody"] != null)
        {
            var model = JsonSerializer.Deserialize<AddUpdateReviewBodyModel>(TempData["ReviewBody"].ToString());
            return View(model);
        }

        return View(new AddUpdateReviewBodyModel
        {
            Countries = new List<string>()
        });
    }

    [HttpPost]
    public IActionResult AddReviewBody(AddUpdateReviewBodyModel? model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        TempData["ReviewBody"] = JsonSerializer.Serialize(model);
        return RedirectToAction("ReviewAmendments", model);
    }


    [HttpGet]
    public IActionResult ReviewAmendments()
    {
        var model = JsonSerializer.Deserialize<AddUpdateReviewBodyModel>(TempData["ReviewBody"].ToString());
        return View(model);
    }


    [HttpPost]
    public IActionResult ReviewAmendments(AddUpdateReviewBodyModel model)
    {
        TempData["ReviewBody"] = JsonSerializer.Serialize(model);
        return RedirectToAction("ConfirmChanges", model);
    }

    [HttpGet]
    public IActionResult ConfirmChanges(AddUpdateReviewBodyModel model)
    {
        TempData["ReviewBody"] = null;
        return View(model);
    }
}