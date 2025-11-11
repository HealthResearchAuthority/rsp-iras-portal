namespace Rsp.IrasPortal.Web.Models;

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
}