namespace Rsp.IrasPortal.Web.Models;

public class PlannedEndDateViewModel
{
    public string IrasId { get; set; }
    public string ShortTitle { get; set; }
    public string ModificationIdentifier { get; set; }
    public DateTime? CurrentPlannedEndDate { get; set; }
    public DateTime? NewPlannedEndDate { get; set; }
    public string PageTitle { get; set; }
}