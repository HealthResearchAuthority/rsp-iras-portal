using System.Diagnostics.Metrics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.Identity;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.SponsorOrganisationsControllerTests;

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
                Id = Guid.NewGuid(),
                IsAuthoriser = true,
                SponsorRole = Roles.Sponsor
            }
        };

        var sponsorRtsResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new AllSponsorOrganisationsResponse
            {
                SponsorOrganisations = new List<SponsorOrganisationDto>
                {
                    new()
                    {
                        IsActive = true,
                        CreatedDate = new DateTime(2024, 5, 1)
                    }
                }
            }
        };

        var organisationResponse = new ServiceResponse<OrganisationDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new OrganisationDto { Id = rtsId, Name = orgName }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(x => x.SearchUsers(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>?>(), // searchQuery you pass in the Act
                It.Is<int>(pn => pn == 1),
                It.Is<int>(ps => ps == 20)))
            .ReturnsAsync(new ServiceResponse<UsersResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UsersResponse
                {
                    TotalCount = 1,
                    Users = new List<User>
                    {
                        new(
                            Guid.NewGuid().ToString(),
                            "azure-ad-12345",
                            "Mr",
                            "Test",
                            "Test",
                            "test.test@example.com",
                            "Software Developer",
                            orgName, // IMPORTANT: match org if your action filters by org
                            "+44 7700 900123",
                            "United Kingdom",
                            "Active",
                            DateTime.UtcNow,
                            DateTime.UtcNow.AddDays(-2),
                            DateTime.UtcNow)
                    }
                }
            });

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation(rtsId))
            .ReturnsAsync(organisationResponse);

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationByRtsId(rtsId))
            .ReturnsAsync(sponsorRtsResponse);

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
    public async Task SubmitAddUser_ShouldReturnView_WithMappedModel_WhenBothServicesSucceed_OrganisationAdministrator()
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
                Id = Guid.NewGuid(),
                IsAuthoriser = true,
                SponsorRole = Roles.OrganisationAdministrator
            }
        };

        var sponsorRtsResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new AllSponsorOrganisationsResponse
            {
                SponsorOrganisations = new List<SponsorOrganisationDto>
                {
                    new()
                    {
                        IsActive = true,
                        CreatedDate = new DateTime(2024, 5, 1)
                    }
                }
            }
        };

        var organisationResponse = new ServiceResponse<OrganisationDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new OrganisationDto { Id = rtsId, Name = orgName }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(x => x.SearchUsers(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>?>(), // searchQuery you pass in the Act
                It.Is<int>(pn => pn == 1),
                It.Is<int>(ps => ps == 20)))
            .ReturnsAsync(new ServiceResponse<UsersResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UsersResponse
                {
                    TotalCount = 1,
                    Users = new List<User>
                    {
                        new(
                            Guid.NewGuid().ToString(),
                            "azure-ad-12345",
                            "Mr",
                            "Test",
                            "Test",
                            "test.test@example.com",
                            "Software Developer",
                            orgName, // IMPORTANT: match org if your action filters by org
                            "+44 7700 900123",
                            "United Kingdom",
                            "Active",
                            DateTime.UtcNow,
                            DateTime.UtcNow.AddDays(-2),
                            DateTime.UtcNow)
                    }
                }
            });

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation(rtsId))
            .ReturnsAsync(organisationResponse);

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationByRtsId(rtsId))
            .ReturnsAsync(sponsorRtsResponse);

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

        var sponsorRtsResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new AllSponsorOrganisationsResponse
            {
                SponsorOrganisations = new List<SponsorOrganisationDto>
                {
                    new()
                    {
                        IsActive = true,
                        CreatedDate = new DateTime(2024, 5, 1)
                    }
                }
            }
        };

        var organisationResponse = new ServiceResponse<OrganisationDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new OrganisationDto { Id = rtsId, Name = orgName }
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(x => x.SearchUsers(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>?>(), // searchQuery you pass in the Act
                It.Is<int>(pn => pn == 1),
                It.Is<int>(ps => ps == 20)))
            .ReturnsAsync(new ServiceResponse<UsersResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UsersResponse
                {
                    TotalCount = 1,
                    Users = new List<User>
                    {
                        new(
                            Guid.NewGuid().ToString(),
                            "azure-ad-12345",
                            "Mr",
                            "Test",
                            "Test",
                            "test.test@example.com",
                            "Software Developer",
                            orgName, // IMPORTANT: match org if your action filters by org
                            "+44 7700 900123",
                            "United Kingdom",
                            "Active",
                            DateTime.UtcNow,
                            DateTime.UtcNow.AddDays(-2),
                            DateTime.UtcNow)
                    }
                }
            });

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation(rtsId))
            .ReturnsAsync(organisationResponse);

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationByRtsId(rtsId))
            .ReturnsAsync(sponsorRtsResponse);

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