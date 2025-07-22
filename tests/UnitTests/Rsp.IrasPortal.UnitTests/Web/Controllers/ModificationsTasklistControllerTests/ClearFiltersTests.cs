using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ModificationsTasklistControllerTests;

public class ClearFiltersTests : TestServiceBase<ModificationsTasklistController>
{
    [Fact]
    public void ClearFilters_ShouldRedirectToSearch()
    {
        // Arrange
        var controller = Sut;
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        // Act
        var result = controller.ClearFilters();

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(Sut.Index));
    }

    [Fact]
    public void ClearFilters_ShouldRetainOnlyIrasIdAndRedirect()
    {
        // Arrange
        var controller = Sut;
        var tempDataProvider = Mock.Of<ITempDataProvider>();
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), tempDataProvider);

        var originalSearch = new ApprovalsSearchModel
        {
            IrasId = "IRAS123",
            ShortProjectTitle = "TestOrg",
        };

        controller.TempData[TempDataKeys.ApprovalsSearchModel] = JsonSerializer.Serialize(originalSearch);

        // Act
        var result = controller.ClearFilters();

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(Sut.Index));

        // Check that TempData only contains the IrasId and nothing else
        controller.TempData.TryGetValue(TempDataKeys.ApprovalsSearchModel, out var updatedJson).ShouldBeTrue();
        updatedJson.ShouldNotBeNull();

        var updatedSearch = JsonSerializer.Deserialize<ApprovalsSearchModel>(updatedJson!.ToString()!);
        updatedSearch.ShouldNotBeNull();
        updatedSearch!.IrasId.ShouldBe("IRAS123");
        updatedSearch.ShortProjectTitle.ShouldBeNull();
    }
}