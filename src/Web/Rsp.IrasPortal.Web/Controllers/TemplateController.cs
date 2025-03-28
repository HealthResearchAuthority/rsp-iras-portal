using Microsoft.AspNetCore.Mvc;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("[controller]/[action]")]
public class TemplateController : Controller
{
    public IActionResult Index()
    {
        var templatemodel = TemplateDummyData.GetDummyData();
        return View(templatemodel);
    }
}