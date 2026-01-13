using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.ApplicationControllerTests;

public class ClearFiltersTests : TestServiceBase<ApplicationController>
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
    public void ClearFilters_ShouldRedirectToSearch()
    {
        // Act
        var result = Sut.ClearFilters();

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(Sut.Welcome));
    }

    [Fact]
    public void ClearFilters_ShouldRetainOnlySearchTitleTermAndRedirect()
    {
        // Arrange
        var originalSearch = new ApplicationSearchModel
        {
            SearchTitleTerm = "abcd",
            Status = ["In draft"]
        };

        _http.Session.SetString(SessionKeys.ProjectRecordSearch, JsonSerializer.Serialize(originalSearch));

        // Act
        var result = Sut.ClearFilters();

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(Sut.Welcome));

        var updatedJson = _http.Session.GetString(SessionKeys.ProjectRecordSearch);
        updatedJson.ShouldNotBeNull();

        var updatedSearch = JsonSerializer.Deserialize<ApplicationSearchModel>(updatedJson!);
        updatedSearch.ShouldNotBeNull();
        updatedSearch!.SearchTitleTerm.ShouldBe("abcd");
        updatedSearch.Status.ShouldBeEmpty();
    }
}