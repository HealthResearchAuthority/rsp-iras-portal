using System.Data;

namespace Rsp.IrasPortal.Application.DTOs;
public record RuleDto
{
    /// <summary>
    /// Id of the rule. This is used for primary key
    /// </summary>
    public int RuleId { get; set; }

    /// <summary>
    /// Sequence in which multiple rules should apply
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// If multiple rules are, mode indicates if rule is applied as an OR or AND
    /// </summary>
    public string Mode { get; set; } = null!;

    /// <summary>
    /// The Id of the question this rule applies to
    /// </summary>
    public string QuestionId { get; set; } = null!;

    /// <summary>
    /// Parent question Id for this rule
    /// </summary>
    public string? ParentQuestionId { get; set; }

    /// <summary>
    /// Description of the rule to display to the user
    /// e.g. If you have answered Q6 as yes please answer this question
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// List of one or more conditions to apply
    /// </summary>
    public IEnumerable<ConditionDto> Conditions { get; set; } = null!;

    /// <summary>
    /// The question set version this rule belongs to
    /// </summary>
    public string VersionId { get; set; } = null!;

    /// <summary>
    /// returns the distinct applicable conditions by description
    /// </summary>
    public IEnumerable<ConditionDto> GetApplicableConditions()
    {
        return Conditions
                .Where(c => c.IsApplicable && c.Description != null)
                .DistinctBy(c => c.Description);
    }
}