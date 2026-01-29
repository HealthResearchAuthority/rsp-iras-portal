namespace Rsp.Portal.Application.DTOs.Responses.CmsContent;

public class MixedContentPageResponse
{
    public string? MetaTitle { get; set; }
    public IDictionary<string, MixedContentPageItem?> ContentItems { get; set; } = new Dictionary<string, MixedContentPageItem?>();
}

public class MixedContentPageItem
{
    public string? ValueType { get; set; }
    public string? Value { get; set; }
}