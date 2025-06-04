namespace Rsp.IrasPortal.Web.Areas.QuestionsManagement.Models;

public class QuestionsViewModel
{
    public List<QuestionCategoryViewModel> Categories { get; set; } = [];
    public List<QuestionSectionViewModel> Sections { get; set; } = [];
    public List<QuestionViewModel> Questions { get; set; } = [];

    public List<string> GetQuestionTypes()
    {
        return [.. Questions
                .DistinctBy(question => question.DataType)
                .Select(question => question.DataType)];
    }

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