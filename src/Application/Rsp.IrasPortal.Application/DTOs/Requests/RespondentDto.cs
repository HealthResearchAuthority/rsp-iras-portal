using System.Text.Json.Serialization;

namespace Rsp.IrasPortal.Application.DTOs.Requests;

/// <summary>
/// Represents a respondent (applicant)
/// </summary>
public record RespondentDto
{
    /// <summary>
    /// Project Application Respondent Id creating/updating the application
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// First Name of the respondent
    /// </summary>
    public string GivenName { get; set; } = null!;

    /// <summary>
    /// Surname of the respondent
    /// </summary>
    public string FamilyName { get; set; } = null!;

    /// <summary>
    /// Email address of the respondent
    /// </summary>
    public string EmailAddress { get; set; } = null!;

    /// <summary>
    /// Role of the Respondent
    /// </summary>
    public string? Role { get; set; }
}