namespace Rsp.IrasPortal.Application.DTOs;

public record ProjectInfoDTO
{
    public string? ProjectName { get; set; }

    public string? ProjectLink { get; set; }
    public string? IrasID { get; set; }
}