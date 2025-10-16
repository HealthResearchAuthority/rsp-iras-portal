namespace Rsp.IrasPortal.Application.DTOs.Requests;

public class ApplicationSearchRequest
{
    public string? SearchTitleTerm { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public List<string> Status { get; set; } = [];
}