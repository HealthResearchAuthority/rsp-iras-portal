using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.SponsorOrganisationsControllerTests;

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
    public void SaveUserPermission_ShouldRedirectToFinalPage_WhenStoredModelExists(SponsorOrganisationAddUserModel model)
    {
        SetupTempData(model);

        var result = Sut.SaveUserPermission(model);

        var viewResult = result.ShouldBeOfType<RedirectToActionResult>();
        viewResult.ActionName.ShouldBe("ViewSponsorOrganisationUser");
    }

    [Theory, AutoData]
    public void SaveUserPermission_ShouldReturnModelStateError_WhenNoStoredModelExists(SponsorOrganisationAddUserModel model)
    {
        SetupTempData(model);
        Sut.TempData.Remove(TempDataKeys.SponsorOrganisationUser);

        var result = Sut.SaveUserPermission(model);

        var viewResult = result.ShouldBeOfType<RedirectToActionResult>();
        viewResult.ActionName.ShouldBe("Index");
        Sut.ModelState.IsValid.ShouldBeFalse();
        Sut.ModelState.ErrorCount.ShouldBe(1);
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