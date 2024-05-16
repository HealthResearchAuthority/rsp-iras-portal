using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Rsp.IrasPortal.Web.Controllers;

[Route("")]
public class AccessTokenController : Controller
{
    [Route("token")]
    public async Task<IActionResult> Token(string tokenName)
    {
        if (User.Identity?.IsAuthenticated is true)
        {
            return new ContentResult
            {
                Content = await HttpContext.GetTokenAsync(tokenName)
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