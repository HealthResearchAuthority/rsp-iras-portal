using Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;
using Rsp.IrasPortal.Application.Responses;

namespace Rsp.IrasPortal.Application.Services;

public interface ICmsContentService
{
    public Task<ServiceResponse<SiteSettingsModel>> GetSiteSettings(bool preview = false);

    public Task<ServiceResponse<GenericPageResponse>> GetPageContentByUrl(string url, bool preview = false);

    public Task<ServiceResponse<MixedContentPageResponse>> GetMixedPageContentByUrl(string url, bool preview = false);

    public Task<ServiceResponse<GenericPageResponse>> GetHomeContent(bool preview = false);

    public Task<ServiceResponse<MixedContentPageResponse>> GetDashboardContent(bool preview = false);
}