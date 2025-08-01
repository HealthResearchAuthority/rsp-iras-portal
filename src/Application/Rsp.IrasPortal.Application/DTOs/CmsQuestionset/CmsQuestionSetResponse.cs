namespace Rsp.IrasPortal.Application.DTOs.CmsQuestionset;

public class CmsQuestionSetResponse
{
    public string Id { get; set; } = null!;
    public string Version { get; set; }
    public DateTime? ActiveFrom { get; set; }
    public DateTime? ActiveTo { get; set; }
    public string? Status { get; set; }
    public IList<SectionModel> Sections { get; set; } = new List<SectionModel>();
}