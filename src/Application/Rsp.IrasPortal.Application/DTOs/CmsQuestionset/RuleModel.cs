namespace Rsp.Portal.Application.DTOs.CmsQuestionset;

public class RuleModel
{
    public IList<ConditionModel> Conditions { get; set; }
    public string Description { get; set; }
    public string Mode { get; set; }
    public QuestionModel ParentQuestion { get; set; }
    public string QuestionId { get; set; }
}