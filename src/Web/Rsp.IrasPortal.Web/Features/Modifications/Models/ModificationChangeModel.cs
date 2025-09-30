using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.Web.Features.Modifications.Models;

public class ModificationChangeModel
{
    public Guid ModificationChangeId { get; set; }
    public string? ModificationType { get; set; }
    public string? Category { get; set; }
    public string? ReviewType { get; set; }
    public string AreaOfChangeName { get; set; } = null!;
    public string SpecificChangeName { get; set; } = null!;
    public string SpecificAreaOfChangeId { get; set; } = null!;
    public string? SpecificChangeAnswer { get; set; }
    public string ChangeStatus { get; set; } = null!;
    public List<QuestionViewModel> Questions { get; set; } = [];
    public List<SupportingDocumentModel> SupportingDocuments { get; set; } = [];
}