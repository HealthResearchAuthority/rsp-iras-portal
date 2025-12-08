using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Rsp.IrasPortal.Application.Configuration;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Infrastructure.Authorization;

namespace Rsp.IrasPortal.Configuration.Auth;

/// <summary>
/// Authentication and Authorization configuration
/// </summary>
public static class AuthConfiguration
{
    /// <summary>
    /// Adds the Authentication and Authorization to the service
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/></param>
    /// <param name="appSettings">Application Settinghs</param>
    public static IServiceCollection AddAuthenticationAndAuthorization(
        this IServiceCollection services,
        AppSettings appSettings)
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
                    options.Events = GenerateCookieAuthenticationEvent(appSettings.AuthSettings.AuthCookieTimeout, ContextItemKeys.AcessToken);

                    options.Cookie.IsEssential = true;
                    options.LoginPath = "/";
                    options.LogoutPath = "/";
                    options.ExpireTimeSpan = TimeSpan.FromSeconds(appSettings.AuthSettings.AuthCookieTimeout);
                    options.SlidingExpiration = true;
                    options.AccessDeniedPath = "/error/forbidden";
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
                    options.Scope.Add("offline_access");
                    options.CallbackPath = "/signin-oidc"; // Default callback path
                    options.SignedOutCallbackPath = "/oidc/logout";
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.UsePkce = false;
                    options.ClaimActions.MapUniqueJsonKey(ClaimTypes.Name, "given_name");
                    options.UseTokenLifetime = false;

                    options.Events.OnTokenValidated = context =>
                    {
                        if (context.Properties != null)
                        {
                            context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(appSettings.AuthSettings.AuthCookieTimeout);
                        }
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
                options.Events = GenerateCookieAuthenticationEvent(appSettings.OneLogin.AuthCookieTimeout, ContextItemKeys.IdToken);

                options.LoginPath = "/";
                options.LogoutPath = "/";
                options.ExpireTimeSpan = TimeSpan.FromSeconds(appSettings.OneLogin.AuthCookieTimeout);
                options.SlidingExpiration = true;
                options.AccessDeniedPath = "/error/forbidden";
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
                options.ClaimActions.MapUniqueJsonKey(ClaimTypes.MobilePhone, "phone_number");

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
                        expires: DateTime.UtcNow.AddSeconds(appSettings.OneLogin.AuthCookieTimeout),
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
                    if (context.Properties != null)
                    {
                        context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(appSettings.OneLogin.AuthCookieTimeout);
                    }

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

    private static void ConfigureAuthorization(IServiceCollection services)
    {
        // the following default policy is used to ensure that
        // the user is authenticated and has email and user_status claims with active value.
        //
        // This policy is applied on the pages where simple [Authorize] attribute is used. The user_status
        // claim is added in the CustomClaimsTransformation class after fetching the user details from the
        // UserManagement service.

        // if the returned user's status is disabled, user_status claim will have disabled value
        // this will prevent disabled users from accessing pages with simple [Authorize] attribute

        // The other pages have more granular policies defined using the [Authorize(Policy = Workdspace.*)] attribute
        // as the user won't have additonal claims when the user is disabled, so they won't be able to access those pages as well
        var policy = new AuthorizationPolicyBuilder()
           .RequireAuthenticatedUser()
           .RequireClaim(ClaimTypes.Email)
           .Build();

        services
            .AddAuthorizationBuilder()
            .SetDefaultPolicy(policy);

        // Replace the default authorization policy handler with a custom one.
        services.AddSingleton<IAuthorizationPolicyProvider, WorkspacePermissionsPolicyProvider>();

        // Register authorization handlers
        services.AddSingleton<IAuthorizationHandler, WorkspaceRequirementHandler>();
        services.AddSingleton<IAuthorizationHandler, PermissionRequirementHandler>();
    }

    private static CookieAuthenticationEvents GenerateCookieAuthenticationEvent(uint cookieAuthenticationTimeout, string accessTokenName)
    {
        return new CookieAuthenticationEvents
        {
            // this event is called on each request
            // to validate that the current principal is still valid
            OnValidatePrincipal = context =>
            {
                context.ShouldRenew = true;
                context.Properties.AllowRefresh = true;
                context.Options.ExpireTimeSpan = TimeSpan.FromSeconds(cookieAuthenticationTimeout);
                context.Options.SlidingExpiration = true;

                // save the original access_token in the memory, this will be needed
                // to regenerate the JwtToken with additional claims
                var accessToken = context.Properties.GetTokenValue(accessTokenName);

                // auth cookie already contains updated expiry datetime
                // so let's use that for the token
                var cookieExpiry = context.Properties.ExpiresUtc;
                if (cookieExpiry.HasValue)
                {
                    // save the updated expiry date so we can
                    // use it in the CustomClaimsTransformation.cs
                    // when creating new token
                    context.HttpContext.Items[ContextItemKeys.AccessTokenCookieExpiryDate] = cookieExpiry;
                }

                // save original access token
                context.HttpContext.Items[ContextItemKeys.BearerToken] = accessToken;

                return Task.CompletedTask;
            }
        };
    }
}