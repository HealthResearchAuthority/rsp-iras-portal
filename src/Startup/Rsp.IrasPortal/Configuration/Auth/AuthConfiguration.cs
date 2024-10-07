using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Rsp.IrasPortal.Application.Configuration;
using Rsp.IrasPortal.Application.Constants;

namespace Rsp.IrasPortal.Configuration.Auth;

/// <summary>
/// Authentication and Authorization configuration
/// </summary>
[ExcludeFromCodeCoverage]
public static class AuthConfiguration
{
    private struct Roles
    {
        public const string admin = nameof(admin);
        public const string user = nameof(user);
        public const string reviewer = nameof(reviewer);
    };

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
                    options.Events = new CookieAuthenticationEvents
                    {
                        // this event is called on each request
                        // to validate that the current principal is still valid
                        OnValidatePrincipal = context =>
                        {
                            // save the original access_token in the memory, this will be needed
                            // to regenerate the JwtToken with additional claims
                            context.HttpContext.Items[ContextItemKeys.AcessToken] = context.Properties.GetTokenValue(ContextItemKeys.AcessToken);

                            return Task.CompletedTask;
                        }
                    };

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

        ConfigureAuthorization(services);

        return services;
    }

    private static void ConfigureAuthorization(IServiceCollection services)
    {
        var policy = new AuthorizationPolicyBuilder()
           .RequireAuthenticatedUser()
           .RequireClaim(ClaimTypes.Email)
           .Build();

        services
            .AddAuthorizationBuilder()
            .AddPolicy("IsReviewer", policy => policy.RequireRole(Roles.reviewer))
            .AddPolicy("IsAdmin", policy => policy.RequireRole(Roles.admin))
            .AddPolicy("IsUser", policy => policy.RequireRole(Roles.user))
            .SetDefaultPolicy(policy);
    }
}