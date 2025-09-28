using Rsp.IrasPortal.Application.Constants;

namespace Rsp.IrasPortal.Application.DTOs.Requests;

/// <summary>
/// Represents an application request to create
/// an application skeleton with title and description.
/// </summary>
public record IrasApplicationRequest
{
    /// <summary>
    /// IRAS Project Id
    /// </summary>
    public string? Id { get; set; } = DateTime.Now.ToString("yyyyMMddHHmmss");

    /// <summary>
    /// The title of the project
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// Description of the application
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// The start date of the project
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Application Status
    /// </summary>
    public string? Status { get; set; } = ApplicationStatuses.Draft;

    /// <summary>
    /// Applicant's name who initiated the application
    /// </summary>
    public string CreatedBy { get; set; } = null!;

    /// <summary>
    /// User's name who updated the application
    /// </summary>
    public string UpdatedBy { get; set; } = null!;

    /// <summary>
    /// Respondent creating the application
    /// </summary>
    public RespondentDto Respondent { get; set; } = null!;

    /// <summary>
    /// IRAS ID of the application
    /// </summary>
    public int? IrasId { get; set; }
}