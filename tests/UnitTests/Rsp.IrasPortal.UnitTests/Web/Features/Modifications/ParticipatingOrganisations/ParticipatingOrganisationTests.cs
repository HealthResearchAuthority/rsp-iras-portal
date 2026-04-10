using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications.ParticipatingOrganisations.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications.ParticipatingOrganisations;

public class ParticipatingOrganisationTests : TestServiceBase<ParticipatingOrganisationsController>
{
    [Fact]
    public async Task ParticipatingOrganisation_ReturnsCorrectView_WithPopulatedViewModel()
    {
        // Arrange
        const string expectedShortTitle = "ASPIRE";
        const string expectedIrasId = "220360";
        const string expectedModId = "220360/1";
        const string expectedPageTitle = "Addition of new sites";

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ShortProjectTitle] = expectedShortTitle,
            [TempDataKeys.IrasId] = expectedIrasId,
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = expectedModId,
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = expectedPageTitle
        };

        // Act
        var result = await Sut.ParticipatingOrganisations();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("SearchOrganisation");

        var model = viewResult.Model.ShouldBeOfType<SearchOrganisationViewModel>();
        model.ShortTitle.ShouldBe(expectedShortTitle);
        model.IrasId.ShouldBe(expectedIrasId);
        model.ModificationIdentifier.ShouldBe(expectedModId);
        model.SpecificAreaOfChange.ShouldBe(expectedPageTitle);
    }

    [Fact]
    public async Task ParticipatingOrganisation_ReturnsView_WithEmptyStrings_WhenTempDataMissing()
    {
        // Arrange
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        // Act
        var result = await Sut.ParticipatingOrganisations();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("SearchOrganisation");

        var model = viewResult.Model.ShouldBeOfType<SearchOrganisationViewModel>();
        model.ShortTitle.ShouldBeEmpty();
        model.IrasId.ShouldBeEmpty();
        model.ModificationIdentifier.ShouldBeEmpty();
        model.SpecificAreaOfChange.ShouldBeEmpty();
    }

    [Fact]
    public async Task ParticipatingOrganisation_ReturnsCorrectView_WithSearchTerm_UsingMockedJson()
    {
        // Arrange
        const string mockedJson = """
        {
            "SearchNameTerm": "Hospital"
        }
        """;

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ShortProjectTitle] = "ASPIRE",
            [TempDataKeys.IrasId] = "220360",
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = "220360/1",
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = "Addition of new sites",
            [TempDataKeys.OrganisationSearchModel] = mockedJson
        };

        // Act
        var result = await Sut.ParticipatingOrganisations();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("SearchOrganisation");

        var model = viewResult.Model.ShouldBeOfType<SearchOrganisationViewModel>();
        model.ShortTitle.ShouldBe("ASPIRE");
        model.IrasId.ShouldBe("220360");
        model.ModificationIdentifier.ShouldBe("220360/1");
        model.SpecificAreaOfChange.ShouldBe("Addition of new sites");
    }
}