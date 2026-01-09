namespace Rsp.IrasPortal.Application.DTOs.Requests;

public class ProjectClosureRequest
{
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier for the researcher associated with this projectrecord table.
    /// </summary>
    public string ProjectRecordId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the sequential number of the project closure.
    /// </summary>
    public int ProjectClosureNumber { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the project closure. This is a combination of IrasId and ProjectClosureNumber
    /// </summary>
    public string TransactionId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier for researcher entered date from the screen i.e actual project closure date
    /// </summary>
    public DateTime? ClosureDate { get; set; }

    /// <summary>
    /// Gets or sets the identifier for the researcher email.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the identifier for researcher when submit to sponsor for project closure.
    /// </summary>
    public DateTime? SentToSponsorDate { get; set; }

    /// <summary>
    /// Gets or sets the identifier when sponsor takes action to authorise or not authorise project closure.
    /// </summary>
    public DateTime? DateActioned { get; set; }

    /// <summary>
    /// Gets or sets the title of the project.
    /// </summary>
    public string ShortProjectTitle { get; set; } = null!;

    /// <summary>
    /// Gets or sets the IRAS identifier associated with the project, if any.
    /// </summary>
    public int? IrasId { get; set; }

    /// <summary>
    /// Gets or sets the status of the project closure.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// User Id who initiated the application
    /// </summary>
    public string CreatedBy { get; set; } = null!;

    /// <summary>
    /// User Id who updated the application
    /// </summary>
    public string UpdatedBy { get; set; } = null!;
}