using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.MyTasklistControllerTests;

public class IndexTests : TestServiceBase<MyTasklistController>
{
    private readonly DefaultHttpContext _http;

    public IndexTests()
    {
        _http = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = _http
        };

        // TempData still available for other short-lived flags if controller uses them elsewhere
        Sut.TempData = new TempDataDictionary(_http, Mock.Of<ITempDataProvider>());
    }

    [Fact]
    public async Task Welcome_ReturnsViewResult_WithIndexViewName()
    {
        // Act
        var result = await Sut.Index(1, 20, "CreatedAt", "asc");

        // Assert
        result.ShouldBeOfType<ViewResult>();
    }

    [Theory, AutoData]
    public async Task Index_ViewModel_Test(GetModificationsResponse modificationResponse)
    {
        var serviceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationResponse
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModifications(It.IsAny<ModificationSearchRequest>(), 1, 20, "CreatedAt", "asc"))
            .ReturnsAsync(serviceResponse);

        var result = await Sut.Index(1, 20, "CreatedAt", "asc");

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<MyTasklistViewModel>();
        var modifications = model?.Modifications.ShouldBeOfType<List<ModificationsModel>>();
    }

    [Theory]
    [InlineData("{\"IrasId\":\"123456\"}", false)]
    [InlineData("{\"FromDay\":\"01\",\"FromMonth\":\"01\",\"FromYear\":\"2020\"}", false)]
    [InlineData("{\"ToDay\":\"31\",\"ToMonth\":\"12\",\"ToYear\":\"2025\"}", false)]
    [InlineData("{}", true)]
    public async Task Index_SearchModel_FromSession_SetsCorrectEmptySearchPerformed(string json, bool expectedEmptySearchPerformed)
    {
        _http.Session.SetString(SessionKeys.MyTasklist, json);

        var result = await Sut.Index(1, 20, "CreatedAt", "asc");

        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldNotBeNull();
        var viewModel = viewResult.Model.ShouldBeAssignableTo<MyTasklistViewModel>();
        viewModel.EmptySearchPerformed.ShouldBe(expectedEmptySearchPerformed);
    }

    [Theory, AutoData]
    public async Task Index_Calculates_FromDate_And_ToDate_Correctly_From_StringSubmissionFields(
        int fromDays, int toDays, GetModificationsResponse modificationResponse)
    {
        fromDays = Math.Clamp(fromDays % 100, 1, 99);
        toDays = Math.Clamp(toDays % 100, 1, 99);

        var model = new ApprovalsSearchModel
        {
            FromDaysSinceSubmission = fromDays.ToString(),
            ToDaysSinceSubmission = toDays.ToString(),
        };

        // Arrange
        SetUserRoles(Roles.StudyWideReviewer);

        var json = JsonSerializer.Serialize(model);
        _http.Session.SetString(SessionKeys.MyTasklist, json);

        var serviceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationResponse
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModifications(It.IsAny<ModificationSearchRequest>(), 1, 20, "CreatedAt", "asc"))
            .ReturnsAsync(serviceResponse);

        var result = await Sut.Index(1, 20, "CreatedAt", "asc");

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var modelResult = viewResult.Model.ShouldBeAssignableTo<MyTasklistViewModel>();
        var modifications = modelResult?.Modifications.ShouldBeOfType<List<ModificationsModel>>();
    }

    private void SetUserRoles(params string[] roles)
    {
        var claims = roles
            .Select(r => new Claim(ClaimTypes.Role, r))
            .ToList();

        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        _http.User = new ClaimsPrincipal(identity);
    }
}