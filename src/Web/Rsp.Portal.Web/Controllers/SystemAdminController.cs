using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Domain.AccessControl;

namespace Rsp.Portal.Web.Controllers;

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