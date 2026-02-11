using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.Identity;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Helpers;

namespace Rsp.Portal.UnitTests.Web.Controllers.SponsorOrganisationsControllerTests;

public class ConfirmDisableUserTests : TestServiceBase<SponsorOrganisationsController>
{
    private readonly DefaultHttpContext _http;
    private readonly Mock<IUserManagementService> _userManagementService;

    public ConfirmDisableUserTests()
    {
        _userManagementService = Mocker.GetMock<IUserManagementService>();
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

    [Theory]
    [AutoData]
    public async Task ConfirmDisableUser_ReturnsToView(
        SponsorOrganisationUserDto sponsorOrganisationUserDto,
        SponsorOrganisationDto sponsorOrganisationDto,
        UserResponse userResponse)
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
            .Setup(s => s.DisableUserInSponsorOrganisation(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(serviceResponseGetUserInSponsorOrganisation);

        var serviceResponseGetAllActiveOrganisationsForUser = new ServiceResponse<IEnumerable<SponsorOrganisationDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new List<SponsorOrganisationDto> { sponsorOrganisationDto }
        };

        Mocker.GetMock<ISponsorOrganisationService>()
           .Setup(s => s.GetAllActiveSponsorOrganisationsForEnabledUser(It.IsAny<Guid>()))
           .ReturnsAsync(serviceResponseGetAllActiveOrganisationsForUser);

        var serviceResponseGetUser = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = userResponse
        };

        Mocker.GetMock<IUserManagementService>()
           .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(serviceResponseGetUser);

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ShowNotificationBanner] = true
        };

        // Act
        var result = await Sut.ConfirmDisableUser(rtsId, userGuid);

        // Assert
        result.ShouldBeOfType<RedirectToActionResult>();
    }

    [Fact]
    public async Task HandleDisableOrganisationUserRole_WhenUserHasNoOtherOrganisations_RemovesSponsorAndOrgAdminRoles()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test.test@example.com";
        var userResponse = SetupUser(email, userId);

        var serviceResponseGetAllActiveOrganisationsForUser = new ServiceResponse<IEnumerable<SponsorOrganisationDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new List<SponsorOrganisationDto>()
        };

        Mocker.GetMock<ISponsorOrganisationService>()
           .Setup(s => s.GetAllActiveSponsorOrganisationsForEnabledUser(It.IsAny<Guid>()))
           .ReturnsAsync(serviceResponseGetAllActiveOrganisationsForUser);

        var serviceResponseGetUser = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = userResponse
        };

        _userManagementService
           .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(serviceResponseGetUser);

        // -------------------------
        // Act
        // -------------------------
        var updatedRoles = await SponsorOrganisationUsersHelper.HandleDisableOrganisationUserRole(
            Mocker.GetMock<ISponsorOrganisationService>().Object,
            userId,
            Mocker.GetMock<IUserManagementService>().Object);

        // -------------------------
        // Assert
        // -------------------------

        var rolesToRemove = string.Join(",", Roles.Sponsor, Roles.OrganisationAdministrator);

        _userManagementService.Verify(s => s.UpdateRoles(
            email,
            rolesToRemove,
            string.Empty), Times.Once);
    }

    [Fact]
    public async Task HandleDisableOrganisationUserRole_WhenUserHasOnlySponsorOrganisations_RemovesOrgAdminRoles()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test.test@example.com";
        var userResponse = SetupUser(email, userId);

        var serviceResponseGetAllActiveOrganisationsForUser = new ServiceResponse<IEnumerable<SponsorOrganisationDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new List<SponsorOrganisationDto>() { new SponsorOrganisationDto
                {
                    Id = Guid.NewGuid(),
                    Users = new List<SponsorOrganisationUserDto>
                    {
                        new SponsorOrganisationUserDto
                        {
                            UserId = userId,
                            SponsorRole = Roles.Sponsor
                        }
                    }
                }
            }
        };

        Mocker.GetMock<ISponsorOrganisationService>()
           .Setup(s => s.GetAllActiveSponsorOrganisationsForEnabledUser(It.IsAny<Guid>()))
           .ReturnsAsync(serviceResponseGetAllActiveOrganisationsForUser);

        var serviceResponseGetUser = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = userResponse
        };

        _userManagementService
           .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(serviceResponseGetUser);

        // -------------------------
        // Act
        // -------------------------
        var updatedRoles = await SponsorOrganisationUsersHelper.HandleDisableOrganisationUserRole(
            Mocker.GetMock<ISponsorOrganisationService>().Object,
            userId,
            Mocker.GetMock<IUserManagementService>().Object);

        // -------------------------
        // Assert
        // -------------------------

        var rolesToRemove = string.Join(",", Roles.OrganisationAdministrator);

        _userManagementService.Verify(s => s.UpdateRoles(
            email,
            rolesToRemove,
            string.Empty), Times.Once);
    }

    [Fact]
    public async Task HandleDisableOrganisationUserRole_WhenUserHasOnlyOrgAdmin_RemovesSponsorRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test.test@example.com";
        var userResponse = SetupUser(email, userId);

        var serviceResponseGetAllActiveOrganisationsForUser = new ServiceResponse<IEnumerable<SponsorOrganisationDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new List<SponsorOrganisationDto>() { new SponsorOrganisationDto
                {
                    Id = Guid.NewGuid(),
                    Users = new List<SponsorOrganisationUserDto>
                    {
                        new SponsorOrganisationUserDto
                        {
                            UserId = userId,
                            SponsorRole = Roles.OrganisationAdministrator
                        }
                    }
                }
            }
        };

        Mocker.GetMock<ISponsorOrganisationService>()
           .Setup(s => s.GetAllActiveSponsorOrganisationsForEnabledUser(It.IsAny<Guid>()))
           .ReturnsAsync(serviceResponseGetAllActiveOrganisationsForUser);

        var serviceResponseGetUser = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = userResponse
        };

        _userManagementService
           .Setup(s => s.GetUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(serviceResponseGetUser);

        // -------------------------
        // Act
        // -------------------------
        var updatedRoles = await SponsorOrganisationUsersHelper.HandleDisableOrganisationUserRole(
            Mocker.GetMock<ISponsorOrganisationService>().Object,
            userId,
            Mocker.GetMock<IUserManagementService>().Object);

        // -------------------------
        // Assert
        // -------------------------

        var rolesToRemove = string.Join(",", Roles.Sponsor);

        _userManagementService.Verify(s => s.UpdateRoles(
            email,
            rolesToRemove,
            string.Empty), Times.Once);
    }

    private UserResponse SetupUser(string email, Guid userId)
    {
        var userResponse = new UserResponse
        {
            User = new User(
                        userId.ToString(),
                        "azure-ad-12345",
                        "Mr",
                        "Test",
                        "Test",
                        email,
                        "Software Developer",
                        "orgName",
                        "+44 7700 900123",
                        "United Kingdom",
                        "Active",
                        DateTime.UtcNow,
                        DateTime.UtcNow.AddDays(-2),
                        DateTime.UtcNow),
            Roles = new List<string> {
                Roles.Sponsor,
                Roles.OrganisationAdministrator
            }
        };

        return userResponse;
    }
}