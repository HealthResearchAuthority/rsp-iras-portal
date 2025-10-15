using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Middleware;

namespace Rsp.IrasPortal.UnitTests.Middleware;

public class CompleteProfileMiddlewareTests : TestServiceBase<CompleteProfileMiddleware>
{
    private readonly DefaultHttpContext _http;

    public CompleteProfileMiddlewareTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
    }

    [Theory, AutoData]
    public async Task Proceeds_When_CompleteProfile_Flag_Is_Present(string email, string identityProviderId, string telephone)
    {
        // arrange
        _http.Items.Add(ContextItemKeys.RequireProfileCompletion, true);
        _http.Items.Add(ContextItemKeys.Email, email);
        _http.Items.Add("identityProviderId", identityProviderId);
        _http.Items.Add("telephoneNumber", telephone);

        _http.Request.Path = "/test-page";
        _http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Email, email)
        }, "TestAuth"));

        await Sut.InvokeAsync(_http);

        var expectedRedirectUrl = "/profileandsettings/editprofile";

        _http.Response.Headers["Location"].ToString().ShouldBe(expectedRedirectUrl);
        _http.Items.ContainsKey(ContextItemKeys.RequireProfileCompletion).ShouldBeFalse();
    }

    [Theory, AutoData]
    public async Task Does_Not_Proceed_When_CompleteProfile_Flag_Is_Missing(string email, string identityProviderId, string telephone)
    {
        // arrange
        _http.Items.Add(ContextItemKeys.Email, email);
        _http.Items.Add("identityProviderId", identityProviderId);
        _http.Items.Add("telephoneNumber", telephone);

        _http.Request.Path = "/test-page";
        _http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Email, email)
        }, "TestAuth"));

        await Sut.InvokeAsync(_http);

        var redirectUrlParams = string.Join("&", $"telephone={Uri.EscapeDataString(telephone)}", $"email={email}", $"identityProviderId={identityProviderId}");
        var expectedRedirectUrl = "/profileandsettings/completeprofile?" + redirectUrlParams;

        _http.Response.Headers["Location"].ToString().ShouldNotBe(expectedRedirectUrl);
        _http.Items.ContainsKey(ContextItemKeys.RequireProfileCompletion).ShouldBeFalse();
    }

    [Theory, AutoData]
    public async Task Does_Not_Proceed_When_RequestUrl_Is_ProfileAndSettings(string email, string identityProviderId, string telephone)
    {
        // arrange
        _http.Items.Add(ContextItemKeys.RequireProfileCompletion, true);
        _http.Items.Add(ContextItemKeys.Email, email);
        _http.Items.Add("identityProviderId", identityProviderId);
        _http.Items.Add("telephoneNumber", telephone);

        _http.Request.Path = "/profileandsettings/completeprofile";
        _http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Email, email)
        }, "TestAuth"));

        await Sut.InvokeAsync(_http);

        var redirectUrlParams = string.Join("&", $"telephone={Uri.EscapeDataString(telephone)}", $"email={email}", $"identityProviderId={identityProviderId}");
        var expectedRedirectUrl = "/profileandsettings/completeprofile?" + redirectUrlParams;

        _http.Response.Headers["Location"].ToString().ShouldNotBe(expectedRedirectUrl);
        _http.Items.ContainsKey(ContextItemKeys.RequireProfileCompletion).ShouldBeFalse();
    }
}