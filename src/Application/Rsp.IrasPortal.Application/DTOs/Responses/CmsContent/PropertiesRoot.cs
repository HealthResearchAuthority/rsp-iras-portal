namespace Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;

public class PropertiesRoot
{
    public bool HasNoContent { get; set; }
    public PageContent? PageContent { get; set; }
    public PageContent? LoginLandingPageBodyText { get; set; }
    public List<LinkModel>? FooterLinks { get; set; }
}