namespace Rsp.IrasPortal.Application.DTOs.Requests;

public class RespondentAnswerDto
{
    /// <summary>
    /// Question Id
    /// </summary>
    public string QuestionId { get; set; } = null!;

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
}