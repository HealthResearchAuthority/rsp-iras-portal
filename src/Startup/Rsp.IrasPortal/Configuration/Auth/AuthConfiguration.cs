using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
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
        public const string systemAdministrator = "system_administrator";
        public const string user = nameof(user);
        public const string reviewer = nameof(reviewer);
    };

    #region AddAuthenticationAndAuthorization
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
                    // Default scheme is cookie authentication
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                    // Default scheme and challenge scheme are same to handle the session and auth
                    // cookie timeout using the cookie authentication handler
                    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
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
                        },

                        OnRedirectToLogin = context =>
                        {
                            context.Response.Redirect("/auth/timedout");
                            return Task.CompletedTask;
                        }
                    };

                    options.LoginPath = "/";
                    options.LogoutPath = "/";
                    options.ExpireTimeSpan = TimeSpan.FromSeconds(appSettings.AuthSettings.AuthCookieTimeout);
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

                    options.Events.OnTokenValidated = context =>
                    {
                        // this key is used to indicate that the user is logged in for the first time
                        // will be used to update the LastLogin during the claims transformation
                        // to indicate when the user was logged in last time.
                        context.HttpContext.Session.SetString(SessionKeys.FirstLogin, bool.TrueString);

                        // this key is used to check if the session is alive in the middleware
                        // and signout the user if the session is expired
                        context.HttpContext.Session.SetString(SessionKeys.Alive, bool.TrueString);

                        return Task.CompletedTask;
                    };
                }
            );

        ConfigureAuthorization(services);

        return services;
    }

    #endregion AddAuthenticationAndAuthorization

    #region AddOneLoginAuthentication
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
                // Default scheme is cookie authentication
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                // Default scheme and challenge scheme are same to handle the session and auth
                // cookie timeout using the cookie authentication handler
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
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
                    },

                    OnRedirectToLogin = context =>
                    {
                        context.Response.Redirect("/auth/timedout");
                        return Task.CompletedTask;
                    }
                };

                options.LoginPath = "/";
                options.LogoutPath = "/";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(appSettings.OneLogin.AuthCookieTimeout);
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

                options.Events.OnTokenValidated = context =>
                {
                    // this key is used to indicate that the user is logged in for the first time
                    // will be used to update the LastLogin during the claims transformation
                    // to indicate when the user was logged in last time.
                    context.HttpContext.Session.SetString(SessionKeys.FirstLogin, bool.TrueString);

                    // this key is used to check if the session is alive in the middleware
                    // and signout the user if the session is expired
                    context.HttpContext.Session.SetString(SessionKeys.Alive, bool.TrueString);

                    return Task.CompletedTask;
                };
            });

        ConfigureAuthorization(services);

        return services;
    }

    #endregion

    #region AddOneLoginClientSecretAuthentication

    /// <summary>
    /// Adds One Login Authentication and Authorization to the service
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/></param>
    /// <param name="appSettings">Application Settinghs</param>
    public static IServiceCollection AddOneLoginClientSecretAuthentication(this IServiceCollection services, AppSettings appSettings)
    {
        // Add services to the container.
        _ = services
            .AddAuthentication(options =>
            {
                // Default scheme is cookie authentication
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                // Default scheme and challenge scheme are same to handle the session and auth
                // cookie timeout using the cookie authentication handler
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
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
                    },

                    OnRedirectToLogin = context =>
                    {
                        context.Response.Redirect("/auth/timedout");
                        return Task.CompletedTask;
                    }
                };

                options.LoginPath = "/";
                options.LogoutPath = "/";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(appSettings.OneLogin.AuthCookieTimeout);
                options.SlidingExpiration = true;
                options.AccessDeniedPath = "/Forbidden";
            })
            .AddOpenIdConnect(options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.Authority = appSettings.OneLogin.Authority;
                options.ClientId = appSettings.OneLogin.ClientId;
                options.ClientSecret = appSettings.OneLogin.ClientSecret;
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
                //options.NonceCookie.SameSite = SameSiteMode.Strict; // Set SameSite to None for cross-site requests
                options.NonceCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Ensure the cookie is secure

                options.Events.OnRedirectToIdentityProvider = context =>
                {
                    // Ensure the redirect URI is set correctly
                    context.ProtocolMessage.RedirectUri = context.Request.Scheme + "://" + context.Request.Host + context.Request.PathBase + options.CallbackPath;
                    return Task.CompletedTask;
                };

                options.Events.OnAuthenticationFailed = context =>
                {
                    Console.WriteLine($"OIDC error: {context.Exception.Message}");
                    return Task.CompletedTask;
                };

                options.Events.OnAuthorizationCodeReceived = async context =>
                {
                    context.TokenEndpointRequest!.ClientId = appSettings.OneLogin.ClientId;
                    context.TokenEndpointRequest.ClientSecret = appSettings.OneLogin.ClientSecret;
                    context.TokenEndpointRequest.GrantType = OpenIdConnectGrantTypes.AuthorizationCode;
                    context.TokenEndpointRequest.Code = context.ProtocolMessage.Code;
                    context.TokenEndpointRequest.RedirectUri = context.Request.Scheme + "://" + context.Request.Host + context.Request.PathBase + options.CallbackPath;

                    await Task.CompletedTask;
                };

                options.Events.OnTokenValidated = context =>
                {
                    // this key is used to indicate that the user is logged in for the first time
                    // will be used to update the LastLogin during the claims transformation
                    // to indicate when the user was logged in last time.
                    context.HttpContext.Session.SetString(SessionKeys.FirstLogin, bool.TrueString);

                    // this key is used to check if the session is alive in the middleware
                    // and signout the user if the session is expired
                    context.HttpContext.Session.SetString(SessionKeys.Alive, bool.TrueString);

                    return Task.CompletedTask;
                };
            });

        ConfigureAuthorization(services);

        return services;
    }

    #endregion AddOneLoginClientSecretAuthentication


    private static void ConfigureAuthorization(IServiceCollection services)
    {
        var policy = new AuthorizationPolicyBuilder()
           .RequireAuthenticatedUser()
           .RequireClaim(ClaimTypes.Email)
           .Build();

        services
            .AddAuthorizationBuilder()
            .AddPolicy("IsReviewer", policy => policy.RequireRole(Roles.reviewer))
            .AddPolicy("IsSystemAdministrator", policy => policy.RequireRole(Roles.systemAdministrator))
            .AddPolicy("IsUser", policy => policy.RequireRole(Roles.user))
            .SetDefaultPolicy(policy);
    }
}