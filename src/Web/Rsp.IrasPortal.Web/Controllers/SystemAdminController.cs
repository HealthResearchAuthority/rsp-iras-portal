using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]", Name = "systemadmin:[action]")]
[Authorize(Policy = "IsAdmin")]
public class SystemAdminController : Controller
{
    [Route("", Name = "systemadmin:view")]
    public IActionResult Index()
    {
        return View();
    }
}