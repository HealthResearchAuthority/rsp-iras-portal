namespace Rsp.IrasPortal.Web.Models;

public class SectionGroupWithQuestionsViewModel
{
    public string SectionGroup { get; set; } = null!;
    public int SectionSequence { get; set; }
    public List<QuestionViewModel> Questions { get; set; } = [];
}