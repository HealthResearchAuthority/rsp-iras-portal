namespace Rsp.IrasPortal.Web.Areas.QuestionsManagement.Models;

using Rsp.IrasPortal.Web.Areas.QuestionsManagement.Enums;

public class ConditionViewModel
{
    public ConditionType ConditionType { get; set; }

    // Whether condition is AND or OR
    public ConditionMode Mode { get; set; }

    public ConditionOperator Operator { get; set; }

    // IN clause Option Type: "Single", "Multi", or "Exact"
    public OptionType OptionType { get; set; }

    // Answer Option IDs from the parent question (comma-separated from UI)
    public string ParentOptionsAsString { get; set; } = string.Empty;

    // Converted internally from ParentOptionsAsString
    public List<AnswerViewModel>? ParentOptions { get; set; }

    public string? ParentQuestionType { get; set; }

    public string? Value { get; set; }

    public string? Description { get; set; }

    // Whether the condition should be negated (i.e., NOT IN)
    public bool Negate { get; set; }
}