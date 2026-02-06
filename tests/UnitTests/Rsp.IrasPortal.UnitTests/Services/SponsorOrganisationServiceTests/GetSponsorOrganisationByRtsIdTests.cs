using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.SponsorOrganisationServiceTests;

public class GetSponsorOrganisationByRtsIdTests : TestServiceBase<SponsorOrganisationService>
{
    [Fact]
    public async Task GetSponsorOrganisationByRtsId_Should_Return_Success_Response_When_Client_Succeeds()
    {
        // Arrange
        const string rtsId = "87765";

        var apiResponse = Mock.Of<IApiResponse<AllSponsorOrganisationsResponse>>(r =>
            r.IsSuccessStatusCode &&
            r.StatusCode == HttpStatusCode.OK &&
            r.Content == new AllSponsorOrganisationsResponse
            {
                SponsorOrganisations = new List<SponsorOrganisationDto>
                {
                    new() { RtsId = rtsId, SponsorOrganisationName = "Acme Research Ltd" }
                }
            });

        var client = Mocker.GetMock<ISponsorOrganisationsServiceClient>();
        client.Setup(c => c.GetSponsorOrganisationByRtsId(rtsId))
            .ReturnsAsync(apiResponse);

        var rtsService = Mocker.GetMock<IRtsService>();
        var sut = new SponsorOrganisationService(client.Object, rtsService.Object);

        // Act
        var result = await sut.GetSponsorOrganisationByRtsId(rtsId);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.SponsorOrganisations.First().RtsId.ShouldBe(rtsId);

        client.Verify(c => c.GetSponsorOrganisationByRtsId(rtsId), Times.Once);
        rtsService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSponsorOrganisationByRtsId_Should_Return_Failure_Response_When_Client_Fails()
    {
        // Arrange
        const string rtsId = "12345";

        var apiResponse = Mock.Of<IApiResponse<AllSponsorOrganisationsResponse>>(r =>
            !r.IsSuccessStatusCode &&
            r.StatusCode == HttpStatusCode.InternalServerError &&
            r.Content == null);

        var client = Mocker.GetMock<ISponsorOrganisationsServiceClient>();
        client.Setup(c => c.GetSponsorOrganisationByRtsId(rtsId))
            .ReturnsAsync(apiResponse);

        var rtsService = Mocker.GetMock<IRtsService>();
        var sut = new SponsorOrganisationService(client.Object, rtsService.Object);

        // Act
        var result = await sut.GetSponsorOrganisationByRtsId(rtsId);

        // Assert
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        result.Content.ShouldBeNull();

        client.Verify(c => c.GetSponsorOrganisationByRtsId(rtsId), Times.Once);
    }
}