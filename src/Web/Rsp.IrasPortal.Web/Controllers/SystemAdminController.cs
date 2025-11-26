using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Domain.AccessControl;

namespace Rsp.IrasPortal.Web.Controllers;

[Authorize(Policy = Workspaces.SystemAdministration)]
[Route("[controller]/[action]", Name = "systemadmin:[action]")]
public class SystemAdminController : Controller
{
    private const string SystemAdminView = nameof(Index);

    [Route("/systemadmin", Name = "systemadmin:view")]
    public IActionResult Index()
    {
        return View(SystemAdminView);
    }
}