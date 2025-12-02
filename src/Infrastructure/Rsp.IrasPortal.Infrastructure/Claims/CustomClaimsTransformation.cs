using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Tokens;
using NetDevPack.Security.Jwt.Core.Interfaces;
using Rsp.IrasPortal.Application.AccessControl;
using Rsp.IrasPortal.Application.Configuration;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Extensions;
using Rsp.IrasPortal.Application.Services;

namespace Rsp.IrasPortal.Infrastructure.Claims;

public class CustomClaimsTransformation
(
    IHttpContextAccessor httpContextAccessor,
    IJwtService jwtService,
    IUserManagementService userManagementService,
    IOptionsSnapshot<AppSettings> appSettings,
    IFeatureManager featureManager
) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // To be able to assign additional roles based on the email we need to
        // find email claim
        var emailClaim = principal.FindFirst(ClaimTypes.Email);
        var mobilePhone = principal.FindFirst(ClaimTypes.MobilePhone)?.Value;
        var identityProviderId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // if there is no email claim, return the current principal
        if (emailClaim is null)
        {
            return principal;
        }

        // get the email claim value
        var email = emailClaim.Value;

        // Clone the principal
        var claimsIdentity = (ClaimsIdentity)principal.Identity!;

        const string roleClaim = ClaimTypes.Role;

        // all users will get iras_portal_user and by default
        // this is to allow application to call the usermanagement
        // microservice to get the roles from the database
        claimsIdentity.AddClaim(new Claim(roleClaim, "iras_portal_user"));

        // at this point we need to generate a new token
        await UpdateAccessToken(principal);

        // now we can call the usermanagement api
        var context = httpContextAccessor.HttpContext!;

        var userResponse = await userManagementService.GetUser(null, null, identityProviderId);
        if (context.Session.GetString(SessionKeys.FirstLogin) == bool.TrueString)
        {
            if (userResponse.IsSuccessStatusCode && userResponse.Content != null)
            {
                // user with provider ID exists
                // let's update their email and telephone number from the claims
                var currentUser = userResponse.Content;
                await userManagementService.UpdateUserEmailAndPhoneNumber(currentUser.User, email, mobilePhone);
            }
            else
            {
                // user cannot be found by provider ID so let's try find them by their email
                var getUserResponseEmail = await userManagementService.GetUser(null, email);

                if (getUserResponseEmail.IsSuccessStatusCode && getUserResponseEmail.Content != null)
                {
                    // user found so let's update their providerID

                    userResponse = getUserResponseEmail;

                    if (!string.IsNullOrEmpty(identityProviderId))
                    {
                        await userManagementService.UpdateUserIdentityProviderId(getUserResponseEmail.Content.User, identityProviderId);
                    }
                }
                else
                {
                    context.Items.Add(ContextItemKeys.RequireProfileCompletion, true);
                    context.Items.Add(ContextItemKeys.Email, email);
                    context.Items.Add("telephoneNumber", mobilePhone);
                    context.Items.Add("identityProviderId", identityProviderId);
                    context.Session.Remove(SessionKeys.FirstLogin);

                    await UpdateAccessToken(principal);
                    return principal;
                }
            }

            await userManagementService.UpdateLastLogin(email);
            context.Session.Remove(SessionKeys.FirstLogin);
        }

        if (userResponse.IsSuccessStatusCode && userResponse.Content != null)
        {
            var user = userResponse.Content;

            var userId = user.User.Id;
            var firstName = principal.FindFirst(ClaimTypes.GivenName)?.Value;
            var surName = principal.FindFirst(ClaimTypes.Surname)?.Value;
            var lastLogin = userResponse.Content.User?.LastLogin;

            context.Items.Add(ContextItemKeys.UserId, userId);
            context.Items.Add(ContextItemKeys.Email, email);
            context.Items.Add(ContextItemKeys.FirstName, firstName);
            context.Items.Add(ContextItemKeys.LastName, surName);
            context.Items.Add(ContextItemKeys.LastLogin, lastLogin);

            if (!string.IsNullOrWhiteSpace(user.User?.Id))
            {
                claimsIdentity.AddClaim(new Claim("userId", user.User.Id));
            }

            // add the roles to claimsIdentity
            foreach (var role in user.Roles)
            {
                claimsIdentity.AddClaim(new Claim(roleClaim, role));
            }

            // get permissions for the role
            var rolePermissions = principal.GetUserPermissions();

            // add permissions as claims
            var permissionsClaims = rolePermissions
                .Select(permission => new Claim("permissions", permission))
                .ToList();

            claimsIdentity.AddClaims(permissionsClaims);

            // get allowed statuses for the user
            var allowedStatuses = RoleStatusPermissions.GetAllowedStatusesForRoles(user.Roles);

            foreach (var (entityType, statuses) in allowedStatuses)
            {
                claimsIdentity.AddClaims(statuses.Select(status => new Claim($"allowed_statuses/{entityType}", status)));
            }

            // for one login
            var oneLoginEnabled = await featureManager.IsEnabledAsync(FeatureFlags.OneLogin);

            if (oneLoginEnabled && !string.IsNullOrEmpty(user.User?.GivenName))
            {
                claimsIdentity.AddClaim(new Claim(ClaimTypes.GivenName, user.User.GivenName));
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Name, user.User.GivenName));
            }

            // as the claims are now updated, we need to generate a new token
            await UpdateAccessToken(principal);
        }

        return principal;
    }

    /// <summary>
    /// Generates a new jwt token using NetDevPack.Security.Jwt.Core package
    /// which emits jwks endpoint to generate keys.
    /// </summary>
    /// <param name="principal"><see cref="ClaimsPrincipal"/></param>
    public async Task UpdateAccessToken(ClaimsPrincipal principal)
    {
        // using jwtService injected by the package to get the
        // current signing credentials.
        var signingCredentials = await jwtService.GetCurrentSigningCredentials();

        var context = httpContextAccessor.HttpContext;

        // if we don't have the HttpContext or
        // getting the accessToken fails or
        // the accessToken value is null or empty then return
        if (context == null)
        {
            return;
        }

        context.Items.TryGetValue(ContextItemKeys.BearerToken, out var bearerToken);

        if (string.IsNullOrWhiteSpace(bearerToken as string))
        {
            return;
        }

        var handler = new JwtSecurityTokenHandler();

        // get the original access token
        var jsonToken = handler.ReadJwtToken(bearerToken as string);

        var oneLoginEnabled = await featureManager.IsEnabledAsync(FeatureFlags.OneLogin);

        var audience = oneLoginEnabled ?
            appSettings.Value.OneLogin.ClientId :
            appSettings.Value.AuthSettings.ClientId;

        // configure the new token using the existing
        // bearer_token properties but with newly added
        // claims.

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = jsonToken.Issuer, // Add this line
            Audience = audience,
            IssuedAt = jsonToken.IssuedAt,
            NotBefore = jsonToken.ValidFrom,
            Subject = (ClaimsIdentity)principal.Identity!,
            SigningCredentials = signingCredentials
        };

        var newTokenExpirationExists = context.Items.TryGetValue(ContextItemKeys.AccessTokenCookieExpiryDate, out var newTokenExpiryObject);

        if (newTokenExpirationExists && newTokenExpiryObject is DateTimeOffset newTokenExpiry)
        {
            // extend new token expiration
            tokenDescriptor.Expires = newTokenExpiry.UtcDateTime;
        }

        // generate the security token
        var token = handler.CreateJwtSecurityToken(tokenDescriptor);

        // write the JWT token to the context.Items to be utilised by
        // message handler when sending outgoing requests.
        context.Items[ContextItemKeys.BearerToken] = handler.WriteToken(token);
    }
}