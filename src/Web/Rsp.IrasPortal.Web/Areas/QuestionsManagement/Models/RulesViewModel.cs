using Microsoft.AspNetCore.Mvc.Rendering;
using Rsp.IrasPortal.Web.Areas.QuestionsManagement.Enums;

namespace Rsp.IrasPortal.Web.Areas.QuestionsManagement.Models;

public class RuleViewModel
{
    public int RuleId { get; set; }

    public string RuleType { get; set; } = null!;

    public int Sequence { get; set; }

    public string QuestionId { get; set; } = null!;

    public string QuestionText { get; set; } = null!;

    // For conditional rules
    public string? ParentQuestionId { get; set; }

    public string? ParentQuestionText { get; set; }

    public string? Description { get; set; } = string.Empty;

    public RuleMode Mode { get; set; } // Rule-level logic: AND or OR

    public List<SelectListItem> ParentQuestions { get; set; } = [];

    public List<ConditionViewModel> Conditions { get; set; } = [];
}