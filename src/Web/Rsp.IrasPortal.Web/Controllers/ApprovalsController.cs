using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]", Name = "approvals:[action]")]
[Authorize(Policy = "IsUser")]
public class ApprovalsController : Controller
{
    public IActionResult Welcome() => View(nameof(Index));
    public IActionResult Search() => View(nameof(Search));
}