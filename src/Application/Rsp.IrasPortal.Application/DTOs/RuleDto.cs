namespace Rsp.IrasPortal.Application.DTOs;
public record RuleDto
{
    public int RuleId { get; set; }
    public int Sequence { get; set; }
    public string Operator { get; set; } = null!;
    public string QuestionId { get; set; } = null!;
    public string ParentQuestionId { get; set; } = null!;
    public string Description { get; set; } = null!;
    public ConditionDto Condition { get; set; } = null!;
}