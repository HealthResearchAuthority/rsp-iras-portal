namespace Rsp.IrasPortal.Web.Models;

/// <summary>
/// Represents an overview of a project, including its title, category, and record identifier.
/// </summary>
public class ProjectOverviewModel
{
    /// <summary>
    /// Gets or sets the title of the project.
    /// </summary>
    public string? ProjectTitle { get; set; }

    /// <summary>
    /// Gets or sets the category identifier for the project.
    /// </summary>
    public string? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the unique record identifier for the project.
    /// </summary>
    public string? ProjectRecordId { get; set; }

    /// <summary>
    /// Gets or sets the planned end date of the project.
    /// </summary>
    public string? ProjectPlannedEndDate { get; set; }

    // <summary>
    /// Gets current project IrasId
    /// </summary>
    public int? IrasId { get; set; }

    // <summary>
    /// Gets current project status
    /// </summary>
    public string Status { get; set; } = null!;
}