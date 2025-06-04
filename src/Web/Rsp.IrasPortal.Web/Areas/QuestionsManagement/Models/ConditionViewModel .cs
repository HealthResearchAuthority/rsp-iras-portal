namespace Rsp.IrasPortal.Web.Areas.QuestionsManagement.Models;

using System.Collections.Generic;

public class ConditionViewModel
{
    // Whether condition is AND or OR
    public string Mode { get; set; } = "AND";

    // IN clause Option Type: "Single", "Multi", or "Exact"
    public string OptionType { get; set; } = "Single";

    // Answer Option IDs from the parent question (comma-separated from UI)
    public string ParentOptionsAsString { get; set; }

    // Converted internally from ParentOptionsAsString
    public List<string> ParentOptions =>
        string.IsNullOrWhiteSpace(ParentOptionsAsString)
        ? []
        : [.. ParentOptionsAsString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    // Whether the condition should be negated (i.e., NOT IN)
    public bool Negate { get; set; }

    // Used for rendering if condition applies
    public bool IsApplicable { get; set; }
}