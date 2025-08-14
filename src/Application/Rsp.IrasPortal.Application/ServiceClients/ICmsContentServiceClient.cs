using Refit;
using Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;

namespace Rsp.IrasPortal.Application.ServiceClients;

public interface ICmsContentServiceClient
{
    [Get("/umbraco/delivery/api/v2/content/item/{pageUrl}")]
    public Task<ApiResponse<GenericPageResponse>> GetPageContentByUrl(string pageUrl);

    [Get("/umbraco/api/siteSettings/getsitefooter")]
    public Task<ApiResponse<SiteSettingsModel>> GetSiteSettings();
}