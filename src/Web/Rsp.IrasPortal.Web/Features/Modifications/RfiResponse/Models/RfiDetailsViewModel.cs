namespace Rsp.IrasPortal.Web.Features.Modifications.RfiResponse.Models;

public class RfiDetailsViewModel
{
    public string? IrasId { get; set; }
    public string? ProjectId { get; set; }
    public string? ShortProjectTitle { get; set; }
    public string? ModificationId { get; set; }
    public string? ModificationGuid { get; set; }
    public string? DateSubmitted { get; set; }
    public IList<string> RfiReasons { get; set; } = [];
}