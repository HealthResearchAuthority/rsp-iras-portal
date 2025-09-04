using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.Modifications.PlannedEndDate.ModificationChangesReviewControllerTests;

public class BackTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public void Back_WithPlannedEndDateJourney_RemovesReviewChangesFlagAndRedirects()
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.JourneyType] = ModificationJourneyTypes.PlannedEndDate,
            [TempDataKeys.ProjectModificationPlannedEndDate.ReviewChanges] = true
        };
        Sut.TempData = tempData;

        // Act
        var result = Sut.Back();

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(Sut.AffectingOrganisations));
        Sut.TempData.ContainsKey(TempDataKeys.ProjectModificationPlannedEndDate.ReviewChanges).ShouldBeFalse();
    }

    [Fact]
    public void Back_WithInvalidJourneyType_ReturnsErrorView()
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.JourneyType] = "InvalidType",
            [TempDataKeys.ProjectModificationPlannedEndDate.ReviewChanges] = true
        };
        Sut.TempData = tempData;

        // Act
        var result = Sut.Back();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Error");
        Sut.TempData.ContainsKey(TempDataKeys.ProjectModificationPlannedEndDate.ReviewChanges).ShouldBeFalse();
    }
}