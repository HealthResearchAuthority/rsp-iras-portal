using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using NetDevPack.Security.Jwt.Core.Interfaces;
using Rsp.IrasPortal.Application.Configuration;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Services;

namespace Rsp.IrasPortal.Infrastructure.Claims;

public class CustomClaimsTransformation
(
    IHttpContextAccessor httpContextAccessor,
    IJwtService jwtService,
    IUserManagementService userManagementService,
    AppSettings appSettings
) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // To be able to assign additional roles based on the email we need to
        // find email claim
        var emailClaim = principal.FindFirst(ClaimTypes.Email);

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
        var getUserResponse = await userManagementService.GetUser(null, email);

        if (getUserResponse.IsSuccessStatusCode && getUserResponse.Content != null)
        {
            var context = httpContextAccessor.HttpContext!;

            var respondentId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var firstName = principal.FindFirst(ClaimTypes.GivenName)?.Value;
            var surName = principal.FindFirst(ClaimTypes.Surname)?.Value;

            context.Items.Add(ContextItemKeys.RespondentId, respondentId);
            context.Items.Add(ContextItemKeys.Email, email);
            context.Items.Add(ContextItemKeys.FirstName, firstName);
            context.Items.Add(ContextItemKeys.LastName, surName);

            var user = getUserResponse.Content;

            // add the roles to claimsIdentity
            foreach (var role in user.Roles)
            {
                claimsIdentity.AddClaim(new Claim(roleClaim, role));
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
        if (context == null ||
            !context.Items.TryGetValue(ContextItemKeys.AcessToken, out var accessToken) ||
            string.IsNullOrWhiteSpace(accessToken as string))
        {
            return;
        }

        var handler = new JwtSecurityTokenHandler();

        // get the original access token
        var jsonToken = handler.ReadJwtToken(accessToken as string);

        // configure the new token using the existing
        // access_token properties but with newly added
        // claims.
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = jsonToken.Issuer, // Add this line
            Audience = appSettings.AuthSettings.ClientId,
            IssuedAt = jsonToken.IssuedAt,
            NotBefore = jsonToken.ValidFrom,
            Subject = (ClaimsIdentity)principal.Identity!,
            Expires = jsonToken.ValidTo,
            SigningCredentials = signingCredentials
        };

        // generate the security token
        var token = handler.CreateJwtSecurityToken(tokenDescriptor);

        // write the JWT token to the context.Items to be utilised by
        // message handler when sending outgoing requests.
        context.Items[ContextItemKeys.AcessToken] = handler.WriteToken(token);
    }
}