using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
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
        var result = await Sut.ParticipatingOrganisation();

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
        var result = await Sut.ParticipatingOrganisation();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("SearchOrganisation");

        var model = viewResult.Model.ShouldBeOfType<SearchOrganisationViewModel>();
        model.ShortTitle.ShouldBeEmpty();
        model.IrasId.ShouldBeEmpty();
        model.ModificationIdentifier.ShouldBeEmpty();
        model.PageTitle.ShouldBeEmpty();
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
            [TempDataKeys.ProjectModificationIdentifier] = "220360/1",
            [TempDataKeys.SpecificAreaOfChangeText] = "Addition of new sites",
            [TempDataKeys.OrganisationSearchModel] = mockedJson
        };

        var searchResponse = new OrganisationSearchResponse
        {
            Organisations = new List<OrganisationDto>
            {
                new() { Id = "1", Name = "Hospital A", Address = "Address A", CountryName = "PL", Type = "Site" }
            },
            TotalCount = 1
        };

        Mocker
            .GetMock<IRtsService>()
            .Setup(s => s.GetOrganisationsByName(expectedSearchTerm, null, 1, 10))
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
            Organisations = new List<OrganisationDto>(),
            TotalCount = totalCount
        };

        Mocker
            .GetMock<IRtsService>()
            .Setup(s => s.GetOrganisationsByName("Hospital", null, pageNumber, pageSize))
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