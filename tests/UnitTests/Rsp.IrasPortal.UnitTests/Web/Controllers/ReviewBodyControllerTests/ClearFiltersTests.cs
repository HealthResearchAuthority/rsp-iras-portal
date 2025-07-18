using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class ClearFiltersTests : TestServiceBase<ReviewBodyController>
{
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

        var complexSearchQuery = redirect.RouteValues["complexSearchQuery"]?.ToString();
        complexSearchQuery.ShouldNotBeNull();

        var deserialized = JsonSerializer.Deserialize<ReviewBodySearchModel>(complexSearchQuery!);
        deserialized.ShouldNotBeNull();
        deserialized!.SearchQuery.ShouldBeNull(); // No value was passed
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

        var complexSearchQuery = redirect.RouteValues["complexSearchQuery"]?.ToString();
        complexSearchQuery.ShouldNotBeNull();

        var deserialized = JsonSerializer.Deserialize<ReviewBodySearchModel>(complexSearchQuery!);
        deserialized.ShouldNotBeNull();
        deserialized!.SearchQuery.ShouldBe(testQuery);
    }
}
