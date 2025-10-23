using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Features.CookiePolicy.Components;

namespace Rsp.IrasPortal.UnitTests.Web.Features.CookiePolicy;

public class CookieBannerComponentTests : TestServiceBase<CookieBannerViewComponent>
{
    private readonly DefaultHttpContext _http;

    public CookieBannerComponentTests()
    {
        _http = new DefaultHttpContext();
        _http.Session = new InMemorySession();

        // Create minimal ActionContext
        var actionContext = new ActionContext(
            _http,
            new RouteData(),
            new ActionDescriptor()
        );

        // Create the ViewContext with TempData
        var viewContext = new ViewContext(
            actionContext,
            new FakeView(),
            new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            new TempDataDictionary(_http, Mock.Of<ITempDataProvider>()),
            TextWriter.Null,
            new HtmlHelperOptions()
        );

        // Assign ViewComponentContext properly
        Sut.ViewComponentContext = new ViewComponentContext
        {
            ViewContext = viewContext
        };
    }

    [Fact]
    public void Return_Nothing_When_Cookies_Not_Set()
    {
        // Arrange
        _http.Request.Headers["Cookie"] = $"{CookieConsentNames.EssentialCookies}=true;";

        // Act
        var result = Sut.Invoke();

        // Assert
        var componentResult = result.ShouldBeOfType<ContentViewComponentResult>();

        // Verify
        componentResult.Content.ShouldBe(string.Empty);
    }

    [Fact]
    public void Return_View_When_Cookies_Not_Set()
    {
        // Arrange

        // Act
        var result = Sut.Invoke();

        // Assert
        var componentResult = result.ShouldBeOfType<ViewViewComponentResult>();

        // Verify
        componentResult.ViewName.ShouldBe("~/Features/CookiePolicy/Views/CookieBanner.cshtml");
    }

    /// <summary>
    /// Minimal fake view so we can construct a ViewContext.
    /// It never renders anything — it just satisfies the type requirement.
    /// </summary>
    private class FakeView : IView
    {
        public string Path => string.Empty;

        public Task RenderAsync(ViewContext context)
        {
            // No-op — nothing is rendered in tests
            return Task.CompletedTask;
        }
    }
}