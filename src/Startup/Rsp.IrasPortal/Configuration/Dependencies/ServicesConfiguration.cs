using Microsoft.AspNetCore.Authentication;
using Rsp.IrasPortal.Application.Filters;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Infrastructure.Claims;
using Rsp.IrasPortal.Infrastructure.HttpMessageHandlers;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.Configuration.Dependencies;

/// <summary>
///  User Defined Services Configuration
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

        // add message handlers
        services.AddTransient<AuthHeadersHandler>();
        services.AddTransient<CmsPreviewHeadersHandler>();
        services.AddTransient<FunctionKeyHeadersHandler>();

        services.AddMemoryCache();

        services.AddControllersWithViews(options => options.Filters.Add<SiteContentFilter>());

        return services;
    }
}