using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Domain.AccessControl;

namespace Rsp.Portal.Web.Features.MemberManagement.Controllers;

/// <summary>
///     Controller responsible for handling Member Management workspace related actions.
/// </summary>
[Authorize(Policy = Workspaces.MemberManagement)]
[FeatureGate(FeatureFlags.RecMemberManagement)]
[Route("membermanagement")]
public class MemberManagementController : Controller
{
    [HttpGet("", Name = "mm:home")]
    public IActionResult Index()
    {
        return View("MemberManagement");
    }
}