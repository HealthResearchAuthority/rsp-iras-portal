using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Features.Modifications.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.Modifications.PlannedEndDate.ModificationChangesReviewControllerTests;

public class SponsorReferenceTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public void SponsorReference_ReturnsViewWithPopulatedModel()
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ShortProjectTitle] = "Test Project",
            [TempDataKeys.IrasId] = "12345",
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = "MOD-1",
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = "Sponsor Reference"
        };
        Sut.TempData = tempData;

        // Act
        var result = Sut.SponsorReference();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<SponsorReferenceViewModel>();
        model.ShortTitle.ShouldBe("Test Project");
        model.IrasId.ShouldBe("12345");
        model.ModificationIdentifier.ShouldBe("MOD-1");
        model.PageTitle.ShouldBe("Sponsor Reference");
    }
}