namespace Rsp.IrasPortal.Application.DTOs.Requests;

/// <summary>
/// Represents a request to create
/// respondent answers in the database.
/// </summary>
public record RespondentAnswersRequest
{
    /// <summary>
    /// Respondent's Id
    /// </summary>
    public string RespondentId { get; set; } = null!;

    /// <summary>
    /// Application Id
    /// </summary>
    public string ApplicationId { get; set; } = null!;

    /// <summary>
    /// Respondent Answers
    /// </summary>
    public List<RespondentAnswerDto> RespondentAnswers { get; set; } = [];
}