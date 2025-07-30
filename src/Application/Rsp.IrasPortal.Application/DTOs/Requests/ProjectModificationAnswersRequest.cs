namespace Rsp.IrasPortal.Application.DTOs.Requests;

/// <summary>
/// Represents a request to create
/// modification answers in the database.
/// </summary>
public record ProjectModificationAnswersRequest
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
    /// Project personnel
    /// </summary>
    public string ProjectPersonnelId { get; set; } = null!;

    /// <summary>
    /// Respondent Answers
    /// </summary>
    public List<RespondentAnswerDto> ModificationAnswers { get; set; } = [];
}