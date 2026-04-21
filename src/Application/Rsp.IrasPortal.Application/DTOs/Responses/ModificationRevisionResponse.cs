namespace Rsp.IrasPortal.Application.DTOs.Responses;

public class ModificationRevisionResponse
{
    public Guid Id { get; set; }
    public Guid ProjectModificationId { get; set; }
    public string Response { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string ResponseOrigin { get; set; } = null!;
    public DateTime CreatedDateTime { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime UpdatedDateTime { get; set; }
    public string UpdatedBy { get; set; } = null!;
}