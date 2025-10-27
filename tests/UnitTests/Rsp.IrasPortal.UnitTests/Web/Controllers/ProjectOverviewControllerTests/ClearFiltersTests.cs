using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers.ProjectOverview;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ProjectOverviewControllerTests;

public class ClearFiltersTests : TestServiceBase<ProjectOverviewController>
{
    private readonly DefaultHttpContext _http;
    private readonly Mock<ITempDataDictionary> MockTempData;

    public ClearFiltersTests()
    {
        MockTempData = new Mock<ITempDataDictionary>();
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
        Sut.TempData = MockTempData.Object;
        var result = Sut.ClearFilters();

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToRouteResult>();
        redirectResult.RouteName.ShouldBe("pov:postapproval");
    }

    [Fact]
    public void ClearFilters_ShouldRetainOnlyModificationIdAndRedirect()
    {
        // Arrange
        Sut.TempData = MockTempData.Object;
        var originalSearch = new ApprovalsSearchModel
        {
            ModificationId = "Mod123",
        };
        _http.Session.SetString(SessionKeys.MyTasklist, JsonSerializer.Serialize(originalSearch));

        // Act
        var result = Sut.ClearFilters();

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToRouteResult>();
        redirectResult.RouteName.ShouldBe("pov:postapproval");

        var updatedJson = _http.Session.GetString(SessionKeys.MyTasklist);
        updatedJson.ShouldNotBeNull();

        var updatedSearch = JsonSerializer.Deserialize<ApprovalsSearchModel>(updatedJson!);
        updatedSearch.ShouldNotBeNull();
        updatedSearch!.ModificationId.ShouldBe("Mod123");
    }
}