using Microsoft.AspNetCore.Mvc;

namespace Rsp.IrasPortal.Web.Controllers.Exceptions;

[Route("[controller]")]
public class ExceptionController(ILogger<ExceptionController> logger) : Controller
{
    [HttpGet(Name = "exc:Index")]
    public IActionResult Index(string exceptionId)
    {
        ViewData["exceptionId"] = exceptionId;
        return View("Exception");
    }

    [HttpPost("ServiceException", Name = "exc:ServiceException")]
    public IActionResult ServiceException(ProblemDetails problemDetails)
    {
        var exceptionId = Guid.NewGuid().ToString();

        logger.LogError("Service exception occurred. ExceptionId: {ExceptionId}, ProblemDetails: {@ProblemDetails}",
            exceptionId, problemDetails);

        return RedirectToAction(nameof(Index), new { exceptionId });
    }

    [HttpGet("NotFound", Name = "exc:NotFound")]
    public IActionResult NotFound()
    {
        throw new NotImplementedException();
        //return View("NotFound");
    }
}