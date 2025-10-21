using System.Globalization;

namespace Rsp.IrasPortal.Application.DTOs.Requests;

/// <summary>
/// Represents a respondent's answer to a question
/// </summary>
public class RespondentAnswerDto
{
    /// <summary>
    /// Question Id
    /// </summary>
    public string QuestionId { get; set; } = null!;

    /// <summary>
    /// Used for the original question text from project record when surfacing the original question
    /// </summary>
    public string? QuestionText { get; set; }

    /// <summary>
    /// Question Version Id
    /// </summary>
    public string VersionId { get; set; } = null!;

    /// <summary>
    /// Question Category Id
    /// </summary>
    public string CategoryId { get; set; } = null!;

    /// <summary>
    /// Question Section Id
    /// </summary>
    public string SectionId { get; set; } = null!;

    /// <summary>
    /// Freetext response of answer
    /// </summary>
    public string? AnswerText { get; set; }

    /// <summary>
    /// Indicates if the SelectedOption was a single or multiple choice option
    /// </summary>
    public string? OptionType { get; set; }

    /// <summary>
    /// Single selection answer e.g. Boolean (Yes/No)
    /// </summary>
    public string? SelectedOption { get; set; }

    /// <summary>
    /// Multiple answers
    /// </summary>
    public List<string> Answers { get; set; } = [];

    public string GetDisplayText(string dataType)
    {
        if (!string.IsNullOrWhiteSpace(AnswerText))
        {
            if (dataType.Equals("Date", StringComparison.OrdinalIgnoreCase) &&
                DateTime.TryParse(AnswerText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                return parsedDate.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture);
            }

            return AnswerText;
        }

        if ((dataType.Equals("radio button", StringComparison.OrdinalIgnoreCase) ||
             dataType.Equals("boolean", StringComparison.OrdinalIgnoreCase) ||
             dataType.Equals("dropdown", StringComparison.OrdinalIgnoreCase)) &&
            !string.IsNullOrWhiteSpace(SelectedOption))
        {
            return SelectedOption;
        }

        return Answers?.Count switch
        {
            > 0 => string.Join("<br/>", Answers),
            _ => string.Empty
        };
    }
}