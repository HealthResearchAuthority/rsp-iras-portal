using Microsoft.AspNetCore.Authentication;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services;
using Rsp.Portal.Application.Filters;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Infrastructure.Claims;
using Rsp.Portal.Infrastructure.HttpMessageHandlers;
using Rsp.Portal.Services;
using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Services;

namespace Rsp.Portal.Configuration.Dependencies;

/// <summary>
/// User Defined Services Configuration
/// </summary>
public static class ServicesConfiguration
{
    /// <summary>
    /// Adds services to the IoC container
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/></param>
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        // add application services
        services.AddScoped<IApplicationsService, ApplicationsService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IRespondentService, RespondentService>();
        services.AddScoped<IClaimsTransformation, CustomClaimsTransformation>();
        services.AddScoped<IReviewBodyService, ReviewBodyService>();
        services.AddScoped<IRtsService, RtsService>();
        services.AddScoped<IProjectModificationsService, ProjectModificationsService>();
        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<ICmsQuestionsetService, CmsQuestionsetService>();
        services.AddScoped<ICmsContentService, CmsContentService>();
        services.AddScoped<ISponsorOrganisationService, SponsorOrganisationService>();
        services.AddScoped<IProjectRecordValidationService, ProjectRecordValidationService>();
        services.AddScoped<IModificationRankingService, ModificationRankingService>();
        services.AddScoped<IProjectClosuresService, ProjectClosuresService>();
        services.AddScoped<ISponsorUserAuthorisationService, SponsorUserAuthorisationService>();
        services.AddScoped<IViewRenderService, ViewRenderService>();

        // add message handlers
        services.AddTransient<AuthHeadersHandler>();
        services.AddTransient<CmsPreviewHeadersHandler>();
        services.AddTransient<FunctionKeyHeadersHandler>();

        services.AddMemoryCache();

        services.AddControllersWithViews(options => options.Filters.Add<SiteContentFilter>());

        return services;
    }
}