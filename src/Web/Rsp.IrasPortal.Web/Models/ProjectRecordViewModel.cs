using Rsp.Portal.Application.DTOs;

namespace Rsp.Portal.Web.Models;

/// <summary>
/// Represents the basic project details including IrasId, short and full project title.
/// </summary>
public class ProjectRecordViewModel : QuestionnaireViewModel
{
    /// <summary>
    /// Gets or sets current project IrasId
    /// </summary>
    public int IrasId { get; set; }

    /// <summary>
    /// Gets or sets the title of the project.
    /// </summary>
    public string ShortProjectTitle { get; set; } = null!;

    /// <summary>
    /// Full project title
    /// </summary>
    public string FullProjectTitle { get; set; } = null!;

    /// <summary>
    /// Identifier for the section in the questionnaire.
    /// </summary>
    public string SectionId { get; set; } = null!;

    /// <summary>
    /// Lead nation
    /// </summary>
    public string LeadNation { get; set; } = null!;

    /// <summary>
    /// Is NHS / HSC Organisation
    /// </summary>
    public bool IsNHSHSCOrganisation { get; set; }

    /// <summary>
    /// Lead nation selected option
    /// </summary>
    public string LeadNationSelectedOption { get; set; } = null!;

    /// <summary>
    /// Is NHS / HSC Organisation selected option
    /// </summary>
    public string IsNHSHSCOrganisationSelectedOption { get; set; }

    public ReviewBodyDto? ReviewBody { get; set; }
}