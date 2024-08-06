using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Domain.Entities;
using Rsp.Logging.Extensions;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "app:[action]")]
public class PlaygroundController(ILogger<PlaygroundController> logger) : Controller
{
    public IActionResult Viewport()
    {
        logger.LogMethodStarted();

        return View();
    }

    [HttpGet]
    public IActionResult Validation()
    {
        logger.LogMethodStarted();

        return View();
    }

    [HttpPost]
    public IActionResult Validation(PlaygroundModel model)
    {
        logger.LogMethodStarted();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        return RedirectToAction(nameof(Success), model);
    }

    public IActionResult Success(PlaygroundModel model)
    {
        logger.LogMethodStarted();

        return View(model);
    }
}