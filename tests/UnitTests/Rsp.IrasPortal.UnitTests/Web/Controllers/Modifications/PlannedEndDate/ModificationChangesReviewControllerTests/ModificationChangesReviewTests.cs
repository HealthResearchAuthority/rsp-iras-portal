using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;
using Shouldly;
using Xunit;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.Modifications.PlannedEndDate.ModificationChangesReviewControllerTests;

public class ModificationChangesReviewTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public void ModificationChangesReview_ReturnsView_WithPopulatedModelFromTempData()
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
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
        var model = viewResult.Model.ShouldBeOfType<AffectingOrganisationsViewModel>();
        model.ShortTitle.ShouldBe("Test Project");
        model.IrasId.ShouldBe("12345");
        model.ModificationIdentifier.ShouldBe("MOD-1");
        model.PageTitle.ShouldBe("Test Area");
    }
}
