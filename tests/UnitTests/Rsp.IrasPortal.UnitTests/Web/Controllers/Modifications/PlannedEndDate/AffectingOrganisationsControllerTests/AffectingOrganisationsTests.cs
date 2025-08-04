using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.Modifications.PlannedEndDate.AffectingOrganisationsControllerTests;

public class AffectingOrganisationsTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public void AffectingOrganisations_ReturnsView_WithPopulatedModelFromTempData()
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ShortProjectTitle] = "Test Project",
            [TempDataKeys.IrasId] = "12345",
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = "MOD-1",
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = "Test Area",
            [TempDataKeys.ProjectModificationPlannedEndDate.AffectedOrganisationsLocations] = JsonSerializer.Serialize(new List<string> { "England" }),
            [TempDataKeys.ProjectModificationPlannedEndDate.AffectedAllOrSomeOrganisations] = "OPT0323",
            [TempDataKeys.ProjectModificationPlannedEndDate.AffectedOrganisationsRequireAdditionalResources] = "OPT0004"
        };

        Sut.TempData = tempData;

        // Act
        var result = Sut.AffectingOrganisations();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<AffectingOrganisationsViewModel>();
        model.ShortTitle.ShouldBe("Test Project");
        model.IrasId.ShouldBe("12345");
        model.ModificationIdentifier.ShouldBe("MOD-1");
        model.PageTitle.ShouldBe("Test Area");
        model.SelectedLocations.ShouldContain("England");
        model.SelectedAffectedOrganisations.ShouldBe("OPT0323");
        model.SelectedAdditionalResources.ShouldBe("OPT0004");
    }

    [Fact]
    public void AffectingOrganisations_ReturnsView_WithEmptyModel_WhenTempDataMissing()
    {
        // Arrange
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        // Act
        var result = Sut.AffectingOrganisations();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<AffectingOrganisationsViewModel>();
        model.ShortTitle.ShouldBeEmpty();
        model.IrasId.ShouldBeEmpty();
        model.ModificationIdentifier.ShouldBeEmpty();
        model.PageTitle.ShouldBeEmpty();
        model.SelectedLocations.ShouldBeEmpty();
        model.SelectedAffectedOrganisations.ShouldBeNull();
        model.SelectedAdditionalResources.ShouldBeNull();
    }
}