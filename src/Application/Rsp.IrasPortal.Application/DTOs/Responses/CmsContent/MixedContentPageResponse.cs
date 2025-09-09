namespace Rsp.IrasPortal.Web.Models.CmsContent;

public class MixedContentPageResponse
{
    public IDictionary<string, MixedContentPageItem?> ContentItems { get; set; } = new Dictionary<string, MixedContentPageItem?>();
}

public class MixedContentPageItem
{
    public string? ValueType { get; set; }
    public string? Value { get; set; }
}