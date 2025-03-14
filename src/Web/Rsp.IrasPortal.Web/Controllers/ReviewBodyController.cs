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
        return RedirectToAction("ConfirmReviewBody", model);
    }


    [HttpGet]
    public IActionResult ConfirmReviewBody()
    {
        var model = JsonSerializer.Deserialize<AddUpdateReviewBodyModel>(TempData["ReviewBody"].ToString());
        return View(model);
    }


    [HttpPost]
    public IActionResult ConfirmReviewBody(AddUpdateReviewBodyModel model)
    {
        TempData["ReviewBody"] = JsonSerializer.Serialize(model);
        return RedirectToAction("Success", model);
    }

    [HttpGet]
    public IActionResult Success(AddUpdateReviewBodyModel model)
    {
        TempData["ReviewBody"] = null;
        return View(model);
    }
}