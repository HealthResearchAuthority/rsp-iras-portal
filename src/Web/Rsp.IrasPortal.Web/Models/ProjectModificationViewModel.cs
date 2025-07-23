namespace Rsp.IrasPortal.Web.Models;

/// <summary>
/// ViewModel representing a modification to a project record.
/// </summary>
public class ProjectModificationViewModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the project record.
    /// </summary>
    public string ProjectRecordId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the modification number for the project.
    /// </summary>
    public int ModificationNumber { get; set; }

    /// <summary>
    /// Gets or sets the identifier for the specific modification.
    /// </summary>
    public string ModificationIdentifier { get; set; } = null!;
}