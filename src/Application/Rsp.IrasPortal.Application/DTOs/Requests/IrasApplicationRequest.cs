namespace Rsp.IrasService.Application.DTOS.Requests;

public record IrasApplicationRequest
{
    public string ApplicationId { get; set; } = null!;
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
    public string? Status { get; set; } = "created";

    /// <summary>
    /// User Id who initiated the application
    /// </summary>
    public string CreatedBy { get; set; } = null!;

    /// <summary>
    /// User Id who updated the application
    /// </summary>
    public string UpdatedBy { get; set; } = null!;
}