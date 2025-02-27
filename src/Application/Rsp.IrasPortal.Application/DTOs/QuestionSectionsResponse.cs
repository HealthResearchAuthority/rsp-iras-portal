namespace Rsp.IrasPortal.Application.DTOs;

/// <summary>
/// Represents question sections response returned by the QuestionSet API
/// </summary>
public record QuestionSectionsResponse
{


       public string QuestionCategoryId { get; set; } = null!;
    public string? SectionId { get; set; } = null!;
    public string SectionName { get; set; } = null!;
}