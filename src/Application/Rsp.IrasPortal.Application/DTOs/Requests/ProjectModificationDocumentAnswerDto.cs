namespace Rsp.Portal.Application.DTOs.Requests;

/// <summary>
/// Represents a respondent's answer to a question
/// </summary>
public class ProjectModificationDocumentAnswerDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the modification.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Modification Document Id
    /// </summary>
    public Guid ModificationDocumentId { get; set; }

    /// <summary>
    /// Question Id
    /// </summary>
    public string QuestionId { get; set; } = null!;

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
}