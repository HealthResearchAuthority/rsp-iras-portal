namespace Rsp.IrasPortal.Application.DTOs.Responses;

public class GetModificationsResponse
{
    public IEnumerable<ModificationsDto> Modifications { get; set; } = [];
    public int TotalCount { get; set; }
}