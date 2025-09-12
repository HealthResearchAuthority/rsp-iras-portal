namespace Rsp.IrasPortal.Web.Models;

public class ModificationUploadDocumentsViewModel : BaseProjectModificationViewModel
{
    public List<IFormFile> Files { get; set; } = [];
}