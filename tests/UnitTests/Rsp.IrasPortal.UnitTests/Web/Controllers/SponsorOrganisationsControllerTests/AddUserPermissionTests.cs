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

public class AddUserPermissionTests : TestServiceBase<SponsorOrganisationsController>
{
    private readonly DefaultHttpContext _http;

    public AddUserPermissionTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
        Sut.TempData = new TempDataDictionary(
            _http,
            Mocker.GetMock<ITempDataProvider>().Object);
    }

    [Theory, AutoData]
    public void AddUserPermission_ShouldReturnView_WithModel_WhenStoredModelExists(SponsorOrganisationAddUserModel model)
    {
        SetupTempData(model);

        var result = Sut.AddUserPermission();

        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeOfType<SponsorOrganisationAddUserModel>();
    }

    [Fact]
    public void AddUserPermission_ShouldReturnModelStateError_WhenNoStoredModelExists()
    {
        var result = Sut.AddUserPermission();

        var viewResult = result.ShouldBeOfType<RedirectToActionResult>();
        Sut.ModelState.IsValid.ShouldBeFalse();
        Sut.ModelState.ErrorCount.ShouldBe(1);
    }

    [Theory, AutoData]
    public async Task SaveUserPermission_ShouldRedirectToFinalPage_WhenStoredModelExists(SponsorOrganisationAddUserModel model)
    {
        SetupTempData(model);

        var result = await Sut.SaveUserPermission(model);

        var viewResult = result.ShouldBeOfType<RedirectToActionResult>();
        viewResult.ActionName.ShouldBe("ViewSponsorOrganisationUser");
    }

    [Theory, AutoData]
    public async Task SaveUserPermission_ShouldReturnModelStateError_WhenNoStoredModelExists(SponsorOrganisationAddUserModel model)
    {
        SetupTempData(model);
        Sut.TempData.Remove(TempDataKeys.SponsorOrganisationUser);

        var result = await Sut.SaveUserPermission(model);

        var viewResult = result.ShouldBeOfType<RedirectToActionResult>();
        viewResult.ActionName.ShouldBe("Index");
        Sut.ModelState.IsValid.ShouldBeFalse();
        Sut.ModelState.ErrorCount.ShouldBe(1);
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