namespace Rsp.Portal.Application.DTOs.Requests;

public class ProjectOverviewDocumentSearchRequest
{
    public string? IrasId { get; set; }
    public Dictionary<string, string> DocumentTypes { get; set; } = [];
    public List<string> AllowedStatuses { get; set; } = [];
}