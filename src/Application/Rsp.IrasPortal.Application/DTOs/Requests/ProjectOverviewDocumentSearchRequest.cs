namespace Rsp.IrasPortal.Application.DTOs.Requests;

public class ProjectOverviewDocumentSearchRequest
{
    public string? IrasId { get; set; }
    public Dictionary<string, string> DocumentTypes { get; set; } = [];
}