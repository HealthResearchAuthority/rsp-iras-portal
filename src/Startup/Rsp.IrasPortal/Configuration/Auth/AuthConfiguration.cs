﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Rsp.IrasPortal.Application.Configuration;
using Rsp.IrasPortal.Application.Constants;

namespace Rsp.IrasPortal.Configuration.Auth;

/// <summary>
/// Authentication and Authorization configuration
/// </summary>
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
                            context.HttpContext.Items[ContextItemKeys.BearerToken] = context.Properties.GetTokenValue(ContextItemKeys.AcessToken);

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

                    options.Events.OnAuthorizationCodeReceived = context =>
                    {
                        //context.TokenEndpointResponse.AccessToken =
                        return Task.CompletedTask;
                    };
                }
            );

        ConfigureAuthorization(services);

        return services;
    }

    /// <summary>
    /// Adds One Login Authentication and Authorization to the service
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/></param>
    /// <param name="appSettings">Application Settinghs</param>
    public static IServiceCollection AddOneLoginAuthentication(this IServiceCollection services, AppSettings appSettings)
    {
        // Add services to the container.
        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Events = new CookieAuthenticationEvents
                {
                    // this event is called on each request
                    // to validate that the current principal is still valid
                    OnValidatePrincipal = context =>
                    {
                        // save the original access_token in the memory, this will be needed
                        // to regenerate the JwtToken with additional claims
                        context.HttpContext.Items[ContextItemKeys.BearerToken] = context.Properties.GetTokenValue(ContextItemKeys.IdToken);

                        return Task.CompletedTask;
                    }
                };

                options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
                options.SlidingExpiration = true;
                options.AccessDeniedPath = "/Forbidden";
            })
            .AddOpenIdConnect(options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.Authority = appSettings.OneLogin.Authority;
                options.ClientId = appSettings.OneLogin.ClientId;
                options.MetadataAddress = $"{appSettings.OneLogin.Authority}/.well-known/openid-configuration";
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.ResponseMode = OpenIdConnectResponseMode.Query;
                options.SaveTokens = true;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("email");
                options.Scope.Add("phone");
                options.CallbackPath = "/onelogin-callback";
                options.SignedOutCallbackPath = "/onelogin-logout-callback";
                options.GetClaimsFromUserInfoEndpoint = true;
                options.UsePkce = false;
                options.ClaimActions.MapUniqueJsonKey(ClaimTypes.Email, "email");

                // GOV.UK One Login used a client assertion to secure the token exchange instead of a client secret.
                // This is a JWT signed with the client's private key.
                // The public key is registered with GOV.UK One Login.
                options.Events.OnAuthorizationCodeReceived = context =>
                {
                    // Load the private key from a secure location.
                    // This example loads the private key from a file, but you could use a secret store.
                    var rsa = RSA.Create();
                    rsa.ImportFromPem(appSettings.OneLogin.PrivateKeyPem);
                    var clientPrivateKey = new RsaSecurityKey(rsa);
                    var signingCredentials = new SigningCredentials(clientPrivateKey, SecurityAlgorithms.RsaSha256);

                    // Create a JWT token with the client ID as the issuer and the token endpoint as the audience.
                    var tokenHandler = new JwtSecurityTokenHandler();

                    var jwt = new JwtSecurityToken
                    (
                        issuer: appSettings.OneLogin.ClientId,
                        audience: $"{appSettings.OneLogin.Authority}/token",
                        claims:
                        [
                            new Claim("jti", RandomNumberGenerator.GetHexString(6)),
                            new Claim("sub", appSettings.OneLogin.ClientId),
                            new Claim(ClaimTypes.Role, "iras_portal_user"),
                        ],
                        expires: DateTime.UtcNow.AddMinutes(5),
                        signingCredentials: signingCredentials
                    );

                    // Set the client assertion on the token request.
                    var clientAssertion = tokenHandler.WriteToken(jwt);
                    Console.WriteLine(clientAssertion);
                    context.TokenEndpointRequest!.ClientAssertion = clientAssertion;
                    context.TokenEndpointRequest.ClientAssertionType = "urn:ietf:params:oauth:client-assertion-type:jwt-bearer";
                    return Task.CompletedTask;
                };
            });

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