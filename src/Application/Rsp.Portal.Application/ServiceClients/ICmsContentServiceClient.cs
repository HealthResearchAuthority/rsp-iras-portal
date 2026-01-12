using Refit;
using Rsp.Portal.Application.DTOs.Responses.CmsContent;

namespace Rsp.Portal.Application.ServiceClients;

public interface ICmsContentServiceClient
{
    [Get("/genericContentPage/getByUrl")]
    public Task<ApiResponse<GenericPageResponse>> GetPageContentByUrl(string url, bool preview = false);

    [Get("/siteSettings/getSiteSettings")]
    public Task<ApiResponse<SiteSettingsModel>> GetSiteSettings(bool preview = false);

    [Get("/mixedcontentpage/getByUrl")]
    public Task<ApiResponse<MixedContentPageResponse>> GetMixedPageContentByUrl(string url, bool preview = false);

    [Get("/genericContentPage/getHomeContent")]
    public Task<ApiResponse<GenericPageResponse>> GetHomeContent(bool preview = false);

    [Get("/mixedcontentpage/getDashboardContent")]
    public Task<ApiResponse<MixedContentPageResponse>> GetDashboardContent(bool preview = false);
}