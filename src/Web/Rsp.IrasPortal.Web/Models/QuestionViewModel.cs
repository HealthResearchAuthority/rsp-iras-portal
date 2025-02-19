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

    private string? _day, _month, _year;

    public string? Day
    {
        get => _day;
        set => SetValue(ref _day, value);
    }

    public string? Month
    {
        get => _month;
        set => SetValue(ref _month, value);
    }

    public string? Year
    {
        get => _year;
        set => SetValue(ref _year, value);
    }

    private void SetValue(ref string? field, string? value)
    {
        field = value;
        UpdateAnswerText();
    }

    private void UpdateAnswerText()
    {
        if (!string.IsNullOrWhiteSpace(Day) &&
            !string.IsNullOrWhiteSpace(Month) &&
            !string.IsNullOrWhiteSpace(Year))
        {
            AnswerText = $"{Year}-{Month.PadLeft(2, '0')}-{Day.PadLeft(2, '0')}";
        }
    }
}