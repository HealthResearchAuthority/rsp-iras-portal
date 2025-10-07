using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.SponsorOrganisationServiceTests;

public class GetSponsorOrganisationTests : TestServiceBase<SponsorOrganisationService>
{
    [Theory]
    [AutoData]
    public async Task GetSponsorOrganisations_Should_Return_Failure_Response_When_Client_Returns_Failure()
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<AllSponsorOrganisationsResponse>>(apiResponse =>
            !apiResponse.IsSuccessStatusCode &&
            apiResponse.StatusCode == HttpStatusCode.BadRequest);

        var client = Mocker.GetMock<ISponsorOrganisationsServiceClient>();
        client.Setup(c =>
                c.GetAllSponsorOrganisations(1, 100, nameof(SponsorOrganisationDto.SponsorOrganisationName), "asc",
                    null))
            .ReturnsAsync(apiResponse);

        var sut = new SponsorOrganisationService(client.Object);

        // Act
        var result = await sut.GetAllSponsorOrganisations(null, 1, 100);

        // Assert
        result.ShouldBeOfType<ServiceResponse<AllSponsorOrganisationsResponse>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Verify
        client.Verify(
            c => c.GetAllSponsorOrganisations(1, 100, nameof(SponsorOrganisationDto.SponsorOrganisationName), "asc",
                null), Times.Once());
    }

    [Theory]
    [AutoData]
    public async Task GetSponsorOrganisations_Should_Return_Success_Response_When_Client_Returns_Success(
        List<SponsorOrganisationDto> sponsorOrganisations)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<AllSponsorOrganisationsResponse>>(apiResponse =>
            apiResponse.IsSuccessStatusCode &&
            apiResponse.StatusCode == HttpStatusCode.OK &&
            apiResponse.Content == new AllSponsorOrganisationsResponse { SponsorOrganisations = sponsorOrganisations });

        var client = Mocker.GetMock<ISponsorOrganisationsServiceClient>();
        client.Setup(c =>
                c.GetAllSponsorOrganisations(1, 100, nameof(SponsorOrganisationDto.SponsorOrganisationName), "asc",
                    null))
            .ReturnsAsync(apiResponse);

        var sut = new SponsorOrganisationService(client.Object);

        // Act
        var result = await sut.GetAllSponsorOrganisations(null, 1, 100);

        // Assert
        result.ShouldBeOfType<ServiceResponse<AllSponsorOrganisationsResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content!.SponsorOrganisations.ShouldBeEquivalentTo(sponsorOrganisations);

        // Verify
        client.Verify(
            c => c.GetAllSponsorOrganisations(1, 100, nameof(SponsorOrganisationDto.SponsorOrganisationName), "asc",
                null), Times.Once());
    }
}