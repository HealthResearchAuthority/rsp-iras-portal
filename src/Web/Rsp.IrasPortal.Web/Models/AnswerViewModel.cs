namespace Rsp.IrasPortal.Web.Models;

public class AnswerViewModel
{
    public string AnswerId { get; set; } = null!;
    public string AnswerText { get; set; } = null!;
    public bool IsSelected { get; set; }
}