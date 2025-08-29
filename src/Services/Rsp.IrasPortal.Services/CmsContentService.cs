using Microsoft.Extensions.Caching.Memory;
using Refit;
using Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services.Extensions;

namespace Rsp.IrasPortal.Services;

public class CmsContentService(ICmsContentServiceClient cmsClient, IMemoryCache cache) : ICmsContentService
{
    private const string CacheKey = "FooterContent";

    public async Task<ServiceResponse<GenericPageResponse>> GetSiteFooter()
    {
        // cehck if footer is in cache
        if (cache.TryGetValue(CacheKey, out ApiResponse<GenericPageResponse>? cachedFooter) && cachedFooter != null)
        {
            // return from cache
            return cachedFooter.ToServiceResponse(); 
        }

        // footer is not in cache so get it from the CMS
        var siteSettings = await cmsClient.GetSiteSettings();

        // Store in cache with an absolute expiration
        cache.Set(CacheKey, siteSettings, TimeSpan.FromMinutes(5));

        return siteSettings.ToServiceResponse();
    }
}