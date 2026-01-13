using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.SponsorOrganisationServiceTests;

public class CreateSponsorOrganisationTests : TestServiceBase<SponsorOrganisationService>
{
    [Fact]
    public async Task CreateSponsorOrganisation_Should_Return_Success_Response_When_Client_Succeeds()
    {
        // Arrange
        var sponsorDto = new SponsorOrganisationDto
        {
            RtsId = "87765",
            SponsorOrganisationName = "Acme Research Ltd",
            Countries = ["England"]
        };

        var apiResponse = Mock.Of<IApiResponse<SponsorOrganisationDto>>(r =>
            r.IsSuccessStatusCode &&
            r.StatusCode == HttpStatusCode.Created &&
            r.Content == sponsorDto);

        var client = Mocker.GetMock<ISponsorOrganisationsServiceClient>();
        client.Setup(c => c.CreateSponsorOrganisation(sponsorDto))
            .ReturnsAsync(apiResponse);

        var rtsService = Mocker.GetMock<IRtsService>();
        var sut = new SponsorOrganisationService(client.Object, rtsService.Object);

        // Act
        var result = await sut.CreateSponsorOrganisation(sponsorDto);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldBe(sponsorDto);

        client.Verify(c => c.CreateSponsorOrganisation(sponsorDto), Times.Once);
        rtsService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateSponsorOrganisation_Should_Return_Failure_Response_When_Client_Fails()
    {
        // Arrange
        var sponsorDto = new SponsorOrganisationDto { RtsId = "99999", SponsorOrganisationName = "Broken Org" };

        var apiResponse = Mock.Of<IApiResponse<SponsorOrganisationDto>>(r =>
            !r.IsSuccessStatusCode &&
            r.StatusCode == HttpStatusCode.BadRequest &&
            r.Content == null);

        var client = Mocker.GetMock<ISponsorOrganisationsServiceClient>();
        client.Setup(c => c.CreateSponsorOrganisation(sponsorDto))
            .ReturnsAsync(apiResponse);

        var rtsService = Mocker.GetMock<IRtsService>();
        var sut = new SponsorOrganisationService(client.Object, rtsService.Object);

        // Act
        var result = await sut.CreateSponsorOrganisation(sponsorDto);

        // Assert
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        result.Content.ShouldBeNull();

        client.Verify(c => c.CreateSponsorOrganisation(sponsorDto), Times.Once);
    }
}