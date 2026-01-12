namespace Rsp.Portal.Application.DTOs;

/// <summary>
/// Represents question sections response returned by the QuestionSet API
/// </summary>
public record QuestionSectionsResponse
{
    public string QuestionCategoryId { get; set; } = null!;
    public string? SectionId { get; set; } = null!;
    public string SectionName { get; set; } = null!;
    public string StaticViewName { get; set; } = null!;
    public bool IsMandatory { get; set; }
    public int Sequence { get; set; }
    public bool IsLastSectionBeforeReview { get; set; }
    public bool StoreUrlReferrer { get; set; }
    public bool EvaluateBackRoute { get; set; }
}