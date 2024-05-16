using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Rsp.IrasPortal.Application.Configuration;

namespace Rsp.IrasPortal.Configuration.Auth;

/// <summary>
/// Authentication and Authorization configuration
/// </summary>
[ExcludeFromCodeCoverage]
public static class AuthConfiguration
{
    /// <summary>
    /// Adds the Authentication and Authorization to the service
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/></param>
    /// <param name="appSettings">Application Settinghs</param>
    public static IServiceCollection AddAuthenticationAndAuthorization(this IServiceCollection services, AppSettings appSettings)
    {
        services
            .AddAuthentication
            (
                options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                }
            )
            .AddCookie
            (
                options =>
                {
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
                    options.SlidingExpiration = true;
                    options.AccessDeniedPath = "/Forbidden";
                }
            )
            .AddOpenIdConnect
            (
                options =>
                {
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.Authority = appSettings.AuthSettings.Authority;
                    options.ClientId = appSettings.AuthSettings.ClientId;
                    options.ClientSecret = appSettings.AuthSettings.ClientSecret;
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.SaveTokens = true;
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                    options.CallbackPath = "/signin-oidc"; // Default callback path
                    options.SignedOutCallbackPath = "/oidc/logout";
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.UsePkce = false;
                    options.ClaimActions.MapUniqueJsonKey(ClaimTypes.Name, "given_name");
                }
            );

        ConfigureAuthorization(services, []);

        return services;
    }

    private static void ConfigureAuthorization(IServiceCollection services, List<string> roles)
    {
        services
            .AddAuthorizationBuilder()
            .AddPolicy("IsAdmin", policy => policy.RequireRole("admin"))
            .AddPolicy("IsUser", policy => policy.RequireRole("user"));
    }
}