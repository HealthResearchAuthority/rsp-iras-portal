namespace Rsp.IrasPortal.Application.DTOs.Requests;

public record RespondentAnswersRequest
{
    /// <summary>
    /// IRAS Project Id
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