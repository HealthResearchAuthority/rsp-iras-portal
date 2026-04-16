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
[Route("[action]", Name = "mm:[action]")]
[FeatureGate(FeatureFlags.RecMemberManagement)]
public class MemberManagementController : Controller
{
    [HttpGet]
    public async Task<IActionResult> MemberManagement()
    {
        return View();
    }
}