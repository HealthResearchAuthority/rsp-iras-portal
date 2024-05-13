using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Rsp.IrasPortal.Infrastructure;

public class CustomClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var claimType = ClaimTypes.Email;

        // if there is no email claim, return the current principal
        if (!principal.HasClaim(claim => claim.Type == claimType))
        {
            return Task.FromResult(principal);
        }

        var claim = principal.Claims.First(c => c.Type == claimType);

        var claimsIdentity = new ClaimsIdentity();

        // if the email is shahzad.hassan then add user claim
        if (ValidateEmail(claim.Value) is "user")
        {
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "user"));
        }

        // if the email is nikhil.bharathesh then add admin claim
        if (ValidateEmail(claim.Value) is "admin")
        {
            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "admin"));
        }

        // if the role claim has been added, then add to the principal
        if (claimsIdentity.HasClaim(claim => claim.Type == ClaimTypes.Role))
        {
            principal.AddIdentity(claimsIdentity);
        }

        return Task.FromResult(principal);
    }

    /// <summary>
    /// This is a temporary code to add custom claims based on the email address
    /// </summary>
    /// <param name="email"></param>
    public static string ValidateEmail(string email)
    {
        return email switch
        {
            "shahzad.hassan@paconsulting.com" => "admin",
            "nikhil.bharathesh_PA_test@hra.nhs.uk" => "user",
            "haris.amin@paconsulting.com" => "admin",
            _ => string.Empty
        };
    }
}