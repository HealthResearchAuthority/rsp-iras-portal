namespace Rsp.IrasPortal.Application.DTOs.Requests;

/// <summary>
/// Represents a request to create respondent answers in the database.
/// </summary>
public record RespondentAnswersRequest
{
    /// <summary>
    /// Gets or sets the respondent's unique identifier.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the application or project record identifier.
    /// </summary>
    public string ProjectRecordId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the list of respondent answers.
    /// </summary>
    public List<RespondentAnswerDto> RespondentAnswers { get; set; } = [];
}