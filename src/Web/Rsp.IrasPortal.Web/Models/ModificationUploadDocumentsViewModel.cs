namespace Rsp.IrasPortal.Web.Models;

public class ModificationUploadDocumentsViewModel : BaseProjectModificationViewModel
{
    public string SpecificAreaOfChange { get; set; } = string.Empty;

    public List<IFormFile> Files { get; set; } = [];
}