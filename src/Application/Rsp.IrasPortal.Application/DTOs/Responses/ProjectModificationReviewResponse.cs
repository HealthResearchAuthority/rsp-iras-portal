using Rsp.IrasPortal.Application.DTOs.Responses;

namespace Rsp.Portal.Application.DTOs.Responses;

public record ProjectModificationReviewResponse
{
    public Guid ModificationId { get; set; }
    public string? ReviewOutcome { get; set; }
    public string? Comment { get; set; }
    public string? ReasonNotApproved { get; set; }
    public List<string> RequestForInformationReasons { get; set; } = [];
    public ICollection<ModificationRevisionResponse> ModificationRevisionResponses { get; set; } = [];

    // These 2 params are legacy implementation - to be removed after RFI goes live
    public string? RevisionDescription { get; set; }
    public string? ApplicantRevisionResponse { get; set; }
}