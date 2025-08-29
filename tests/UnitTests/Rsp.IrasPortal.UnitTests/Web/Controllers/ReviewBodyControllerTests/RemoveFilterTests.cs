using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

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

        var deserializedSearch =
            JsonSerializer.Deserialize<ReviewBodySearchModel>(routeValues["complexSearchQuery"]?.ToString());
        deserializedSearch!.Country.ShouldNotContain("England");
        deserializedSearch.Country.ShouldContain("Scotland");

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

        var deserializedSearch =
            JsonSerializer.Deserialize<ReviewBodySearchModel>(redirect.RouteValues["complexSearchQuery"]?.ToString());
        deserializedSearch!.Status.ShouldBeNull();

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

        var deserializedSearch =
            JsonSerializer.Deserialize<ReviewBodySearchModel>(redirect.RouteValues["complexSearchQuery"]?.ToString());
        deserializedSearch!.Status.ShouldBeNull(); // status is always cleared

        var sessionJson = _http.Session.GetString(SessionKeys.ReviewBodiesSearch);
        sessionJson.ShouldNotBeNullOrWhiteSpace();
    }
}