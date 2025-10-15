using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.SponsorOrganisationServiceTests;

public class GetSponsorOrganisationTests : TestServiceBase<SponsorOrganisationService>
{
    [Fact]
    public async Task GetSponsorOrganisations_Should_Forward_Paging_And_Sort_Params_To_Client()
    {
        // Arrange
        var pageNumber = 2;
        var pageSize = 50;
        var sortField = nameof(SponsorOrganisationDto.RtsId);
        var sortDirection = SortDirections.Descending;

        var apiResponse = Mock.Of<IApiResponse<AllSponsorOrganisationsResponse>>(r =>
            r.IsSuccessStatusCode &&
            r.StatusCode == HttpStatusCode.OK &&
            r.Content == new AllSponsorOrganisationsResponse
                { SponsorOrganisations = new List<SponsorOrganisationDto>() });

        var client = Mocker.GetMock<ISponsorOrganisationsServiceClient>();
        client.Setup(c => c.GetAllSponsorOrganisations(
                pageNumber, pageSize, sortField, sortDirection, It.IsAny<SponsorOrganisationSearchRequest?>()))
            .ReturnsAsync(apiResponse);

        var rtsService = Mocker.GetMock<IRtsService>();

        var sut = new SponsorOrganisationService(client.Object, rtsService.Object);

        // Act
        var result = await sut.GetAllSponsorOrganisations(
            null,
            pageNumber,
            pageSize,
            sortField,
            sortDirection);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        client.Verify(c => c.GetAllSponsorOrganisations(pageNumber, pageSize, sortField, sortDirection,
            It.IsAny<SponsorOrganisationSearchRequest?>()), Times.Once);
        rtsService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSponsorOrganisations_Should_Not_Call_Rts_Name_Search_When_SearchQuery_Is_Null()
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<AllSponsorOrganisationsResponse>>(r =>
            r.IsSuccessStatusCode &&
            r.StatusCode == HttpStatusCode.OK &&
            r.Content == new AllSponsorOrganisationsResponse
                { SponsorOrganisations = new List<SponsorOrganisationDto>() });

        var client = Mocker.GetMock<ISponsorOrganisationsServiceClient>();
        client.Setup(c => c.GetAllSponsorOrganisations(
                1, 20, "name", "asc", null))
            .ReturnsAsync(apiResponse);

        var rtsService = Mocker.GetMock<IRtsService>();

        var sut = new SponsorOrganisationService(client.Object, rtsService.Object);

        // Act
        var result = await sut.GetAllSponsorOrganisations();

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        rtsService.Verify(
            rs => rs.GetOrganisationsByName(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(),
                null, "asc", "name"), Times.Never);
    }

    [Fact]
    public async Task GetSponsorOrganisations_Should_Not_Call_Rts_Name_Search_When_SearchQuery_IsNot_Null()
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<AllSponsorOrganisationsResponse>>(r =>
            r.IsSuccessStatusCode &&
            r.StatusCode == HttpStatusCode.OK &&
            r.Content == new AllSponsorOrganisationsResponse
            {
                SponsorOrganisations = new List<SponsorOrganisationDto>
                {
                    new()
                    {
                        SponsorOrganisationName = "Org 1",
                        RtsId = "1",
                        Countries = new List<string>
                        {
                            "England"
                        }
                    }
                }
            });

        var client = Mocker.GetMock<ISponsorOrganisationsServiceClient>();
        client.Setup(c => c.GetAllSponsorOrganisations(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<SponsorOrganisationSearchRequest>()))
            .ReturnsAsync(apiResponse);

        var rtsService = Mocker.GetMock<IRtsService>();

        rtsService.Setup(x =>
            x.GetOrganisationsByName(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(
            new ServiceResponse<OrganisationSearchResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationSearchResponse
                {
                    Organisations = new List<OrganisationDto>
                    {
                        new()
                        {
                            Name = "Org 1",
                            Id = "1",
                            CountryName = "England"
                        }
                    }
                }
            });

        rtsService.Setup(x =>
            x.GetOrganisation(It.IsAny<string>())).ReturnsAsync(
            new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationDto
                {
                    Name = "Org 1",
                    Id = "1",
                    CountryName = "England"
                }
            });

        var sut = new SponsorOrganisationService(client.Object, rtsService.Object);

        // Act
        var result = await sut.GetAllSponsorOrganisations(new SponsorOrganisationSearchRequest
        {
            SearchQuery = "org"
        });

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        rtsService.Verify(
            rs => rs.GetOrganisationsByName(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(),
                null, "asc", "name"), Times.Once);
    }

    [Fact]
    public async Task GetSponsorOrganisations_Should_Not_Call_Rts_Name_Search_When_NoResults()
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<AllSponsorOrganisationsResponse>>(r =>
            r.IsSuccessStatusCode &&
            r.StatusCode == HttpStatusCode.OK &&
            r.Content == new AllSponsorOrganisationsResponse
            {
            
            });

        var client = Mocker.GetMock<ISponsorOrganisationsServiceClient>();
        client.Setup(c => c.GetAllSponsorOrganisations(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<SponsorOrganisationSearchRequest>()))
            .ReturnsAsync(apiResponse);

        var rtsService = Mocker.GetMock<IRtsService>();

        rtsService.Setup(x =>
            x.GetOrganisationsByName(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(
            new ServiceResponse<OrganisationSearchResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationSearchResponse
                {
              
                }
            });

        rtsService.Setup(x =>
            x.GetOrganisation(It.IsAny<string>())).ReturnsAsync(
            new ServiceResponse<OrganisationDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new OrganisationDto
                {
                    Name = "Org 1",
                    Id = "1",
                    CountryName = "England"
                }
            });

        var sut = new SponsorOrganisationService(client.Object, rtsService.Object);

        // Act
        var result = await sut.GetAllSponsorOrganisations(new SponsorOrganisationSearchRequest
        {
            SearchQuery = "org"
        });

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        rtsService.Verify(
            rs => rs.GetOrganisationsByName(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(),
                null, "asc", "name"), Times.Once);
    }
}