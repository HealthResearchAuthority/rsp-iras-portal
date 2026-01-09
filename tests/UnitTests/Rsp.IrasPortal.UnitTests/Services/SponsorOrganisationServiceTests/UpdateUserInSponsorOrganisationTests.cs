using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.SponsorOrganisationServiceTests;

public class UpdateUserInSponsorOrganisationTests : TestServiceBase<SponsorOrganisationService>
{
    [AutoData, Theory]
    public async Task UpdateUserInSponsorOrganisation_ShouldReturnContent(SponsorOrganisationUserDto updateUser)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<SponsorOrganisationUserDto>>(r =>
            r.IsSuccessStatusCode &&
            r.StatusCode == HttpStatusCode.OK &&
            r.Content == updateUser);

        var client = Mocker.GetMock<ISponsorOrganisationsServiceClient>();
        client.Setup(c => c.UpdateUserInSponsorOrganisation(updateUser.RtsId, updateUser.UserId.ToString(), updateUser))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.UpdateSponsorOrganisationUser(updateUser);

        // Assert
        client.Verify(c => c.UpdateUserInSponsorOrganisation(updateUser.RtsId, updateUser.UserId.ToString(), updateUser), Times.Once);
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
        result.Content.UserId.ShouldBe(updateUser.UserId);
    }
}