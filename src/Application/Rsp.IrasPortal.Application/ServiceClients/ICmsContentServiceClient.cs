using Refit;
using Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;
using Rsp.IrasPortal.Web.Models.CmsContent;

namespace Rsp.IrasPortal.Application.ServiceClients;

public interface ICmsContentServiceClient
{
    [Get("/genericContentPage/getByUrl")]
    public Task<ApiResponse<GenericPageResponse>> GetPageContentByUrl(string url);

    [Get("/siteSettings/getSiteSettings")]
    public Task<ApiResponse<SiteSettingsModel>> GetSiteSettings();

    [Get("/mixedcontentpage/getByUrl")]
    public Task<ApiResponse<MixedContentPageResponse>> GetMixedPageContentByUrl(string url);

    [Get("/genericContentPage/getHomeContent")]
    public Task<ApiResponse<GenericPageResponse>> GetHomeContent();
}