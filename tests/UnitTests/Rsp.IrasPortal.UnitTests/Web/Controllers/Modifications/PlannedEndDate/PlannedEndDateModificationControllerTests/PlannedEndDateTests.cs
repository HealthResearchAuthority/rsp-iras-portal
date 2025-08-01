using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;
using Shouldly;
using Xunit;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.Modifications.PlannedEndDate.PlannedEndDateModificationControllerTests;

public class PlannedEndDateTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public void PlannedEndDate_ReturnsView_WithPopulatedModelFromTempData()
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ShortProjectTitle] = "Test Project",
            [TempDataKeys.IrasId] = "12345",
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = "MOD-1",
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = "Test Area",
            [TempDataKeys.PlannedProjectEndDate] = "01 January 2025",
            [TempDataKeys.ProjectModificationPlannedEndDate.NewPlannedProjectEndDate] = "02 February 2026"
        };
        Sut.TempData = tempData;

        // Act
        var result = Sut.PlannedEndDate();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("PlannedEndDate");
        var model = viewResult.Model.ShouldBeOfType<PlannedEndDateViewModel>();
        model.ShortTitle.ShouldBe("Test Project");
        model.IrasId.ShouldBe("12345");
        model.ModificationIdentifier.ShouldBe("MOD-1");
        model.PageTitle.ShouldBe("Test Area");
        model.CurrentPlannedEndDate.ShouldBe("01 January 2025");
        model.NewPlannedEndDate.Day.ShouldBe("02");
        model.NewPlannedEndDate.Month.ShouldBe("02");
        model.NewPlannedEndDate.Year.ShouldBe("2026");
    }

    [Fact]
    public void PlannedEndDate_ReturnsView_WithEmptyModel_WhenTempDataMissing()
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
        model.PageTitle.ShouldBeEmpty();
        model.CurrentPlannedEndDate.ShouldBeEmpty();
        model.NewPlannedEndDate.Day.ShouldBeNull();
        model.NewPlannedEndDate.Month.ShouldBeNull();
        model.NewPlannedEndDate.Year.ShouldBeNull();
    }
}
