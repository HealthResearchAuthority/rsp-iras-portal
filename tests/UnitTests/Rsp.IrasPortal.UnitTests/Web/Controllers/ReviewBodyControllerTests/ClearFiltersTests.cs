using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.UnitTests;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

public class ClearFiltersTests : TestServiceBase<ReviewBodyController>
{
    private readonly DefaultHttpContext _http;

    public ClearFiltersTests()
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
    public void ClearFilters_WithoutSearchQuery_ShouldRedirectWithEmptySearch()
    {
        // Act
        var result = Sut.ClearFilters();

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("rbc:viewreviewbodies");

        redirect.RouteValues.ShouldContainKeyAndValue("pageNumber", 1);
        redirect.RouteValues.ShouldContainKeyAndValue("pageSize", 20);
        redirect.RouteValues.ShouldContainKeyAndValue("fromPagination", true);

        var sessionJson = _http.Session.GetString(SessionKeys.ReviewBodiesSearch);
        sessionJson.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ClearFilters_WithSearchQuery_ShouldRetainSearchQuery()
    {
        // Arrange
        const string testQuery = "test-term";

        // Act
        var result = Sut.ClearFilters(testQuery);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("rbc:viewreviewbodies");

        redirect.RouteValues.ShouldContainKeyAndValue("pageNumber", 1);
        redirect.RouteValues.ShouldContainKeyAndValue("pageSize", 20);
        redirect.RouteValues.ShouldContainKeyAndValue("fromPagination", true);

        var sessionJson = _http.Session.GetString(SessionKeys.ReviewBodiesSearch);
        sessionJson.ShouldNotBeNullOrWhiteSpace();
    }
}