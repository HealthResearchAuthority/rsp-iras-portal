using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.Identity;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.SponsorOrganisationsControllerTests;

public class AddUserRoleTests : TestServiceBase<SponsorOrganisationsController>
{
    [Theory, AutoData]
    public async Task AddUserRole_ShouldReturnView_WithMappedModel_WhenNoStoredModelExists(Guid userId, SponsorOrganisationAddUserModel model)
    {
        // Arrange
        SetupTempData(model);

        var result = await Sut.AddUserRole(model.RtsId, userId);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeOfType<SponsorOrganisationAddUserModel>()
            .RtsId.ShouldBe(model.RtsId);
    }

    [Theory, AutoData]
    public async Task SaveUserRole_ShouldReturnModelStateError_WhenStoredModelIsNull(SponsorOrganisationAddUserModel model)
    {
        // Arrange
        SetupTempData(model);
        Sut.TempData.Remove(TempDataKeys.SponsorOrganisationUser);

        // Act
        var result = await Sut.SaveUserRole(model);

        // Assert
        result.ShouldBeOfType<RedirectToActionResult>();
        Sut.ModelState.IsValid.ShouldBeFalse();
        Sut.ModelState.ErrorCount.ShouldBe(1);
    }

    [Theory, AutoData]
    public async Task SaveUserRole_ShouldReturnModelStateError_WhenRoleIsNull(SponsorOrganisationAddUserModel model)
    {
        // Arrange
        model.SponsorRole = null;

        SetupTempData(model);

        // Act
        var result = await Sut.SaveUserRole(model);

        // Assert
        result.ShouldBeOfType<ViewResult>();
        Sut.ModelState.IsValid.ShouldBeFalse();
        Sut.ModelState.ErrorCount.ShouldBe(1);
    }

    [Theory, AutoData]
    public async Task SaveUserRole_ShouldRedirectToFinalPage_WhenRoleIsOrgAdmin(SponsorOrganisationAddUserModel model)
    {
        // Arrange
        model.SponsorRole = Roles.OrganisationAdministrator;

        SetupTempData(model);

        // Act
        var result = await Sut.SaveUserRole(model);

        // Assert
        var viewResult = result.ShouldBeOfType<RedirectToActionResult>();
        Sut.ModelState.IsValid.ShouldBeTrue();
        viewResult.ActionName.ShouldBe("ViewSponsorOrganisationUser");
    }

    [Theory, AutoData]
    public async Task SaveUserRole_ShouldRedirectToAuthoriserPage_WhenRoleIsSponsor(SponsorOrganisationAddUserModel model)
    {
        // Arrange
        model.SponsorRole = Roles.Sponsor;

        SetupTempData(model);

        // Act
        var result = await Sut.SaveUserRole(model);

        // Assert
        var viewResult = result.ShouldBeOfType<RedirectToActionResult>();
        Sut.ModelState.IsValid.ShouldBeTrue();
        viewResult.ActionName.ShouldBe("AddUserPermission");
    }

    private void SetupTempData(SponsorOrganisationAddUserModel model)
    {
        // Arrange
        const string rtsId = "87765";
        const string orgName = "Acme Research Ltd";
        const string country = "England";

        model.RtsId = rtsId;

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

        var ctx = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = ctx };
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.SponsorOrganisationUser] = JsonSerializer.Serialize(model)
        };
    }
}