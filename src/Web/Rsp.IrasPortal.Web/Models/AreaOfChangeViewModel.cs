using Microsoft.AspNetCore.Mvc.Rendering;

namespace Rsp.IrasPortal.Web.Models;

public class AreaOfChangeViewModel
{
    public string IrasId { get; set; }
    public string ShortTitle { get; set; }
    public string ModificationIdentifier { get; set; }
    public int? AreaOfChangeId { get; set; }
    public int? SpecificChangeId { get; set; }

    public IEnumerable<SelectListItem> AreaOfChangeOptions { get; set; }
    public IEnumerable<SelectListItem> SpecificChangeOptions { get; set; }
}