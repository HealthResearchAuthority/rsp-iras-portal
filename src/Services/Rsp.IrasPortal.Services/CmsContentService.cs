using Microsoft.Extensions.Caching.Memory;
using Refit;
using Rsp.IrasPortal.Application.Configuration;
using Rsp.IrasPortal.Application.DTOs.Responses.CmsContent;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services.Extensions;
using Rsp.IrasPortal.Web.Models.CmsContent;

namespace Rsp.IrasPortal.Services;

public class CmsContentService(
    ICmsContentServiceClient cmsClient,
    IMemoryCache cache,
    AppSettings appSettings) : ICmsContentService
{
    private readonly int ContentCacheDuration = appSettings.ContentCacheDurationMinutes.HasValue
        ? appSettings.ContentCacheDurationMinutes.Value : 1;

    private const string FooterCacheKey = "FooterContent";

    public async Task<ServiceResponse<MixedContentPageResponse>> GetMixedPageContentByUrl(string url)
    {
        // check if page content is in cache
        if (cache.TryGetValue(url, out ApiResponse<MixedContentPageResponse>? cachedPageContent) && cachedPageContent != null)
        {
            // return from cache
            return cachedPageContent.ToServiceResponse();
        }

        var response = await cmsClient.GetMixedPageContentByUrl(url);

        if (response.IsSuccessStatusCode && response.Content != null)
        {
            // Store in cache with an absolute expiration
            cache.Set(url, response, TimeSpan.FromMinutes(ContentCacheDuration));
        }

        return response.ToServiceResponse();
    }

    public async Task<ServiceResponse<GenericPageResponse>> GetPageContentByUrl(string url)
    {
        // check if page content is in cache
        if (cache.TryGetValue(url, out ApiResponse<GenericPageResponse>? cachedPageContent) && cachedPageContent != null)
        {
            // return from cache
            return cachedPageContent.ToServiceResponse();
        }

        var response = await cmsClient.GetPageContentByUrl(url);

        if (response.IsSuccessStatusCode && response.Content != null)
        {
            // Store in cache with an absolute expiration
            cache.Set(url, response, TimeSpan.FromMinutes(ContentCacheDuration));
        }

        return response.ToServiceResponse();
    }

    public async Task<ServiceResponse<SiteSettingsModel>> GetSiteSettings()
    {
        // cehck if footer is in cache
        if (cache.TryGetValue(FooterCacheKey, out ApiResponse<SiteSettingsModel>? cachedFooter) && cachedFooter != null)
        {
            // return from cache
            return cachedFooter.ToServiceResponse();
        }

        // footer is not in cache so get it from the CMS
        var siteSettings = await cmsClient.GetSiteSettings();

        // Store in cache with an absolute expiration
        cache.Set(FooterCacheKey, siteSettings, TimeSpan.FromMinutes(5));

        return siteSettings.ToServiceResponse();
    }
}