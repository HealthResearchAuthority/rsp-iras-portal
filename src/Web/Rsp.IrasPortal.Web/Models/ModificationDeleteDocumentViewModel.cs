using Rsp.IrasPortal.Application.DTOs.Requests;

namespace Rsp.IrasPortal.Web.Models;

public class ModificationDeleteDocumentViewModel
{
    public string? BackRoute { get; set; }
    public List<ProjectModificationDocumentRequest> Documents { get; set; } = new();
}