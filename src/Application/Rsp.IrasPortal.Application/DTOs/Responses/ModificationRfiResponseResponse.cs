namespace Rsp.Portal.Application.DTOs.Responses;

public class ModificationRfiResponseResponse
{
    public Guid ModificationId { get; set; }
    public List<string> RfiResponses { get; set; } = [];
}