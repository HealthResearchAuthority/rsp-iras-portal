using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Domain.AccessControl;

namespace Rsp.Portal.Web.Controllers;

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