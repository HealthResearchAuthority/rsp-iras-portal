using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Identity;
using Rsp.IrasPortal.Web.Features.SponsorWorkspace.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Features.SponsorWorkspace;

public class SponsorWorkspaceControllerTests : TestServiceBase<SponsorWorkspaceController>
{
    private readonly DefaultHttpContext _http;

    public SponsorWorkspaceControllerTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

    [Theory, AutoData]
    public async Task SponsorWorkspaceMenu_ShouldReturnServiceError_WhenUserNotExist(ClaimsPrincipal user)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.BadRequest,
        };

        Mocker.GetMock<IUserManagementService>()
           .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(serviceResponse);
        _http.User = user;

        // Act
        var result = await Sut.SponsorWorkspace();

        // Assert
        var viewResult = result.ShouldBeOfType<StatusCodeResult>();
        viewResult.StatusCode.ShouldBe(400);
    }

    [Theory, AutoData]
    public async Task SponsorWorkspace_ShouldReturnView_WhenOrganisationExists(
    ClaimsPrincipal userClaims,
    User mockUser,
    SponsorOrganisationDto sponsorOrganisation,
    OrganisationDto rtsOrganisation)
    {
        // Arrange
        var gid = Guid.NewGuid();

        // Dodaj użytkownika do sponsorOrganisation.Users
        sponsorOrganisation.Users = new List<SponsorOrganisationUserDto>
        {
            new SponsorOrganisationUserDto
            {
                Id = Guid.NewGuid(),
                UserId = gid
            }
        };

        var mockUserResponse = new UserResponse
        {
            User = mockUser with { Id = gid.ToString() }
        };

        var userResponse = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = mockUserResponse
        };

        var sponsorResponse = new ServiceResponse<IEnumerable<SponsorOrganisationDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new List<SponsorOrganisationDto> { sponsorOrganisation }
        };

        var rtsResponse = new ServiceResponse<OrganisationDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = rtsOrganisation
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(It.IsAny<string>(), null, null))
            .ReturnsAsync(userResponse);

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetAllActiveSponsorOrganisationsForEnabledUser(gid))
            .ReturnsAsync(sponsorResponse);

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation(It.IsAny<string>()))
            .ReturnsAsync(rtsResponse);

        _http.User = userClaims;

        // Act
        var result = await Sut.SponsorWorkspace();

        // Assert
        result.ShouldBeOfType<ViewResult>();
    }
}