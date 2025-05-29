namespace Rsp.IrasPortal.Web.Models;

public class QuestionnaireViewModel
{
    public bool ReviewAnswers { get; set; }
    public string CurrentStage { get; set; } = "";
    public List<QuestionViewModel> Questions { get; set; } = [];

    public string? GetShortProjectTitle()
    {
        return Questions
            .FirstOrDefault(q => q.QuestionText.Equals("Short project title", StringComparison.OrdinalIgnoreCase))?.AnswerText;
    }

    public string? GetFirstCategory()
    {
        return Questions
        .GroupBy(q => q.Section)
        .OrderBy(g => g.First().Sequence)
        .FirstOrDefault()?.FirstOrDefault()?.Category;
    }
}