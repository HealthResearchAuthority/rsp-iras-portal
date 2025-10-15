using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Identity;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.SponsorOrganisationsControllerTests;

public class SubmitAddUserTests : TestServiceBase<SponsorOrganisationsController>
{
    private readonly DefaultHttpContext _http;

    public SubmitAddUserTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

    [Fact]
    public async Task SubmitAddUser_ShouldReturnView_WithMappedModel_WhenBothServicesSucceed()
    {
        // Arrange
        const string rtsId = "87765";
        const string orgName = "Acme Research Ltd";

        var userGuid = Guid.NewGuid();
        var userId = userGuid.ToString();

        var sponsorResponse = new ServiceResponse<SponsorOrganisationUserDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new SponsorOrganisationUserDto
            {
                RtsId = rtsId,
                UserId = userGuid,
                Id = Guid.NewGuid()
            }
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.AddUserToSponsorOrganisation(It.IsAny<SponsorOrganisationUserDto>()))
            .ReturnsAsync(sponsorResponse);

        Mocker.GetMock<IUserManagementService>()
            .Setup(x => x.GetUser(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UserResponse
                {
                    User = new User(
                        userId,
                        "azure-ad-12345",
                        "Mr",
                        "Test",
                        "Test",
                        "test.test@example.com",
                        "Software Developer",
                        orgName,
                        "+44 7700 900123",
                        "United Kingdom",
                        "Active",
                        DateTime.UtcNow,
                        DateTime.UtcNow.AddDays(-2),
                        DateTime.UtcNow)
                }
            });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ShowEditLink] = false
        };

        // Act
        var result = await Sut.SubmitAddUser(rtsId, userGuid, Guid.NewGuid());

        // Assert
        result.ShouldBeOfType<RedirectToActionResult>();

        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.AddUserToSponsorOrganisation(It.IsAny<SponsorOrganisationUserDto>()), Times.Once);

        Mocker.GetMock<IUserManagementService>()
            .Verify(x => x.GetUser(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAddUser_ShouldReturnView_WithMappedModel_WhenOneServiceErrors()
    {
        // Arrange
        const string rtsId = "87765";
        const string orgName = "Acme Research Ltd";

        var userGuid = Guid.NewGuid();
        var userId = userGuid.ToString();

        var sponsorResponse = new ServiceResponse<SponsorOrganisationUserDto>
        {
            StatusCode = HttpStatusCode.BadGateway,
            Content = new SponsorOrganisationUserDto
            {
                RtsId = rtsId,
                UserId = userGuid,
                Id = Guid.NewGuid()
            }
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.AddUserToSponsorOrganisation(It.IsAny<SponsorOrganisationUserDto>()))
            .ReturnsAsync(sponsorResponse);

        Mocker.GetMock<IUserManagementService>()
            .Setup(x => x.GetUser(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UserResponse
                {
                    User = new User(
                        userId,
                        "azure-ad-12345",
                        "Mr",
                        "Test",
                        "Test",
                        "test.test@example.com",
                        "Software Developer",
                        orgName,
                        "+44 7700 900123",
                        "United Kingdom",
                        "Active",
                        DateTime.UtcNow,
                        DateTime.UtcNow.AddDays(-2),
                        DateTime.UtcNow)
                }
            });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ShowEditLink] = false
        };

        // Act
        var result = await Sut.SubmitAddUser(rtsId, userGuid, Guid.NewGuid());

        // Assert
        result.ShouldBeOfType<StatusCodeResult>();

        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.AddUserToSponsorOrganisation(It.IsAny<SponsorOrganisationUserDto>()), Times.Once);

        Mocker.GetMock<IUserManagementService>()
            .Verify(x => x.GetUser(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);
    }
}