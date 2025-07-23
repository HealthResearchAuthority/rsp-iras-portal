using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ModificationsTasklistControllerTests;

public class AssignModificationsTests : TestServiceBase<ModificationsTasklistController>
{
    [Fact]
    public async Task AssignModifications_WhenNoIdsSelected_AddsModelErrorAndRedirects()
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        Sut.TempData = tempData;

        // Act
        var result = await Sut.AssignModifications(new List<string>());

        // Assert
        Sut.ModelState.ContainsKey(ModificationsTasklist.ModificationToAssignNotSelected).ShouldBeTrue();
        tempData.ContainsKey(TempDataKeys.ModelState).ShouldBeTrue();

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.Index));
    }
}