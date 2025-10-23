using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Identity;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.SponsorOrganisationsControllerTests;

public class ViewSponsorOrganisationUsersTests : TestServiceBase<SponsorOrganisationsController>
{
    private readonly DefaultHttpContext _http;

    public ViewSponsorOrganisationUsersTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

    [Fact]
    public async Task ViewSponsorOrganisationUsers_ShouldReturnView_WithMappedModel_WhenBothServicesSucceed_GivenName()
    {
        // Arrange
        const string rtsId = "87765";
        const string orgName = "Acme Research Ltd";
        const string country = "England";

        var userGuid = Guid.NewGuid();
        var userId = userGuid.ToString();

        var sponsorResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new AllSponsorOrganisationsResponse
            {
                SponsorOrganisations = new List<SponsorOrganisationDto>
                {
                    new()
                    {
                        IsActive = true,
                        CreatedDate = new DateTime(2024, 5, 1),
                        Users = new List<SponsorOrganisationUserDto>
                        {
                            new()
                            {
                                RtsId = "123",
                                UserId = userGuid,
                                Id = Guid.NewGuid()
                            }
                        }
                    }
                }
            }
        };

        var organisationResponse = new ServiceResponse<OrganisationDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new OrganisationDto { Id = rtsId, Name = orgName, CountryName = country }
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationByRtsId(rtsId))
            .ReturnsAsync(sponsorResponse);

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation(rtsId))
            .ReturnsAsync(organisationResponse);

        Mocker.GetMock<IUserManagementService>()
            .Setup(x => x.GetUsersByIds(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync(new ServiceResponse<UsersResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UsersResponse
                {
                    TotalCount = 1,
                    Users = new List<User>
                    {
                        new(
                            userId,
                            "azure-ad-12345",
                            "Mr",
                            "Test",
                            "Test",
                            "test.test@example.com",
                            "Software Developer",
                            "Rsp Systems Ltd",
                            "+44 7700 900123",
                            "United Kingdom",
                            "Active",
                            DateTime.UtcNow,
                            DateTime.UtcNow.AddDays(-2),
                            DateTime.UtcNow)
                    }
                }
            });

        // Act
        var result = await Sut.ViewSponsorOrganisationUsers(rtsId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<SponsorOrganisationListUsersModel>();

        model.SponsorOrganisation.RtsId.ShouldBe(rtsId);
        model.SponsorOrganisation.SponsorOrganisationName.ShouldBe(orgName);
        model.SponsorOrganisation.Countries.ShouldContain(country);
        model.SponsorOrganisation.IsActive.ShouldBeTrue();
        model.SponsorOrganisation.UpdatedDate.ShouldBe(
            sponsorResponse.Content.SponsorOrganisations.First().CreatedDate);

        model.Users.ShouldNotBeNull();
        model.Users.ShouldHaveSingleItem();
        model.Users.First().Id.ShouldBe(userId);

        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.GetSponsorOrganisationByRtsId(rtsId), Times.Once);
        Mocker.GetMock<IRtsService>()
            .Verify(s => s.GetOrganisation(rtsId), Times.Once);
    }


    [Fact]
    public async Task ViewSponsorOrganisationUsers_ShouldReturnView_WithMappedModel_WhenBothServicesSucceed_FamilyName()
    {
        // Arrange
        const string rtsId = "87765";
        const string orgName = "Acme Research Ltd";
        const string country = "England";

        var userGuid = Guid.NewGuid();
        var userId = userGuid.ToString();

        var sponsorResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new AllSponsorOrganisationsResponse
            {
                SponsorOrganisations = new List<SponsorOrganisationDto>
                {
                    new()
                    {
                        IsActive = true,
                        CreatedDate = new DateTime(2024, 5, 1),
                        Users = new List<SponsorOrganisationUserDto>
                        {
                            new()
                            {
                                RtsId = "123",
                                UserId = userGuid,
                                Id = Guid.NewGuid()
                            }
                        }
                    }
                }
            }
        };

        var organisationResponse = new ServiceResponse<OrganisationDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new OrganisationDto { Id = rtsId, Name = orgName, CountryName = country }
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationByRtsId(rtsId))
            .ReturnsAsync(sponsorResponse);

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation(rtsId))
            .ReturnsAsync(organisationResponse);

        Mocker.GetMock<IUserManagementService>()
            .Setup(x => x.GetUsersByIds(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync(new ServiceResponse<UsersResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UsersResponse
                {
                    TotalCount = 1,
                    Users = new List<User>
                    {
                        new(
                            userId,
                            "azure-ad-12345",
                            "Mr",
                            "Test",
                            "Test",
                            "test.test@example.com",
                            "Software Developer",
                            "Rsp Systems Ltd",
                            "+44 7700 900123",
                            "United Kingdom",
                            "Active",
                            DateTime.UtcNow,
                            DateTime.UtcNow.AddDays(-2),
                            DateTime.UtcNow)
                    }
                }
            });

        // Act
        var result = await Sut.ViewSponsorOrganisationUsers(rtsId, null, 1, 20, "FamilyName");


        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<SponsorOrganisationListUsersModel>();

        model.SponsorOrganisation.RtsId.ShouldBe(rtsId);
        model.SponsorOrganisation.SponsorOrganisationName.ShouldBe(orgName);
        model.SponsorOrganisation.Countries.ShouldContain(country);
        model.SponsorOrganisation.IsActive.ShouldBeTrue();
        model.SponsorOrganisation.UpdatedDate.ShouldBe(
            sponsorResponse.Content.SponsorOrganisations.First().CreatedDate);

        model.Users.ShouldNotBeNull();
        model.Users.ShouldHaveSingleItem();
        model.Users.First().Id.ShouldBe(userId);

        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.GetSponsorOrganisationByRtsId(rtsId), Times.Once);
        Mocker.GetMock<IRtsService>()
            .Verify(s => s.GetOrganisation(rtsId), Times.Once);
    }

    [Fact]
    public async Task ViewSponsorOrganisationUsers_ShouldReturnView_WithMappedModel_WhenBothServicesSucceed_Email()
    {
        // Arrange
        const string rtsId = "87765";
        const string orgName = "Acme Research Ltd";
        const string country = "England";

        var userGuid = Guid.NewGuid();
        var userId = userGuid.ToString();

        var sponsorResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new AllSponsorOrganisationsResponse
            {
                SponsorOrganisations = new List<SponsorOrganisationDto>
                {
                    new()
                    {
                        IsActive = true,
                        CreatedDate = new DateTime(2024, 5, 1),
                        Users = new List<SponsorOrganisationUserDto>
                        {
                            new()
                            {
                                RtsId = "123",
                                UserId = userGuid,
                                Id = Guid.NewGuid()
                            }
                        }
                    }
                }
            }
        };

        var organisationResponse = new ServiceResponse<OrganisationDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new OrganisationDto { Id = rtsId, Name = orgName, CountryName = country }
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationByRtsId(rtsId))
            .ReturnsAsync(sponsorResponse);

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation(rtsId))
            .ReturnsAsync(organisationResponse);

        Mocker.GetMock<IUserManagementService>()
            .Setup(x => x.GetUsersByIds(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync(new ServiceResponse<UsersResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UsersResponse
                {
                    TotalCount = 1,
                    Users = new List<User>
                    {
                        new(
                            userId,
                            "azure-ad-12345",
                            "Mr",
                            "Test",
                            "Test",
                            "test.test@example.com",
                            "Software Developer",
                            "Rsp Systems Ltd",
                            "+44 7700 900123",
                            "United Kingdom",
                            "Active",
                            DateTime.UtcNow,
                            DateTime.UtcNow.AddDays(-2),
                            DateTime.UtcNow)
                    }
                }
            });

        // Act
        var result = await Sut.ViewSponsorOrganisationUsers(rtsId, null, 1, 20, "Email");


        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<SponsorOrganisationListUsersModel>();

        model.SponsorOrganisation.RtsId.ShouldBe(rtsId);
        model.SponsorOrganisation.SponsorOrganisationName.ShouldBe(orgName);
        model.SponsorOrganisation.Countries.ShouldContain(country);
        model.SponsorOrganisation.IsActive.ShouldBeTrue();
        model.SponsorOrganisation.UpdatedDate.ShouldBe(
            sponsorResponse.Content.SponsorOrganisations.First().CreatedDate);

        model.Users.ShouldNotBeNull();
        model.Users.ShouldHaveSingleItem();
        model.Users.First().Id.ShouldBe(userId);

        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.GetSponsorOrganisationByRtsId(rtsId), Times.Once);
        Mocker.GetMock<IRtsService>()
            .Verify(s => s.GetOrganisation(rtsId), Times.Once);
    }

    [Fact]
    public async Task
        ViewSponsorOrganisationUsers_ShouldReturnView_WithMappedModel_WhenBothServicesSucceed_CurrentLogin()
    {
        // Arrange
        const string rtsId = "87765";
        const string orgName = "Acme Research Ltd";
        const string country = "England";

        var userGuid = Guid.NewGuid();
        var userId = userGuid.ToString();

        var sponsorResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new AllSponsorOrganisationsResponse
            {
                SponsorOrganisations = new List<SponsorOrganisationDto>
                {
                    new()
                    {
                        IsActive = true,
                        CreatedDate = new DateTime(2024, 5, 1),
                        Users = new List<SponsorOrganisationUserDto>
                        {
                            new()
                            {
                                RtsId = "123",
                                UserId = userGuid,
                                Id = Guid.NewGuid()
                            }
                        }
                    }
                }
            }
        };

        var organisationResponse = new ServiceResponse<OrganisationDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new OrganisationDto { Id = rtsId, Name = orgName, CountryName = country }
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationByRtsId(rtsId))
            .ReturnsAsync(sponsorResponse);

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation(rtsId))
            .ReturnsAsync(organisationResponse);

        Mocker.GetMock<IUserManagementService>()
            .Setup(x => x.GetUsersByIds(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync(new ServiceResponse<UsersResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UsersResponse
                {
                    TotalCount = 1,
                    Users = new List<User>
                    {
                        new(
                            userId,
                            "azure-ad-12345",
                            "Mr",
                            "Test",
                            "Test",
                            "test.test@example.com",
                            "Software Developer",
                            "Rsp Systems Ltd",
                            "+44 7700 900123",
                            "United Kingdom",
                            "Active",
                            DateTime.UtcNow,
                            DateTime.UtcNow.AddDays(-2),
                            DateTime.UtcNow)
                    }
                }
            });

        // Act
        var result = await Sut.ViewSponsorOrganisationUsers(rtsId, null, 1, 20, "CurrentLogin");


        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<SponsorOrganisationListUsersModel>();

        model.SponsorOrganisation.RtsId.ShouldBe(rtsId);
        model.SponsorOrganisation.SponsorOrganisationName.ShouldBe(orgName);
        model.SponsorOrganisation.Countries.ShouldContain(country);
        model.SponsorOrganisation.IsActive.ShouldBeTrue();
        model.SponsorOrganisation.UpdatedDate.ShouldBe(
            sponsorResponse.Content.SponsorOrganisations.First().CreatedDate);

        model.Users.ShouldNotBeNull();
        model.Users.ShouldHaveSingleItem();
        model.Users.First().Id.ShouldBe(userId);

        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.GetSponsorOrganisationByRtsId(rtsId), Times.Once);
        Mocker.GetMock<IRtsService>()
            .Verify(s => s.GetOrganisation(rtsId), Times.Once);
    }

    [Fact]
    public async Task ViewSponsorOrganisationUsers_ShouldReturnView_WithMappedModel_WhenBothServicesSucceed_Status()
    {
        // Arrange
        const string rtsId = "87765";
        const string orgName = "Acme Research Ltd";
        const string country = "England";

        var userGuid = Guid.NewGuid();
        var userId = userGuid.ToString();

        var sponsorResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new AllSponsorOrganisationsResponse
            {
                SponsorOrganisations = new List<SponsorOrganisationDto>
                {
                    new()
                    {
                        IsActive = true,
                        CreatedDate = new DateTime(2024, 5, 1),
                        Users = new List<SponsorOrganisationUserDto>
                        {
                            new()
                            {
                                RtsId = "123",
                                UserId = userGuid,
                                Id = Guid.NewGuid()
                            }
                        }
                    }
                }
            }
        };

        var organisationResponse = new ServiceResponse<OrganisationDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new OrganisationDto { Id = rtsId, Name = orgName, CountryName = country }
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationByRtsId(rtsId))
            .ReturnsAsync(sponsorResponse);

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation(rtsId))
            .ReturnsAsync(organisationResponse);

        Mocker.GetMock<IUserManagementService>()
            .Setup(x => x.GetUsersByIds(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync(new ServiceResponse<UsersResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new UsersResponse
                {
                    TotalCount = 1,
                    Users = new List<User>
                    {
                        new(
                            userId,
                            "azure-ad-12345",
                            "Mr",
                            "Test",
                            "Test",
                            "test.test@example.com",
                            "Software Developer",
                            "Rsp Systems Ltd",
                            "+44 7700 900123",
                            "United Kingdom",
                            "Active",
                            DateTime.UtcNow,
                            DateTime.UtcNow.AddDays(-2),
                            DateTime.UtcNow)
                    }
                }
            });

        // Act
        var result = await Sut.ViewSponsorOrganisationUsers(rtsId, null, 1, 20, "Status");


        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<SponsorOrganisationListUsersModel>();

        model.SponsorOrganisation.RtsId.ShouldBe(rtsId);
        model.SponsorOrganisation.SponsorOrganisationName.ShouldBe(orgName);
        model.SponsorOrganisation.Countries.ShouldContain(country);
        model.SponsorOrganisation.IsActive.ShouldBeTrue();
        model.SponsorOrganisation.UpdatedDate.ShouldBe(
            sponsorResponse.Content.SponsorOrganisations.First().CreatedDate);

        model.Users.ShouldNotBeNull();
        model.Users.ShouldHaveSingleItem();
        model.Users.First().Id.ShouldBe(userId);

        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.GetSponsorOrganisationByRtsId(rtsId), Times.Once);
        Mocker.GetMock<IRtsService>()
            .Verify(s => s.GetOrganisation(rtsId), Times.Once);
    }
}