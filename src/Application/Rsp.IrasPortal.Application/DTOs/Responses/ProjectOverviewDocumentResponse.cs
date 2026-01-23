namespace Rsp.Portal.Application.DTOs.Responses;

public class ProjectOverviewDocumentResponse
{
    /// <summary>
    /// The unique record identifier for the project.
    /// </summary>
    public string ProjectRecordId { get; set; } = null!;

    /// <summary>
    /// A collection of modification DTOs associated with this response.
    /// </summary>
    public IEnumerable<ProjectOverviewDocumentDto> Documents { get; set; } = [];

    /// <summary>
    /// The total count of modifications associated with the application.
    /// </summary>
    public int TotalCount { get; set; }
}