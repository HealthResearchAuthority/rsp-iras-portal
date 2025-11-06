using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.SponsorOrganisationServiceTests;

public class GetAllActiveSponsorOrganisationsForEnabledUserTests : TestServiceBase<SponsorOrganisationService>
{
    [Theory, AutoData]
    public async Task GetAllActiveSponsorOrganisationsForEnabledUser_ShouldReturnContent(List<SponsorOrganisationDto> expectedOrganisations)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<IEnumerable<SponsorOrganisationDto>>>(r =>
            r.IsSuccessStatusCode &&
            r.StatusCode == HttpStatusCode.OK &&
            r.Content == expectedOrganisations);

        var client = Mocker.GetMock<ISponsorOrganisationsServiceClient>();
        client.Setup(c => c.GetAllActiveSponsorOrganisationsForEnabledUser(It.IsAny<Guid>()))
              .ReturnsAsync(apiResponse);

        var rtsService = Mocker.GetMock<IRtsService>();
        var sut = new SponsorOrganisationService(client.Object, rtsService.Object);

        var userId = Guid.NewGuid();

        // Act
        var result = await sut.GetAllActiveSponsorOrganisationsForEnabledUser(userId);

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        result.Content.ShouldBe(expectedOrganisations);
    }
}