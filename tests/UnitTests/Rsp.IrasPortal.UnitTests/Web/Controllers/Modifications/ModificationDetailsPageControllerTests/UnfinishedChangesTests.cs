using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Features.Modifications.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.Modifications.ModificationDetailsPageControllerTests;

public class UnfinishedChangesTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public void UnfinishedChanges_ReturnsCorrectView()
    {
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
        var result = Sut.UnfinishedChanges();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("UnfinishedChanges");
        viewResult.Model.ShouldBeOfType<ModificationDetailsViewModel>();
    }
}