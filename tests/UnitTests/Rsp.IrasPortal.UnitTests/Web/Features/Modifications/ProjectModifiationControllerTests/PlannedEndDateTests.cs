using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.ProjectModifiationControllerTests;

public class PlannedEndDateTests : TestServiceBase<ModificationsController>
{
    [Fact]
    public async Task PlannedEndDate_ReturnsCorrectView_WithPopulatedViewModel()
    {
        // Arrange
        var expectedShortTitle = "ASPIRE";
        var expectedIrasId = "220360";
        var expectedModId = "220360/1";
        var expectedPageTitle = "Addition of new sites";

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ShortProjectTitle] = expectedShortTitle,
            [TempDataKeys.IrasId] = expectedIrasId,
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = expectedModId,
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = expectedPageTitle
        };

        // Act
        var result = Sut.PlannedEndDate();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("PlannedEndDate");

        var model = viewResult.Model.ShouldBeOfType<PlannedEndDateViewModel>();
        model.ShortTitle.ShouldBe(expectedShortTitle);
        model.IrasId.ShouldBe(expectedIrasId);
        model.ModificationIdentifier.ShouldBe(expectedModId);
        model.SpecificAreaOfChange.ShouldBe(expectedPageTitle);
    }

    [Fact]
    public async Task PlannedEndDate_ReturnsView_WithEmptyStrings_WhenTempDataMissing()
    {
        // Arrange
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        // Act
        var result = Sut.PlannedEndDate();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("PlannedEndDate");

        var model = viewResult.Model.ShouldBeOfType<PlannedEndDateViewModel>();
        model.ShortTitle.ShouldBeEmpty();
        model.IrasId.ShouldBeEmpty();
        model.ModificationIdentifier.ShouldBeEmpty();
        model.SpecificAreaOfChange.ShouldBeEmpty();
    }
}