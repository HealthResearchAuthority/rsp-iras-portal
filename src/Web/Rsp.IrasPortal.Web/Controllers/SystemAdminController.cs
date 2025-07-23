using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "systemadmin:[action]")]
[Authorize(Policy = "IsSystemAdministrator")]
public class SystemAdminController : Controller
{
    private const string SystemAdminView = nameof(Index);

    [Route("/systemadmin", Name = "systemadmin:view")]
    public IActionResult Index()
    {
        return View(SystemAdminView);
    }
}