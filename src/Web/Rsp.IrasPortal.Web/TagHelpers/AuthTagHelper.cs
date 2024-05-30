using System.Security.Claims;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Rsp.IrasPortal.Web.TagHelpers.Models;

namespace Rsp.IrasPortal.Web.TagHelpers;

/// <summary>
/// Shows or hides contents based on authenticated user, roles and roles processing logic
/// </summary>
[HtmlTargetElement("*", Attributes = "auth-params")]
[HtmlTargetElement("authorized", Attributes = "auth-params")]
public class AuthTagHelper(IHttpContextAccessor httpContextAccessor) : TagHelper
{
    public AuthTagHelperParams AuthParams { get; set; } = null!;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        await base.ProcessAsync(context, output);

        var httpContext = httpContextAccessor.HttpContext;

        // httpContext is null don't do anything
        if (httpContext == null)
        {
            return;
        }

        // check if user is authenticated or not
        var isUserAuthenticated = httpContext.User.Identity?.IsAuthenticated is true;

        // check if roles are provided
        var rolesProvided = !string.IsNullOrWhiteSpace(AuthParams.Roles);

        var claims = Enumerable.Empty<Claim>();

        // get the role claims if user is authenticated and roles are provided
        if (isUserAuthenticated && rolesProvided)
        {
            claims = httpContext.User.FindAll(claim => claim.Type == ClaimTypes.Role);
        }

        // | ShowWhenAuthenticated | UserIsAuthenticated  | RolesProvided | UserInRoles | Show Output |
        // |-----------------------|----------------------|---------------|-------------|-------------|
        // | true                  | true                 | true          | true        | yes         |
        // | true                  | true                 | true          | false       | no          |
        // | true                  | true                 | false         | N/A         | yes         |
        // | true                  | false                | N/A           | N/A         | no          |
        // | false                 | false                | N/A           | N/A         | yes         |
        // | false                 | true                 | N/A           | N/A         | no          |

        // use switch expression and pattern matching to build the above decision table
        var showOutput = (AuthParams.ShowWhenAuthenticated, isUserAuthenticated, rolesProvided, IsUserInRoles(claims.ToList())) switch
        {
            (true, true, true, true) => true,
            (true, true, true, false) => false,
            (true, true, false, _) => true,
            (true, false, _, _) => false,
            (false, false, _, _) => true,
            (false, true, _, _) => false
        };

        if (showOutput)
        {
            return;
        }

        output.TagName = null;
        output.SuppressOutput();
    }

    private bool IsUserInRoles(IList<Claim> claims)
    {
        if (claims.Count == 0)
        {
            return false;
        }

        var rolesArray = AuthParams.Roles!.Split(',', StringSplitOptions.RemoveEmptyEntries);

        // get the intersection of the role values
        var intersection = claims.IntersectBy(rolesArray, claim => claim.Value);

        return AuthParams.RolesLogic switch
        {
            // suppress the output if the user is not in any roles
            RolesProcessing.Or => intersection.Any(),

            // suppress the output if the user is not in all roles
            RolesProcessing.And => intersection.Count() == rolesArray.Length,

            _ => false
        };
    }
}