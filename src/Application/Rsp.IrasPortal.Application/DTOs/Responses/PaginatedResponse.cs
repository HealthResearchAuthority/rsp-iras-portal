namespace Rsp.Portal.Application.DTOs.Responses;

public class PaginatedResponse<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
}