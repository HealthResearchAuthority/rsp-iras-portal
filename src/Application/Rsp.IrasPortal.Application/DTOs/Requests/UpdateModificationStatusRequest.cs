namespace Rsp.Portal.Application.DTOs.Requests;

public class UpdateModificationStatusRequest
{
    public string ProjectRecordId { get; init; } = default!;
    public Guid ModificationId { get; init; }
    public string Status { get; init; } = default!;
    public string? ReasonNotApproved { get; init; }
    public string? Response { get; init; }
    public string? Role { get; init; }
    public string? ResponseOrigin { get; init; }
}