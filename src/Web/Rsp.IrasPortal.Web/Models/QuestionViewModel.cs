using Rsp.IrasPortal.Application.DTOs;

namespace Rsp.IrasPortal.Web.Models;

public class QuestionViewModel
{
    public int Index { get; set; }
    public string QuestionId { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string SectionId { get; set; } = null!;
    public string Section { get; set; } = null!;
    public int Sequence { get; set; }
    public string? Heading { get; set; }
    public string QuestionText { get; set; } = null!;
    public string QuestionType { get; set; } = null!;
    public string DataType { get; set; } = null!;
    public bool IsMandatory { get; set; }
    public bool IsOptional { get; set; }
    public string? AnswerText { get; set; }
    public string? SelectedOption { get; set; }
    public List<AnswerViewModel> Answers { get; set; } = [];
    public IList<RuleDto> Rules { get; set; } = [];
}