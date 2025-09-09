namespace Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;

public class PropertiesRoot
{
    public bool HasNoContent { get; set; }
    public PageContent? PageContent { get; set; }
    public PageContent? LoginLandingPageAboveTheFold { get; set; }
    public PageContent? LoginLandingPageBelowTheFold { get; set; }
    public List<LinkModel>? RightSideMenuLinks { get; set; }
}