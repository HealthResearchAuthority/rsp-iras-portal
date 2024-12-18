using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Infrastructure.Claims;
using Rsp.IrasPortal.Infrastructure.HttpMessageHandlers;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.Configuration.Dependencies;

/// <summary>
///  User Defined Services Configuration
/// </summary>
[ExcludeFromCodeCoverage]
public static class ServicesConfiguration
{
    /// <summary>
    /// Adds services to the IoC container
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/></param>
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        // add application services
        services.AddTransient<IApplicationsService, ApplicationsService>();
        services.AddTransient<IUserManagementService, UserManagementService>();
        services.AddTransient<IQuestionSetService, QuestionSetService>();
        services.AddTransient<IRespondentService, RespondentService>();
        services.AddTransient<IClaimsTransformation, CustomClaimsTransformation>();

        // add message handlers
        services.AddTransient<AuthHeadersHandler>();

        return services;
    }
}