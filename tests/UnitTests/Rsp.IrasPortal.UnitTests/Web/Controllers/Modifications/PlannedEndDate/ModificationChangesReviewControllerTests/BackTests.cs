using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.Modifications.PlannedEndDate.ModificationChangesReviewControllerTests;

public class BackTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public void Back_RemovesReviewChangesFlagAndRedirects()
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModificationPlannedEndDate.ReviewChanges] = true
        };
        Sut.TempData = tempData;

        // Act
        var result = Sut.Back();

        // Assert
        result.ShouldBeOfType<RedirectToRouteResult>().RouteName.ShouldBe("pmc:affectingorganisations");
        Sut.TempData.ContainsKey(TempDataKeys.ProjectModificationPlannedEndDate.ReviewChanges).ShouldBeFalse();
    }
}