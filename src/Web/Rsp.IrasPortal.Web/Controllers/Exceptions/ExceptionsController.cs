using Microsoft.AspNetCore.Mvc;

namespace Rsp.IrasPortal.Web.Controllers.Exceptions;

[Route("[controller]/[action]", Name = "exc:[action]")]
public class ExceptionsController(ILogger<ExceptionsController> logger) : Controller
{
    public IActionResult Index(string exceptionId)
    {
        ViewData["exceptionId"] = exceptionId;
        return View();
    }

    public IActionResult ServiceException(ProblemDetails problemDetails)
    {
        var exceptionId = Guid.NewGuid().ToString();

        logger.LogError("Service exception occurred. ExceptionId: {ExceptionId}, ProblemDetails: {@ProblemDetails}",
            exceptionId, problemDetails);

        return RedirectToAction(nameof(Index), new { exceptionId });
    }

    public IActionResult NotFound()
    {
        throw new NotImplementedException();
        //return View("NotFound");
    }
}