using Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Web.Models.CmsContent;

namespace Rsp.IrasPortal.Application.Services;

public interface ICmsContentService
{
    public Task<ServiceResponse<SiteSettingsModel>> GetSiteSettings();

    public Task<ServiceResponse<GenericPageResponse>> GetPageContentByUrl(string url);

    public Task<ServiceResponse<MixedContentPageResponse>> GetMixedPageContentByUrl(string url);

    public Task<ServiceResponse<GenericPageResponse>> GetHomeContent();
}