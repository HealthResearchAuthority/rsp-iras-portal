using Microsoft.AspNetCore.Mvc;

namespace Rsp.IrasPortal.Web.Controllers;

public partial class ProjectModificationController
{
    [HttpGet]
    public IActionResult ModificationDetailsPage()
    {
        return View();
    }
}