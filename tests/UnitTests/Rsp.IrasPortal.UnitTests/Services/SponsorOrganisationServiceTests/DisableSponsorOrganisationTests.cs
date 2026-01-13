using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.SponsorOrganisationServiceTests;

public class DisableSponsorOrganisationTests : TestServiceBase<SponsorOrganisationService>
{
    [Fact]
    public async Task DisableSponsorOrganisation_ShouldReturnContent()
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<SponsorOrganisationDto>>(r =>
            r.IsSuccessStatusCode &&
            r.StatusCode == HttpStatusCode.OK &&
            r.Content == new SponsorOrganisationDto
                { RtsId = "123" });

        var client = Mocker.GetMock<ISponsorOrganisationsServiceClient>();
        client.Setup(c => c.DisableSponsorOrganisation("123"))
            .ReturnsAsync(apiResponse);

        var rtsService = Mocker.GetMock<IRtsService>();

        var sut = new SponsorOrganisationService(client.Object, rtsService.Object);

        // Act
        var result = await sut.DisableSponsorOrganisation("123");

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
    }
}