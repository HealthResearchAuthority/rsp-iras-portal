using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Domain.Identity;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.SponsorOrganisationsControllerTests;

public class ConfirmAddUpdateUserTests : TestServiceBase<SponsorOrganisationsController>
{
    private readonly DefaultHttpContext _http;

    public ConfirmAddUpdateUserTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

    [Fact]
    public async Task ConfirmAddUpdateUser_ShouldReturnView_WithMappedModel_WhenBothServicesSucceed()
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
        var result = await Sut.ConfirmAddUpdateUser(rtsId, userGuid);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<ConfirmAddUpdateSponsorOrganisationUserModel>();

        model.SponsorOrganisation.RtsId.ShouldBe(rtsId);
        model.SponsorOrganisation.SponsorOrganisationName.ShouldBe(orgName);
        model.SponsorOrganisation.Countries.ShouldContain(country);
        model.SponsorOrganisation.IsActive.ShouldBeTrue();
        model.SponsorOrganisation.UpdatedDate.ShouldBe(
            sponsorResponse.Content.SponsorOrganisations.First().CreatedDate);

        model.ShouldNotBeNull();

        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.GetSponsorOrganisationByRtsId(rtsId), Times.Once);
        Mocker.GetMock<IRtsService>()
            .Verify(s => s.GetOrganisation(rtsId), Times.Once);

        // Prove your users search was actually invoked:
        Mocker.GetMock<IUserManagementService>()
            .Verify(x => x.GetUser(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmAddUpdateUser_ShouldReturnServiceError_WhenPrimaryServiceFails()
    {
        // Arrange
        const string rtsId = "99999";
        var errorResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            StatusCode = HttpStatusCode.InternalServerError,
            Content = null
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationByRtsId(rtsId))
            .ReturnsAsync(errorResponse);

        // Act
        var result = await Sut.ConfirmAddUpdateUser(rtsId, Guid.NewGuid());

        // Assert
        var objectResult = result.ShouldBeOfType<StatusCodeResult>();
        objectResult.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);

        // Verify RTS never called
        Mocker.GetMock<IRtsService>()
            .Verify(s => s.GetOrganisation(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmAddUpdateUser_ShouldRedirectToIndex_WhenNoSponsorOrgOrSecondCallFails()
    {
        // Arrange
        const string rtsId = "88888";

        var sponsorResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new AllSponsorOrganisationsResponse
            {
                SponsorOrganisations = new List<SponsorOrganisationDto>
                {
                    new() { IsActive = true, CreatedDate = new DateTime(2024, 5, 1) }
                }
            }
        };

        var rtsFailure = new ServiceResponse<OrganisationDto>
        {
            StatusCode = HttpStatusCode.BadRequest,
            Content = null
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetSponsorOrganisationByRtsId(rtsId))
            .ReturnsAsync(sponsorResponse);

        Mocker.GetMock<IRtsService>()
            .Setup(s => s.GetOrganisation(rtsId))
            .ReturnsAsync(rtsFailure);

        // Act
        var result = await Sut.ConfirmAddUpdateUser(rtsId, Guid.NewGuid());

        // Assert
        var redirect = result.ShouldBeOfType<StatusCodeResult>();
        redirect.StatusCode.ShouldBe(400);
    }
}