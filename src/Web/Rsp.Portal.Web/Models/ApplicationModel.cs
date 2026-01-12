namespace Rsp.Portal.Web.Models;

public class ApplicationModel
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
    public int? IrasId { get; set; } = null;
    public string? Description { get; set; } = null;
    public string? CreatedBy { get; set; } = null;
}