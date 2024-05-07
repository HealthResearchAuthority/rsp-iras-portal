using Microsoft.AspNetCore.Mvc;
using Rsp.Logging.Domain;
using Rsp.Logging.Extensions;

namespace Rsp.IrasPortal.Web;

[Route("[controller]")]
public class ForbiddenController(ILogger<ForbiddenController> logger) : Controller
{
    [Route("")]
    public IActionResult Index()
    {
        logger.LogErrorHp(LogEvents.UnhandledException.Code, LogEvents.UnhandledException.Description);

        return View();
    }
}