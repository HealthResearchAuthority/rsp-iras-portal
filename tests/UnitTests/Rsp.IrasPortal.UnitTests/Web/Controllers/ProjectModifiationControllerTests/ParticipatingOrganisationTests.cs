using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ProjectModifiationControllerTests;

public class ParticipatingOrganisationTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public async Task ParticipatingOrganisation_ReturnsCorrectView_WithPopulatedViewModel()
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
            [TempDataKeys.ProjectModificationIdentifier] = expectedModId,
            [TempDataKeys.SpecificAreaOfChangeText] = expectedPageTitle
        };

        // Act
        var result = Sut.ParticipatingOrganisation();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("SearchOrganisation");

        var model = viewResult.Model.ShouldBeOfType<SearchOrganisationViewModel>();
        model.ShortTitle.ShouldBe(expectedShortTitle);
        model.IrasId.ShouldBe(expectedIrasId);
        model.ModificationIdentifier.ShouldBe(expectedModId);
        model.PageTitle.ShouldBe(expectedPageTitle);
    }

    [Fact]
    public async Task ParticipatingOrganisation_ReturnsView_WithEmptyStrings_WhenTempDataMissing()
    {
        // Arrange
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        // Act
        var result = Sut.ParticipatingOrganisation();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("SearchOrganisation");

        var model = viewResult.Model.ShouldBeOfType<SearchOrganisationViewModel>();
        model.ShortTitle.ShouldBeEmpty();
        model.IrasId.ShouldBeEmpty();
        model.ModificationIdentifier.ShouldBeEmpty();
        model.PageTitle.ShouldBeEmpty();
    }
}