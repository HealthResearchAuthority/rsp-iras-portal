using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;
using Rsp.IrasPortal.Web.Helpers;

namespace Rsp.IrasPortal.Web.Models;

public class QuestionViewModel
{
    public Guid? Id { get; set; }
    public int Index { get; set; }
    public string QuestionId { get; set; } = null!;
    public string VersionId { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string SectionId { get; set; } = null!;
    public string Section { get; set; } = null!;
    public int SectionSequence { get; set; }
    public int Sequence { get; set; }
    public string? Heading { get; set; }
    public string QuestionText { get; set; } = null!;
    public string QuestionType { get; set; } = null!;
    public string DataType { get; set; } = null!;
    public bool IsMandatory { get; set; }
    public bool IsOptional { get; set; }
    private string? _answerText;

    public string? AnswerText
    {
        get => _answerText;
        set
        {
            _answerText = value;
            TryPopulateDateFields();
        }
    }

    public string? SelectedOption { get; set; }
    public List<AnswerViewModel> Answers { get; set; } = [];
    public IList<RuleDto> Rules { get; set; } = [];
    public string ShortQuestionText { get; set; } = null!;
    public bool IsModificationQuestion { get; set; }
    public bool ShowOriginalAnswer { get; set; }
    public string ShowAnswerOn { get; set; } = string.Empty;
    public string? SectionGroup { get; set; }
    public int SequenceInSectionGroup { get; set; }
    public bool IsEditable { get; set; }
    public IList<ComponentContent> GuidanceComponents { get; set; } = [];

    public string? NhsInvolvment { get; set; }
    public string? NonNhsInvolvment { get; set; }
    public bool AffectedOrganisations { get; set; }
    public bool RequireAdditionalResources { get; set; }
    public bool UseAnswerForNextSection { get; set; }
    public string PlaybackQuestionId { get; set; } = string.Empty;

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

    public string GetModelKey() => DataType.ToLower() switch
    {
        "date" or "text" or "text area" or "email" => $"Questions[{Index}].AnswerText",
        "checkbox" => $"Questions[{Index}].Answers",
        "radio button" or "boolean" or "look-up list" or "dropdown" => $"Questions[{Index}].SelectedOption",
        _ => ""
    };

    public string GetDisplayText(bool includePrompt = true)
    {
        if (!string.IsNullOrWhiteSpace(AnswerText))
        {
            if (DataType.Equals("Date", StringComparison.OrdinalIgnoreCase))
            {
                return DateHelper.ConvertDateToString(AnswerText);
            }
            return AnswerText!;
        }

        if ((DataType.Equals("radio button", StringComparison.OrdinalIgnoreCase) ||
             DataType.Equals("boolean", StringComparison.OrdinalIgnoreCase) ||
             DataType.Equals("dropdown", StringComparison.OrdinalIgnoreCase)) &&
            !string.IsNullOrWhiteSpace(SelectedOption))
        {
            var answerText = Answers.FirstOrDefault(a => a.AnswerId == SelectedOption)?.AnswerText;

            if (!string.IsNullOrWhiteSpace(answerText))
            {
                return answerText;
            }

            return includePrompt ? $"Enter {QuestionText.ToLowerInvariant()}" : string.Empty;
        }

        if (Answers?.Any(a => a.IsSelected) == true)
        {
            return string.Join("<br/>", Answers.Where(a => a.IsSelected).Select(a => a.AnswerText));
        }

        if (includePrompt)
        {
            var label = string.IsNullOrWhiteSpace(ShortQuestionText) ? QuestionText : ShortQuestionText;
            return $"Enter {label.ToLowerInvariant()}";
        }

        return string.Empty;
    }

    public string GetActionText()
    {
        var label = string.IsNullOrWhiteSpace(ShortQuestionText) ? QuestionText : ShortQuestionText;

        var isAnswered = !string.IsNullOrWhiteSpace(AnswerText)
                     || Answers.Any(a => a.IsSelected)
                     || (!string.IsNullOrWhiteSpace(SelectedOption) && Answers.Any(a => a.AnswerId == SelectedOption));

        var labelText = label.Contains("NHS / HSC", StringComparison.OrdinalIgnoreCase)
            ? label
            : label.ToLowerInvariant();

        return isAnswered ? "Change" : $"Enter {labelText}";
    }

    public bool IsMissingAnswer()
    {
        return string.IsNullOrWhiteSpace(AnswerText)
               && string.IsNullOrWhiteSpace(SelectedOption)
               && Answers?.Count(a => a.IsSelected) == 0;
    }

    private void SetValue(ref string? field, string? value)
    {
        field = value;
        UpdateAnswerText();
    }

    private void UpdateAnswerText()
    {
        if (!string.IsNullOrWhiteSpace(Day) ||
            !string.IsNullOrWhiteSpace(Month) ||
            !string.IsNullOrWhiteSpace(Year))
        {
            var year = Year ?? "0000";
            var month = (Month ?? "00").PadLeft(2, '0');
            var day = (Day ?? "00").PadLeft(2, '0');
            AnswerText = $"{year}-{month}-{day}";
        }
    }

    private void TryPopulateDateFields()
    {
        if (DataType?.Equals("Date", StringComparison.OrdinalIgnoreCase) != true)
            return;

        if (string.IsNullOrWhiteSpace(_answerText))
            return;

        // Attempt to parse date in YYYY-MM-DD format
        if (DateTime.TryParse(_answerText, out var parsedDate))
        {
            _year = parsedDate.Year.ToString();
            _month = parsedDate.Month.ToString("00");
            _day = parsedDate.Day.ToString("00");
        }
        else
        {
            // Fallback: simple string split
            var parts = _answerText.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 3)
            {
                _year = parts[0];
                _month = parts[1];
                _day = parts[2];
            }
        }
    }
}