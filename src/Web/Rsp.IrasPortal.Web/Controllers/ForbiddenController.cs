using Microsoft.AspNetCore.Mvc;

namespace Rsp.IrasPortal.Web;

public class ForbiddenController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
