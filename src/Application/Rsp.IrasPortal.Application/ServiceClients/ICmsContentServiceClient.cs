using Refit;
using Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;
using Rsp.IrasPortal.Web.Models.CmsContent;

namespace Rsp.IrasPortal.Application.ServiceClients;

public interface ICmsContentServiceClient
{
    [Get("/umbraco/delivery/api/v2/content/item/{pageUrl}")]
    public Task<ApiResponse<GenericPageResponse>> GetPageContentByUrl(string pageUrl);

    [Get("/umbraco/api/siteSettings/getsitesettings")]
    public Task<ApiResponse<GenericPageResponse>> GetSiteSettings();

    [Get("/umbraco/api/mixedcontentpage/getcontentbyurl")]
    public Task<ApiResponse<MixedContentPageResponse>> GetMixedPageContentByUrl(string url);
}