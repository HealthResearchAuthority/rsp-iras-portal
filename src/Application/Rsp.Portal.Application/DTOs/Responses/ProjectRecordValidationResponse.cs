namespace Rsp.Portal.Application.DTOs.Responses;

/// <summary>
/// Represents the result of validating a project record.
/// </summary>
public record ProjectRecordValidationResponse
{
    /// <summary>
    /// UTC timestamp indicating when the validation was executed.
    /// </summary>
    public DateTime TimeStamp { get; set; }

    /// <summary>
    /// Optional error message describing why validation failed; null when validation succeeds.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Optional payload with details about the validated project record.
    /// </summary>
    public ProjectRecordDto? Data { get; set; }
}