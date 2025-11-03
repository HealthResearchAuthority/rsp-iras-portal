using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Identity;
using Rsp.IrasPortal.Web.Features.SponsorWorkspace;

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
    public async Task SponsorWorkspaceMenu_ShouldReturnView_WhenOrganisationExists
    (
    ClaimsPrincipal userClaims,
    User mockUser
    )
    {
        // Arrange
        var organisationName = "TEST ORGANISATION";
        var userWithOrganisation = mockUser with { Organisation = organisationName };

        var serviceResponse = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new UserResponse
            {
                User = userWithOrganisation
            }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        _http.User = userClaims;

        // Act
        var result = await Sut.SponsorWorkspace();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        (Sut.ViewBag.SponsorOrganisationName as string).ShouldBe(organisationName);
    }

    [Theory, AutoData]
    public async Task SponsorWorkspaceMenu_ShouldThrow_WhenOrganisationIsMissing
    (
    ClaimsPrincipal userClaims,
    User mockUser
    )
    {
        // Arrange
        var userWithoutOrganisation = mockUser with { Organisation = null };

        var serviceResponse = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new UserResponse
            {
                User = userWithoutOrganisation
            }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        _http.User = userClaims;

        // Act
        var result = await Sut.SponsorWorkspace();

        // Assert
        var viewResult = result.ShouldBeOfType<StatusCodeResult>();
        viewResult.StatusCode.ShouldBe(400);
    }
}