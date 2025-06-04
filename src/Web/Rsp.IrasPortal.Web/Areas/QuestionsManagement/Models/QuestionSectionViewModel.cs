namespace Rsp.IrasPortal.Web.Areas.QuestionsManagement.Models;

/// <summary>
/// Represents question sections response returned by the QuestionSet API
/// </summary>
public record QuestionSectionViewModel
{
    public string QuestionCategoryId { get; set; } = null!;
    public string? SectionId { get; set; }
    public string SectionName { get; set; } = null!;
    public string? VersionId { get; set; }
}