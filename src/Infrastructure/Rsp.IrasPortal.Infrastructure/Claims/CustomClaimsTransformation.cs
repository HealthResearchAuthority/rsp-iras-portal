using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using NetDevPack.Security.Jwt.Core.Interfaces;
using Rsp.IrasPortal.Application.Configuration;
using Rsp.IrasPortal.Application.Constants;

namespace Rsp.IrasPortal.Infrastructure.Claims;

public class CustomClaimsTransformation(IHttpContextAccessor httpContextAccessor, IJwtService jwtService, AppSettings appSettings) : IClaimsTransformation
{
    private struct Roles
    {
        public const string admin = nameof(admin);
        public const string user = nameof(user);
        public const string reviewer = nameof(reviewer);
    }

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

        var roleClaims = claimsIdentity.Claims.Where(c => c.Type == roleClaim);

        var claimValue = ValidateEmail(email);

        // if the email is nikhil.bharathesh then add user claim
        // if the email is shahzad.hassan or haris.amin then add admin claim
        if (claimValue is Roles.user or Roles.admin &&
            roleClaims.FirstOrDefault(claim => claim.Value == claimValue) == null)
        {
            claimsIdentity.AddClaim(new Claim(roleClaim, claimValue));
        }

        // if the email is shahzad.hassan then add admin claim
        if (claimValue == Roles.admin &&
            roleClaims.FirstOrDefault(claim => claim.Value == Roles.reviewer) == null)
        {
            claimsIdentity.AddClaim(new Claim(roleClaim, Roles.reviewer));
        }

        await UpdateAccessToken(principal);

        return principal;
    }

    /// <summary>
    /// This is a temporary code to add custom claims based on the email address
    /// </summary>
    /// <param name="email">email to validate</param>
    public static string ValidateEmail(string email)
    {
        return email switch
        {
            "shahzad.hassan@paconsulting.com" => Roles.admin,
            "nikhil.bharathesh_PA_test@hra.nhs.uk" => Roles.user,
            "haris.amin@paconsulting.com" => Roles.admin,
            _ => string.Empty
        };
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
            !context.Items.TryGetValue(TokenKeys.AcessToken, out var accessToken) ||
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
        context.Items[TokenKeys.AcessToken] = handler.WriteToken(token);
    }
}