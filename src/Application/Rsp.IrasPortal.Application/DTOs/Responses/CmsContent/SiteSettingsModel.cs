namespace Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;

public class SiteSettingsModel
{
    public IList<LinkModel>? FooterLinks { get; set; }
    public RichTextProperties? PhaseBannerContent { get; set; }
    public IList<ServiceNavigationItemModel>? ServiceNavigation { get; set; }
}