namespace Rsp.Portal.Application.DTOs;

public record SectionDto
{
    public string SectionId { get; set; } = null!;
    public string QuestionCategoryId { get; set; } = null!;
    public string SectionName { get; set; } = null!;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string VersionId { get; set; } = null!;
}