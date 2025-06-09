namespace Rsp.IrasPortal.Web.Areas.QuestionsManagement.Models;

public class QuestionsViewModel
{
    public List<QuestionCategoryViewModel> Categories { get; set; } = [];
    public List<QuestionSectionViewModel> Sections { get; set; } = [];
    public List<QuestionViewModel> Questions { get; set; } = [];
    public List<string> QuestionTypes { get; set; } = [];

    public string VersionId { get; set; } = null!;
    public string SelectedCategory { get; set; } = null!;
    public string SelectedSection { get; set; } = null!;
    public string SelectedType { get; set; } = null!;

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