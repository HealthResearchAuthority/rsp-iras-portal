using Microsoft.AspNetCore.Mvc;

namespace Rsp.IrasPortal.Web;

[Route("[controller]")]
public class ForbiddenController : Controller
{
    [Route("")]
    public IActionResult Index()
    {
        return View();
    }
}
