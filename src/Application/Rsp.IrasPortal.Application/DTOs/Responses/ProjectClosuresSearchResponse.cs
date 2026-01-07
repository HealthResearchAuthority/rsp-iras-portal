namespace Rsp.IrasPortal.Application.DTOs.Responses;

public class ProjectClosuresSearchResponse
{
    /// <summary>
    /// A collection of project closures associated with this response.
    /// </summary>
    public IEnumerable<ProjectClosuresResponse> ProjectClosures { get; set; } = [];

    /// <summary>
    /// The total count of modifications associated with the application.
    /// </summary>
    public int TotalCount { get; set; }
}