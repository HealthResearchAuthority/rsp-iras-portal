namespace Rsp.IrasPortal.Application.DTOs.Responses;

public class ProjectClosuresResponse
{
    public IEnumerable<ProjectClosuresDto> ProjectClosures { get; set; } = [];
    public int TotalCount { get; set; }
}