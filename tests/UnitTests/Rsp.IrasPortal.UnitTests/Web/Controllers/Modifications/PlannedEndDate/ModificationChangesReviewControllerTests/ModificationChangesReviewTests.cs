using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.Modifications.PlannedEndDate.ModificationChangesReviewControllerTests;

public class ModificationChangesReviewTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public void ModificationChangesReview_WithPlannedEndDateJourney_ReturnsViewWithPopulatedModel()
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.JourneyType] = ModificationJourneyTypes.PlannedEndDate,
            [TempDataKeys.ShortProjectTitle] = "Test Project",
            [TempDataKeys.IrasId] = "12345",
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = "MOD-1",
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = "Test Area"
        };
        Sut.TempData = tempData;

        // Act
        var result = Sut.ModificationChangesReview();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("PlannedEndDateReview");

        var model = viewResult.Model.ShouldBeOfType<AffectingOrganisationsViewModel>();
        model.ShortTitle.ShouldBe("Test Project");
        model.IrasId.ShouldBe("12345");
        model.ModificationIdentifier.ShouldBe("MOD-1");
        model.PageTitle.ShouldBe("Test Area");
    }

    [Fact]
    public void ModificationChangesReview_WithInvalidJourneyType_ReturnsErrorView()
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.JourneyType] = "InvalidType"
        };
        Sut.TempData = tempData;

        // Act
        var result = Sut.ModificationChangesReview();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Error");
        viewResult.Model.ShouldNotBeNull();
    }
}