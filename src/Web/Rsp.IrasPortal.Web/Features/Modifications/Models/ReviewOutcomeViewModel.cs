namespace Rsp.Portal.Web.Features.Modifications.Models;

public class ReviewOutcomeViewModel
{
    public ModificationDetailsViewModel ModificationDetails { get; set; } = new ModificationDetailsViewModel();
    public string? ReviewOutcome { get; set; }
    public string? Comment { get; set; }
    public string? ReasonNotApproved { get; set; }
    public List<string> RequestForInformationReasons { get; set; } = [string.Empty];
}