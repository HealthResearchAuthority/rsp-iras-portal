using Rsp.Portal.Application.DTOs.Responses.CmsContent;
using Rsp.Portal.Application.Responses;

namespace Rsp.Portal.Application.Services;

public interface ICmsContentService
{
    public Task<ServiceResponse<SiteSettingsModel>> GetSiteSettings(bool preview = false);

    public Task<ServiceResponse<GenericPageResponse>> GetPageContentByUrl(string url, bool preview = false);

    public Task<ServiceResponse<MixedContentPageResponse>> GetMixedPageContentByUrl(string url, bool preview = false);

    public Task<ServiceResponse<GenericPageResponse>> GetHomeContent(bool preview = false);

    public Task<ServiceResponse<MixedContentPageResponse>> GetDashboardContent(bool preview = false);
}