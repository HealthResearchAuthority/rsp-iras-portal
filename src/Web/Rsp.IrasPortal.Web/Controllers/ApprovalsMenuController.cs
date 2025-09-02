using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "approvalsmenu:[action]")]
[Authorize(Roles = "system_administrator,workflow_co-ordinator,team_manager,study-wide_reviewer")]
public class ApprovalsMenuController : Controller
{
    [Route("/approvalsmenu", Name = "approvalsmenu:welcome")]
    public IActionResult Welcome()
    {
        return View(nameof(Index));
    }
}