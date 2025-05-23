namespace Rsp.IrasPortal.Web.Models;

public class ResearchApplicationSummaryModel
{
    public int? IrasId { get; set; }

    public string? ApplicatonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsNew { get; set; } = false; // temporary for first iteration
    public DateTime ProjectEndDate { get; set; }
}