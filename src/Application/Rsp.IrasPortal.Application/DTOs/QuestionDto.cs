namespace Rsp.IrasPortal.Application.DTOs;

public record QuestionDto
{
    public string QuestionId { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string SectionId { get; set; } = null!;
    public string Section { get; set; } = null!;
    public int Sequence { get; set; }
    public string? Heading { get; set; }
    public string QuestionText { get; set; } = null!;
    public string QuestionType { get; set; } = null!;
    public string DataType { get; set; } = null!;
    public bool IsMandatory { get; set; }
    public bool IsOptional { get; set; }
    public IEnumerable<AnswerDto> Answers { get; set; } = [];
    public IEnumerable<RuleDto> Rules { get; set; } = [];
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string VersionId { get; set; } = null!;
    public string ShortQuestionText { get; set; } = null!;
}