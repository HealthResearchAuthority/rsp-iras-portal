namespace Rsp.IrasPortal.Web.Models;

public class QuestionnaireViewModel
{
    public bool ReviewAnswers { get; set; }
    public string CurrentStage { get; set; } = "";
    public List<QuestionViewModel> Questions { get; set; } = [];
}