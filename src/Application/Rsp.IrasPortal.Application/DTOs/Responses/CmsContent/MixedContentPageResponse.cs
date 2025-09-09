namespace Rsp.IrasPortal.Web.Models.CmsContent;

public class MixedContentPageResponse
{
    public IDictionary<string, string?> ContentItems { get; set; } = new Dictionary<string, string?>();
}

public class MixedContentPageItem
{
    public string? ContentPlaceholderAlias { get; set; }
    public string? ContentValue { get; set; }
}