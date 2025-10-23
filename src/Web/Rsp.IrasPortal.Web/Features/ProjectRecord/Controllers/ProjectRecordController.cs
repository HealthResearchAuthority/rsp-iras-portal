using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rsp.IrasPortal.Web.Features.ProjectRecord.Controllers;

[ExcludeFromCodeCoverage]
[Route("[controller]/[action]", Name = "prc:[action]")]
[Authorize(Policy = "IsApplicant")]
public class ProjectRecordController : Controller
{
    public IActionResult ProjectRecordExists()
    {
        return View();
    }

    public IActionResult ProjectNotEligible()
    {
        return View();
    }
}