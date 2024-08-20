namespace Rsp.IrasPortal.Web.Models;

public class Answer
{
    public string AnswerId { get; set; } = null!;
    public string? Text { get; set; }
    public string? Value { get; set; }
    public bool IsSelected => Value is not null and not "false";
}