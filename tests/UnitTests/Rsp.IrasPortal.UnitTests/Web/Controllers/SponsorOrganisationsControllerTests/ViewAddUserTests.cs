using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.Identity;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.SponsorOrganisationsControllerTests;

public class ViewAddUserTests : TestServiceBase<SponsorOrganisationsController>
{
    private readonly DefaultHttpContext _http;

    public ViewAddUserTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
        Sut.TempData = new TempDataDictionary(
            _http,
            Mocker.GetMock<ITempDataProvider>().Object);
    }

    [Fact]
    public async Task ViewAddUser_ShouldReturnView_WithMappedModel_WhenBothServicesSucceed()
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
                        CreatedDate = new DateTime(2024, 5, 1)
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

        // Act
        var result = await Sut.ViewAddUser(rtsId, "test");

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

        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.GetSponsorOrganisationByRtsId(rtsId), Times.Once);
        Mocker.GetMock<IRtsService>()
            .Verify(s => s.GetOrganisation(rtsId), Times.Once);

        // Prove your users search was actually invoked:
        Mocker.GetMock<IUserManagementService>()
            .Verify(x => x.SearchUsers(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>?>(),
                It.IsAny<int>(),
                It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task ViewAddUser_ShouldReturnView_WithMappedModel_WhenBothServicesSucceed_NoQuery()
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
                        CreatedDate = new DateTime(2024, 5, 1)
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

        // Act
        var result = await Sut.ViewAddUser(rtsId, null);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<SponsorOrganisationListUsersModel>();

        model.SponsorOrganisation.RtsId.ShouldBe(rtsId);
        model.SponsorOrganisation.SponsorOrganisationName.ShouldBe(orgName);
        model.SponsorOrganisation.Countries.ShouldContain(country);
        model.SponsorOrganisation.IsActive.ShouldBeTrue();
        model.SponsorOrganisation.UpdatedDate.ShouldBe(
            sponsorResponse.Content.SponsorOrganisations.First().CreatedDate);

        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.GetSponsorOrganisationByRtsId(rtsId), Times.Once);
        Mocker.GetMock<IRtsService>()
            .Verify(s => s.GetOrganisation(rtsId), Times.Once);
    }
}