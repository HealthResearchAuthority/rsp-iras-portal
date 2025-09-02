using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApprovalsControllerTests;

public class ClearFiltersTests : TestServiceBase<ApprovalsController>
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
        // No session set-up needed; controller should still redirect.
        var result = Sut.ClearFilters();

        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(Sut.Index));
    }

    [Fact]
    public void ClearFilters_ShouldRetainOnlyIrasIdAndRedirect()
    {
        // Arrange
        var originalSearch = new ApprovalsSearchModel
        {
            IrasId = "IRAS123",
            ShortProjectTitle = "TestOrg"
        };

        _http.Session.SetString(SessionKeys.ApprovalsSearch, JsonSerializer.Serialize(originalSearch));

        // Act
        var result = Sut.ClearFilters();

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(Sut.Index));

        var updatedJson = _http.Session.GetString(SessionKeys.ApprovalsSearch);
        updatedJson.ShouldNotBeNull();

        var updatedSearch = JsonSerializer.Deserialize<ApprovalsSearchModel>(updatedJson!);
        updatedSearch.ShouldNotBeNull();
        updatedSearch!.IrasId.ShouldBe("IRAS123");
        updatedSearch.ShortProjectTitle.ShouldBeNull(); // all other filters cleared
    }
}