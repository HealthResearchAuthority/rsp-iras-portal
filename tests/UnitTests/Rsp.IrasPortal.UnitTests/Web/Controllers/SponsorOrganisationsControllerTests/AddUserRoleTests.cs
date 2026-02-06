using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.SponsorOrganisationsControllerTests;

public class AddUserRoleTests : TestServiceBase<SponsorOrganisationsController>
{
    private readonly DefaultHttpContext _http;

    public AddUserRoleTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
        Sut.TempData = new TempDataDictionary(
            _http,
            Mocker.GetMock<ITempDataProvider>().Object);
    }

    [Theory, AutoData]
    public void AddUserRole_ShouldReturnView_WithMappedModel_WhenNoStoredModelExists(string rtsId, Guid userId)
    {
        var result = Sut.AddUserRole(rtsId, userId);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeOfType<SponsorOrganisationAddUserModel>()
            .RtsId.ShouldBe(rtsId);
    }

    [Theory, AutoData]
    public void SaveUserRole_ShouldReturnModelStateError_WhenStoredModelIsNull(SponsorOrganisationAddUserModel model)
    {
        // Arrange
        SetupTempData(model);
        Sut.TempData.Remove(TempDataKeys.SponsorOrganisationUser);

        // Act
        var result = Sut.SaveUserRole(model);

        // Assert
        result.ShouldBeOfType<RedirectToActionResult>();
        Sut.ModelState.IsValid.ShouldBeFalse();
        Sut.ModelState.ErrorCount.ShouldBe(1);
    }

    [Theory, AutoData]
    public void SaveUserRole_ShouldReturnModelStateError_WhenRoleIsNull(SponsorOrganisationAddUserModel model)
    {
        // Arrange
        model.SponsorRole = null;

        SetupTempData(model);

        // Act
        var result = Sut.SaveUserRole(model);

        // Assert
        result.ShouldBeOfType<ViewResult>();
        Sut.ModelState.IsValid.ShouldBeFalse();
        Sut.ModelState.ErrorCount.ShouldBe(1);
    }

    [Theory, AutoData]
    public void SaveUserRole_ShouldRedirectToFinalPage_WhenRoleIsOrgAdmin(SponsorOrganisationAddUserModel model)
    {
        // Arrange
        model.SponsorRole = Roles.OrganisationAdministrator;

        SetupTempData(model);

        // Act
        var result = Sut.SaveUserRole(model);

        // Assert
        var viewResult = result.ShouldBeOfType<RedirectToActionResult>();
        Sut.ModelState.IsValid.ShouldBeTrue();
        viewResult.ActionName.ShouldBe("ViewSponsorOrganisationUser");
    }

    [Theory, AutoData]
    public void SaveUserRole_ShouldRedirectToAuthoriserPage_WhenRoleIsSponsor(SponsorOrganisationAddUserModel model)
    {
        // Arrange
        model.SponsorRole = Roles.Sponsor;

        SetupTempData(model);

        // Act
        var result = Sut.SaveUserRole(model);

        // Assert
        var viewResult = result.ShouldBeOfType<RedirectToActionResult>();
        Sut.ModelState.IsValid.ShouldBeTrue();
        viewResult.ActionName.ShouldBe("AddUserPermission");
    }

    private void SetupTempData(SponsorOrganisationAddUserModel model)
    {
        var ctx = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = ctx };
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.SponsorOrganisationUser] = JsonSerializer.Serialize(model)
        };
    }
}