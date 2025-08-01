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
    public string ModificationIdentifier { get; set; } = null!;

    /// <summary>
    /// Gets or sets the title displayed on the page for context.
    /// </summary>
    public string? PageTitle { get; set; }
}