namespace Rsp.Portal.Application.DTOs;
public record AnswerDto
{
    public string AnswerId { get; set; } = null!;
    public string AnswerText { get; set; } = null!;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string VersionId { get; set; } = null!;
}