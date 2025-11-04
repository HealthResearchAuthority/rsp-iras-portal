using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications.ParticipatingOrganisations.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.ParticipatingOrganisations;

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
        var result = await Sut.ParticipatingOrganisation();

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
        var result = await Sut.ParticipatingOrganisation();

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
        const string expectedSearchTerm = "Hospital";
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

        var searchResponse = new OrganisationSearchResponse
        {
            Organisations =
            [
                new() { Id = "1", Name = "Hospital A", Address = "Address A", CountryName = "PL", Type = "Site" }
            ],
            TotalCount = 1
        };

        Mocker
            .GetMock<IRtsService>()
            .Setup(s => s.GetOrganisationsByName(expectedSearchTerm, OrganisationRoles.Sponsor, 1, 10,null,"asc", "name"))
            .ReturnsAsync(
                new ServiceResponse<OrganisationSearchResponse>()
                    .WithContent(searchResponse, HttpStatusCode.OK)
            );

        // Act
        var result = await Sut.ParticipatingOrganisation();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("SearchOrganisation");

        var model = viewResult.Model.ShouldBeOfType<SearchOrganisationViewModel>();
        model.Search.SearchNameTerm.ShouldBe(expectedSearchTerm);
        model.Organisations.First().Organisation.Name.ShouldBe("Hospital A");
        model.Pagination!.TotalCount.ShouldBe(1);
    }

    [Theory]
    [AutoData]
    public async Task ParticipatingOrganisation_SetsPaginationCorrectly(
    int totalCount,
    int pageNumber,
    int pageSize)
    {
        // Arrange
        pageNumber = Math.Max(1, pageNumber % 10);
        pageSize = Math.Max(1, pageSize % 50);

        const string mockedJson = """
        {
            "SearchNameTerm": "Hospital"
        }
        """;

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.OrganisationSearchModel] = mockedJson
        };

        var response = new OrganisationSearchResponse
        {
            Organisations = [],
            TotalCount = totalCount
        };

        Mocker
            .GetMock<IRtsService>()
            .Setup(s => s.GetOrganisationsByName("Hospital", OrganisationRoles.Sponsor, pageNumber, pageSize, null, "asc", "name"))
            .ReturnsAsync(
                new ServiceResponse<OrganisationSearchResponse>()
                    .WithContent(response, HttpStatusCode.OK)
            );

        // Act
        var result = await Sut.ParticipatingOrganisation(pageNumber, pageSize);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<SearchOrganisationViewModel>();

        model.Pagination.ShouldNotBeNull();
        model.Pagination.PageNumber.ShouldBe(pageNumber);
        model.Pagination.PageSize.ShouldBe(pageSize);
        model.Pagination.TotalCount.ShouldBe(totalCount);
        model.Pagination.FormName.ShouldBe("organisation-selection");
    }
}