using Rsp.Portal.Application.DTOs.Responses;

namespace Rsp.Portal.Web.Models;

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

    /// <summary>
    /// Gets or sets current project IrasId
    /// </summary>
    public int? IrasId { get; set; }

    /// <summary>
    /// Gets or sets current project status
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Gets or sets sections with questions for specific view
    /// </summary>
    public List<SectionGroupWithQuestionsViewModel> SectionGroupQuestions { get; set; } = [];

    /// <summary>
    /// Get or sets organisation name for Project Details
    /// </summary>
    public string? OrganisationName { get; set; }

    /// <summary>
    /// Gets or sets the audit trails associated with the project.
    /// </summary>
    public IEnumerable<ProjectRecordAuditTrailDto> AuditTrails { get; set; } = [];
}