using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;

namespace Rsp.IrasPortal.Web.Controllers;

[ExcludeFromCodeCoverage]
[Route("")]
public class AccessTokenController() : Controller
{
    [Route("token")]
    public async Task<IActionResult> Token(string tokenName)
    {
        if (User.Identity?.IsAuthenticated is true)
        {
            return new ContentResult
            {
                Content = JsonSerializer.Serialize
                (
                    new
                    {
                        token = await HttpContext.GetTokenAsync(tokenName),
                        updatedToken = HttpContext.Items[ContextItemKeys.BearerToken]
                    }
                )
            };
        }

        return new ContentResult
        {
            Content = "Not Signed In"
        };
    }

    [Route("header")]
    public IActionResult Header(string headerName)
    {
        if (User.Identity?.IsAuthenticated is true)
        {
            HttpContext.Request.Headers.TryGetValue(headerName, out var value);

            return new ContentResult
            {
                Content = value.Count > 0 ? value : ""
            };
        }

        return new ContentResult
        {
            Content = "Not Signed In"
        };
    }
}