using Rsp.Portal.Application.DTOs.Requests;

namespace Rsp.Portal.Web.Models;

public class ModificationDeleteDocumentViewModel
{
    public string? BackRoute { get; set; }
    public List<ProjectModificationDocumentRequest> Documents { get; set; } = new();
}