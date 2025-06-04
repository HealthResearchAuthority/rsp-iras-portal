namespace Rsp.IrasPortal.Web.Areas.QuestionsManagement.Models;

public record QuestionCategoryViewModel
{
    public string CategoryId { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public string VersionId { get; set; } = null!;
}