using Refit;
using Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;
using Rsp.IrasPortal.Web.Models.CmsContent;

namespace Rsp.IrasPortal.Application.ServiceClients;

public interface ICmsContentServiceClient
{
    [Get("/umbraco/api/genericContentPage/getByUrl")]
    public Task<ApiResponse<GenericPageResponse>> GetPageContentByUrl(string url);

    [Get("/umbraco/api/siteSettings/getSiteSettings")]
    public Task<ApiResponse<SiteSettingsModel>> GetSiteSettings();

    [Get("/umbraco/api/mixedcontentpage/getByUrl")]
    public Task<ApiResponse<MixedContentPageResponse>> GetMixedPageContentByUrl(string url);

    [Get("/umbraco/api/genericContentPage/getHomeContent")]
    public Task<ApiResponse<GenericPageResponse>> GetHomeContent();
}