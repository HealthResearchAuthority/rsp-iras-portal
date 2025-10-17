﻿using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.SponsorOrganisationServiceTests;

public class GetUserInSponsorOrganisationTests : TestServiceBase<SponsorOrganisationService>
{
    [Fact]
    public async Task GetUserInSponsorOrganisation_ShouldReturnContent()
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<SponsorOrganisationUserDto>>(r =>
            r.IsSuccessStatusCode &&
            r.StatusCode == HttpStatusCode.OK &&
            r.Content == new SponsorOrganisationUserDto
                { RtsId = "123" });

        var client = Mocker.GetMock<ISponsorOrganisationsServiceClient>();
        client.Setup(c => c.GetUserInSponsorOrganisation("123", It.IsAny<Guid>()))
            .ReturnsAsync(apiResponse);

        var rtsService = Mocker.GetMock<IRtsService>();

        var sut = new SponsorOrganisationService(client.Object, rtsService.Object);

        // Act
        var result = await sut.GetUserInSponsorOrganisation("123", Guid.NewGuid());

        // Assert
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.Content.ShouldNotBeNull();
    }
}