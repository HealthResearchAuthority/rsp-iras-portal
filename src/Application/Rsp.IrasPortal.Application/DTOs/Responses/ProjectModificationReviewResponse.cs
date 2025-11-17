namespace Rsp.IrasPortal.Application.DTOs.Responses;

public record ProjectModificationReviewResponse
{
    public Guid ModificationId { get; set; }
    public string? ReviewOutcome { get; set; }
    public string? Comment { get; set; }
    public string? ReasonNotApproved { get; set; }
}