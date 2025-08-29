using Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;
using Rsp.IrasPortal.Application.Responses;

namespace Rsp.IrasPortal.Application.Services;

public interface ICmsContentService
{
    public Task<ServiceResponse<GenericPageResponse>> GetSiteFooter();
}