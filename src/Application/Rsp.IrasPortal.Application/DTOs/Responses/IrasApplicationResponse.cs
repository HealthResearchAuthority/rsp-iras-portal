namespace Rsp.Portal.Application.DTOs.Responses;

public class IrasApplicationResponse
{
    /// <summary>
    /// The public key for the application database record
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// The title of the project
    /// </summary>
    public string ShortProjectTitle { get; set; } = null!;

    /// <summary>
    /// Description of the application
    /// </summary>
    public string FullProjectTitle { get; set; } = null!;

    /// <summary>
    /// Application Status
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Indicates if the application is active or not
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// DateTime when the application was created
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// DateTime when the application was updated
    /// </summary>
    public DateTime UpdatedDate { get; set; }

    /// <summary>
    /// UserId of the person initiated the application
    /// </summary>
    public string CreatedBy { get; set; } = null!;

    /// <summary>
    /// UserId of the person updated the application
    /// </summary>
    public string UpdatedBy { get; set; } = null!;

    /// <summary>
    /// IRAS ID of the application
    /// </summary>
    public int? IrasId { get; set; }
}