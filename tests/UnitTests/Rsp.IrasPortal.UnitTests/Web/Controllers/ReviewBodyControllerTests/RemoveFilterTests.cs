using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class RemoveFilterTests : TestServiceBase<ReviewBodyController>
{
    private readonly DefaultHttpContext _http;

    public RemoveFilterTests()
    {
        _http = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = _http
        };
    }

    [Fact]
    public void RemoveFilter_ShouldRemoveCountryValue_AndRedirect()
    {
        // Arrange
        var model = new ReviewBodySearchModel
        {
            Country = new List<string> { "England", "Scotland" }
        };
        var json = JsonSerializer.Serialize(model);

        // Act
        var result = Sut.RemoveFilter("Country", "England", json);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("rbc:viewreviewbodies");

        var routeValues = redirect.RouteValues!;
        routeValues["pageNumber"].ShouldBe(1);
        routeValues["pageSize"].ShouldBe(20);
        routeValues["fromPagination"].ShouldBe(true);

        // (optional) also verify session got updated
        var sessionJson = _http.Session.GetString(SessionKeys.ReviewBodiesSearch);
        sessionJson.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void RemoveFilter_ShouldClearStatus_AndRedirect()
    {
        // Arrange
        var model = new ReviewBodySearchModel { Status = true };
        var json = JsonSerializer.Serialize(model);

        // Act
        var result = Sut.RemoveFilter("status", null, json);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("rbc:viewreviewbodies");

        var sessionJson = _http.Session.GetString(SessionKeys.ReviewBodiesSearch);
        sessionJson.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void RemoveFilter_ShouldUseDefaultSearch_WhenModelIsNull()
    {
        // Act
        var result = Sut.RemoveFilter("status", null);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("rbc:viewreviewbodies");

        var sessionJson = _http.Session.GetString(SessionKeys.ReviewBodiesSearch);
        sessionJson.ShouldNotBeNullOrWhiteSpace();
    }
}