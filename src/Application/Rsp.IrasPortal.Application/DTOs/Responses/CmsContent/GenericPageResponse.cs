namespace Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;

public class GenericPageResponse
{
    public string? ContentType { get; set; }

    public string? Name { get; set; }

    public DateTime CreateDate { get; set; }

    public DateTime UpdateDate { get; set; }

    public Route? Route { get; set; }

    public Guid Id { get; set; }

    public PropertiesRoot? Properties { get; set; }

    public Dictionary<string, object>? Cultures { get; set; }
}