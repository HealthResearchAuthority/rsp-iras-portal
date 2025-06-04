namespace Rsp.IrasPortal.Web.Areas.QuestionsManagement.Models;

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

public class RuleViewModel
{
    // General metadata
    public string QuestionId { get; set; }

    public string QuestionText { get; set; }

    public string RuleType { get; set; } // "Conditional" or "Validation"

    // For conditional rules
    public string ParentQuestionId { get; set; }

    public List<SelectListItem> ParentQuestions { get; set; } = [];

    public string Mode { get; set; } = "AND"; // Rule-level logic: AND or OR

    public List<ConditionViewModel> Conditions { get; set; } = [];

    // For validation rules
    public string ValidationType { get; set; } // "Email", "Phone", "Length", "Regex", "Date"

    public string ValidationValue { get; set; } // MaxLength or Regex pattern etc.
}