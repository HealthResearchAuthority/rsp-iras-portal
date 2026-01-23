namespace Rsp.Portal.Application.DTOs;
public record AnswerOptionDto
{
    public string OptionId { get; set; } = null!;
    public string OptionText { get; set; } = null!;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string VersionId { get; set; } = null!;
}