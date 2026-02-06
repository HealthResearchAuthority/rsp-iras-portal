namespace Rsp.Portal.Application.DTOs.Responses.CmsContent;

public class PropertiesRoot
{
    public string? MetaTitle { get; set; }
    public bool HasNoContent { get; set; }
    public PageContent? PageContent { get; set; }
    public PageContent? LoginLandingPageAboveTheFold { get; set; }
    public PageContent? LoginLandingPageBelowTheFold { get; set; }
    public List<LinkModel>? RightSideMenuLinks { get; set; }
}