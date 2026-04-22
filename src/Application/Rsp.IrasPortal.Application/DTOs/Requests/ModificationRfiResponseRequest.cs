namespace Rsp.Portal.Application.DTOs.Requests;

public class ModificationRfiResponseRequest
{
    public Guid ProjectModificationId { get; set; }
    public List<string> Responses { get; set; } = [];
    public string Role { get; set; }
    public string ResponseOrigin { get; set; }
}