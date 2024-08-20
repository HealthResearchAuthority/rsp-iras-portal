namespace Rsp.IrasPortal.Web.Models;

public class QuestionnaireViewModel
{
    //public ILookup<string, QuestionViewModel> Sections { get; set; } = null!;

    public string? PreviousStage { get; set; }

    public string? CurrentStage { get; set; }

    public List<QuestionViewModel> Questions { get; set; } = [];

    public List<QuestionResponse> Answers { get; set; } = [];
}