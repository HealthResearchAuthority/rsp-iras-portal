using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.SponsorOrganisationServiceTests;

public class DisableUserInSponsorOrganisationTests : TestServiceBase<SponsorOrganisationService>
{
    [Fact]
    public async Task DisableUserInSponsorOrganisation_ShouldReturnContent()
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<SponsorOrganisationUserDto>>(r =>
            r.IsSuccessStatusCode &&
            r.StatusCode == HttpStatusCode.OK &&
            r.Content == new SponsorOrganisationUserDto
                { RtsId = "123" });

        var client = Mocker.GetMock<ISponsorOrganisationsServiceClient>();
        client.Setup(c => c.DisableUserInSponsorOrganisation("123", It.IsAny<Guid>()))
            .ReturnsAsync(apiResponse);

        var rtsService = Mocker.GetMock<IRtsService>();

        var sut = new SponsorOrganisationService(client.Object, rtsService.Object);

        // Act
        var result = await sut.DisableUserInSponsorOrganisation("123", Guid.NewGuid());

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
    }
}