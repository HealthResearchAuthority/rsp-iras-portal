namespace Rsp.IrasPortal.Web.Models;

/// <summary>
/// Base view model for project modification-related pages.
/// Contains common project metadata used across multiple views.
/// </summary>
public class BaseProjectModificationViewModel
{
    /// <summary>
    /// Gets or sets the IRAS (Integrated Research Application System) identifier for the project.
    /// </summary>
    public string IrasId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the short title of the project.
    /// </summary>
    public string? ShortTitle { get; set; }

    /// <summary>
    /// Gets or sets the identifier associated with the current project modification.
    /// </summary>
    public string? ModificationId { get; set; }

    /// <summary>
    /// Gets or sets the identifier associated with the current project modification.
    /// </summary>
    public string ModificationIdentifier { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier representing the modification change.
    /// </summary>
    public string? ModificationChangeId { get; set; }

    /// <summary>
    /// Gets or sets the title displayed on the page for context.
    /// </summary>
    public string? SpecificAreaOfChange { get; set; }

    /// <summary>
    /// Gets or sets the Specific Area of Change Id
    /// </summary>
    public string? SpecificAreaOfChangeId { get; set; }

    /// <summary>
    /// Gets or sets the Project Record Id.
    /// </summary>
    public string ProjectRecordId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Status of the Modification
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the created date of the Modification
    /// </summary>
    public string DateCreated { get; set; } = null!;
}