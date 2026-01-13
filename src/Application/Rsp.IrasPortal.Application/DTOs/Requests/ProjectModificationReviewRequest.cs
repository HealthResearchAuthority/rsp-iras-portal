namespace Rsp.Portal.Application.DTOs.Requests;

public record ProjectModificationReviewRequest
{
    public Guid ProjectModificationId { get; set; }
    public string Outcome { get; set; } = null!;
    public string? Comment { get; set; }
    public string? ReasonNotApproved { get; set; }
}