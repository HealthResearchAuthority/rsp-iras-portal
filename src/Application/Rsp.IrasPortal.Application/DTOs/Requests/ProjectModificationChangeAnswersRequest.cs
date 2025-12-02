namespace Rsp.IrasPortal.Application.DTOs.Requests;

/// <summary>
/// Represents a request to create
/// modification answers in the database.
/// </summary>
public record ProjectModificationChangeAnswersRequest
{
    /// <summary>
    /// Project modification change Id
    /// </summary>
    public Guid ProjectModificationChangeId { get; set; }

    /// <summary>
    /// Project record Id
    /// </summary>
    public string ProjectRecordId { get; set; } = null!;

    /// <summary>
    /// User Id
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Respondent Answers
    /// </summary>
    public List<RespondentAnswerDto> ModificationChangeAnswers { get; set; } = [];
}