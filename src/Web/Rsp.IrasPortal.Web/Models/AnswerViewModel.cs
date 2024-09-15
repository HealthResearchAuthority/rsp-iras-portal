using Rsp.IrasPortal.Application.DTOs;

namespace Rsp.IrasPortal.Web.Models;

public class AnswerViewModel
{
    public QuestionsResponse? ParentQuestion { get; set; }
    public string AnswerId { get; set; } = null!;
    public string AnswerText { get; set; } = null!;
    public string? Value { get; set; }
    public bool IsSelected => Value is not null and not "false";
}