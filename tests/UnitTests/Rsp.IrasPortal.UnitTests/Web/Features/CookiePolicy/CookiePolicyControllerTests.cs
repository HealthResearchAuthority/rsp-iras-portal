﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Web.Features.CookiePolicy.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Features.CookiePolicy;

public class CookiePolicyControllerTests : TestServiceBase<CookiesController>
{
    private readonly DefaultHttpContext _http;

    public CookiePolicyControllerTests()
    {
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        Sut.TempData = tempData;
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

    [Fact]
    public void CookieSet_When_Cookies_Accepted_Returns_View()
    {
        // arrange
        var userConsent = "yes";
        _http.Request.Headers["Referer"] = "some-url";

        // act
        var result = Sut.AcceptConsent(userConsent);

        // assert
        result.ShouldBeOfType<RedirectResult>();
    }

    [Fact]
    public void CookieSettings_Returns_View()
    {
        // Arrange

        // Act
        var result = Sut.CookieSettings();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("~/Features/CookiePolicy/Views/CookiesSettingsPage.cshtml");
    }
}