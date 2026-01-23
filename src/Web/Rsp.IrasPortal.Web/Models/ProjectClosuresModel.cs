namespace Rsp.Portal.Web.Models;

public class ProjectClosuresModel
{
    public Guid Id { get; set; }
    public string ProjectRecordId { get; set; } = null!;
    public string ShortProjectTitle { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int? IrasId { get; set; } = null;
    public string UserId { get; set; } = null!;
    public string? UserEmail { get; set; } = null;
    public DateTime? DateActioned { get; set; } = null;
    public DateTime? SentToSponsorDate { get; set; } = null;
    public DateTime? ClosureDate { get; set; } = null!;
    public int ProjectClosureNumber { get; set; }
    public string TransactionId { get; set; } = null!;
    public DateViewModel ActualClosureDate { get; set; } = null!;
}