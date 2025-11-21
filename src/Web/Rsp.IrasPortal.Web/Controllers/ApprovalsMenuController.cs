using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Domain.AccessControl;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "approvalsmenu:[action]")]
[Authorize(Policy = Workspaces.Approvals)]
public class ApprovalsMenuController : Controller
{
    [Route("/approvalsmenu", Name = "approvalsmenu:welcome")]
    public IActionResult Welcome()
    {
        return View(nameof(Index));
    }
}