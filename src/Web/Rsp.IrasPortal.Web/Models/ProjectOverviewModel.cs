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
    /// Gets or sets current project IrasId
    /// </summary>
    public int? IrasId { get; set; }

    // <summary>
    /// Gets or sets current project status
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets the list of participating nations in the project.
    /// </summary>
    public List<string>? ParticipatingNations { get; set; }

    /// <summary>
    /// Gets or sets the name of the NHS or HSC organisation associated with the project.
    /// </summary>
    public string? NhsOrHscOrganisations { get; set; }

    /// <summary>
    /// Gets or sets the lead nation for the project.
    /// </summary>
    public string? LeadNation { get; set; }

    /// <summary>
    /// Gets or sets the name of the chief investigator for the project.
    /// </summary>
    public string? ChiefInvestigator { get; set; }

    /// <summary>
    /// Gets or sets the name of the primary sponsor organisation.
    /// </summary>
    public string? PrimarySponsorOrganisation { get; set; }

    /// <summary>
    /// Gets or sets the contact person for the sponsor organisation.
    /// </summary>
    public string? SponsorContact { get; set; }
}