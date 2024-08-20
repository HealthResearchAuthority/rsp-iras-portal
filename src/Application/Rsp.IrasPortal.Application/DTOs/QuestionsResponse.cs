namespace Rsp.IrasPortal.Application.DTOs;

public record QuestionsResponse
{
    public string QuestionId { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string SectionId { get; set; } = null!;
    public string Section { get; set; } = null!;
    public string Heading { get; set; } = null!;
    public string QuestionText { get; set; } = null!;
    public string QuestionType { get; set; } = null!;
    public string DataType { get; set; } = null!;
    public bool IsMandatory { get; set; }
    public bool IsOptional { get; set; }
    public IList<AnswerDto> Answers { get; set; } = [];
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Version { get; set; }
}