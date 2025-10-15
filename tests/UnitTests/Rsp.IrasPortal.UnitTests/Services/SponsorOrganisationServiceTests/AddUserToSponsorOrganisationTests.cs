using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.SponsorOrganisationServiceTests;

public class AddUserToSponsorOrganisationTests : TestServiceBase<SponsorOrganisationService>
{
    [Fact]
    public async Task AddUserToSponsorOrganisation_Should_Forward_Dto_To_Client_And_Return_ServiceResponse_Success()
    {
        // Arrange
        var dto = new SponsorOrganisationUserDto
        {
            // fill only what your ctor/validators require
            RtsId = "87765",
            UserId = Guid.NewGuid()
        };

        var apiResponse = Mock.Of<IApiResponse<SponsorOrganisationUserDto>>(r =>
            r.IsSuccessStatusCode &&
            r.StatusCode == HttpStatusCode.OK &&
            r.Content == dto);

        var client = Mocker.GetMock<ISponsorOrganisationsServiceClient>();
        client.Setup(c => c.AddUserToSponsorOrganisation(dto))
            .ReturnsAsync(apiResponse);

        var rtsService = Mocker.GetMock<IRtsService>();
        var sut = new SponsorOrganisationService(client.Object, rtsService.Object);

        // Act
        var result = await sut.AddUserToSponsorOrganisation(dto);

        // Assert
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldNotBeNull();
        result.Content.ShouldBe(dto);

        client.Verify(c => c.AddUserToSponsorOrganisation(dto), Times.Once);
        rtsService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddUserToSponsorOrganisation_Should_Propagate_Failure_From_Client()
    {
        // Arrange
        var dto = new SponsorOrganisationUserDto
        {
            RtsId = "87765",
            UserId = Guid.NewGuid()
        };

        var apiResponse = Mock.Of<IApiResponse<SponsorOrganisationUserDto>>(r =>
            !r.IsSuccessStatusCode &&
            r.StatusCode == HttpStatusCode.BadRequest &&
            r.Content == null);

        var client = Mocker.GetMock<ISponsorOrganisationsServiceClient>();
        client.Setup(c => c.AddUserToSponsorOrganisation(dto))
            .ReturnsAsync(apiResponse);

        var rtsService = Mocker.GetMock<IRtsService>();
        var sut = new SponsorOrganisationService(client.Object, rtsService.Object);

        // Act
        var result = await sut.AddUserToSponsorOrganisation(dto);

        // Assert
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        result.Content.ShouldBeNull();

        client.Verify(c => c.AddUserToSponsorOrganisation(dto), Times.Once);
        rtsService.VerifyNoOtherCalls();
    }
}