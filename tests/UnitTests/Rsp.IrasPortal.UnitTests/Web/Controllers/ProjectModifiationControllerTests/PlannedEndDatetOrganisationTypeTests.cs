using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ProjectModifiationControllerTests;

public class PlannedEndDatetOrganisationTypeTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public async Task PlannedEndDatetOrganisationType_ReturnsCorrectView_WithPopulatedViewModel()
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
        var result = Sut.PlannedEndDateOrganisationType();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBeNull();

        var model = viewResult.Model.ShouldBeOfType<PlannedEndDateOrganisationTypeViewModel>();
        model.ShortTitle.ShouldBe(expectedShortTitle);
        model.IrasId.ShouldBe(expectedIrasId);
        model.ModificationIdentifier.ShouldBe(expectedModId);
        model.PageTitle.ShouldBe(expectedPageTitle);

        // Ensure OrganisationTypes dictionary contains expected keys and values
        model.OrganisationTypes.ShouldContainKey("OPT0025");
        model.OrganisationTypes["OPT0025"].ShouldBe("NHS/HSC");

        model.OrganisationTypes.ShouldContainKey("OPT0026");
        model.OrganisationTypes["OPT0026"].ShouldBe("Non-NHS/HSC");

        // Validate default SelectedOrganisationTypes list is initialized
        model.SelectedOrganisationTypes.ShouldNotBeNull();
        model.SelectedOrganisationTypes.ShouldBeEmpty();
    }

    [Fact]
    public void PlannedEndDatetOrganisationType_ReturnsView_WithEmptyStrings_WhenTempDataMissing()
    {
        // Arrange
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        // Act
        var result = Sut.PlannedEndDateOrganisationType();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBeNull();

        var model = viewResult.Model.ShouldBeOfType<PlannedEndDateOrganisationTypeViewModel>();
        model.ShortTitle.ShouldBeEmpty();
        model.IrasId.ShouldBeEmpty();
        model.ModificationIdentifier.ShouldBeEmpty();
        model.PageTitle.ShouldBeEmpty();
    }
}