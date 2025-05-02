namespace Rsp.IrasPortal.Web.Models;

public class QuestionnaireViewModel
{
    public bool ReviewAnswers { get; set; }
    public string CurrentStage { get; set; } = "";
    public List<QuestionViewModel> Questions { get; set; } = [];

    public List<object> GetConditionalRules()
    {
        return [.. Questions
            .Where(q => !q.IsMandatory && q.Rules.Any())
            .Select(q => new
            {
                q.QuestionId,
                q.Rules
            })];
    }
}