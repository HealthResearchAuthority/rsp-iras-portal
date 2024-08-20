namespace Rsp.IrasPortal.Web.Models;

public class QuestionResponse
{
    public string Section { get; set; } = null!;
    public string? Heading { get; set; } = null!;
    public bool IsMandatory { get; set; }
    public string QuestionType { get; set; } = null!;
    public string DataType { get; set; } = null!;
    public string QuestionId { get; set; } = null!;
    public IList<Answer> SelectedAnswers { get; set; } = [];
    public string? AnswerText { get; set; }
}