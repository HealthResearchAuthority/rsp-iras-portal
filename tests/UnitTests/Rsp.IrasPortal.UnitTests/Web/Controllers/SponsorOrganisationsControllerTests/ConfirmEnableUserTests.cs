using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.SponsorOrganisationsControllerTests;

public class ConfirmEnableUserTests : TestServiceBase<SponsorOrganisationsController>
{
    private readonly DefaultHttpContext _http;

    public ConfirmEnableUserTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

    [Theory]
    [AutoData]
    public async Task ConfirmEnableUser_ReturnsToView(
        SponsorOrganisationUserDto sponsorOrganisationUserDto)
    {
        // Arrange
        const string rtsId = "87765";

        var userGuid = Guid.NewGuid();

        var serviceResponseGetUserInSponsorOrganisation = new ServiceResponse<SponsorOrganisationUserDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = sponsorOrganisationUserDto
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.EnableUserInSponsorOrganisation(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(serviceResponseGetUserInSponsorOrganisation);

        // Act
        var result = await Sut.ConfirmEnableUser(rtsId, userGuid);

        // Assert
        result.ShouldBeOfType<RedirectToActionResult>();
    }
}