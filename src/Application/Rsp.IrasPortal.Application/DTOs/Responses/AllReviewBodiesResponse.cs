namespace Rsp.IrasPortal.Application.DTOs.Responses;

public class AllReviewBodiesResponse
{
    public IEnumerable<ReviewBodyDto> ReviewBodies { get; set; } = [];

    public int TotalCount { get; set; }
}