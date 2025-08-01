using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.Modifications.PlannedEndDate.OrganisationTypeModificationControllerTests;

public class PlannedEndDateOrganisationTypeTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public void PlannedEndDateOrganisationType_ReturnsView_WithPopulatedModelFromTempData()
    {
        // Arrange
        var selectedTypes = new List<string> { "NHS/HSC" };
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ShortProjectTitle] = "Test Project",
            [TempDataKeys.IrasId] = "12345",
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = "MOD-1",
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = "Test Area",
            [TempDataKeys.ProjectModificationPlannedEndDate.AffectingOrganisationsType] = JsonSerializer.Serialize(selectedTypes)
        };

        // Act
        var result = Sut.PlannedEndDateOrganisationType();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<PlannedEndDateOrganisationTypeViewModel>();
        model.ShortTitle.ShouldBe("Test Project");
        model.IrasId.ShouldBe("12345");
        model.ModificationIdentifier.ShouldBe("MOD-1");
        model.PageTitle.ShouldBe("Test Area");
        model.SelectedOrganisationTypes.ShouldContain("NHS/HSC");
    }

    [Fact]
    public void PlannedEndDateOrganisationType_ReturnsView_WithEmptyModel_WhenTempDataMissing()
    {
        // Arrange
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        // Act
        var result = Sut.PlannedEndDateOrganisationType();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<PlannedEndDateOrganisationTypeViewModel>();
        model.ShortTitle.ShouldBeEmpty();
        model.IrasId.ShouldBeEmpty();
        model.ModificationIdentifier.ShouldBeEmpty();
        model.PageTitle.ShouldBeEmpty();
        model.SelectedOrganisationTypes.ShouldBeEmpty();
    }
}