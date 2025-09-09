using System.Net;
using System.Security.Claims;
using System.Text.Json;
using AutoFixture.Xunit2;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses; // adjust if your DTOs live elsewhere
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ModificationsTasklistControllerTests;

public class IndexTests : TestServiceBase<ModificationsTasklistController>
{
    private readonly DefaultHttpContext _http;

    public IndexTests()
    {
        _http = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };

        // Ensure a ClaimsPrincipal exists (controller uses User?.FindFirstValue("userId"))
        _http.User = new ClaimsPrincipal(new ClaimsIdentity());

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = _http
        };

        Sut.TempData = new TempDataDictionary(_http, Mock.Of<ITempDataProvider>());
    }

    [Fact]
    public async Task Welcome_ReturnsViewResult_WithIndexViewName()
    {
        // Act
        var result = await Sut.Index(1, 20, null, "CreatedAt", "asc");

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

        var result = await Sut.Index(1, 20, null, "CreatedAt", "asc");

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<ModificationsTasklistViewModel>();
        var modifications = model?.Modifications.ShouldBeOfType<List<TaskListModificationViewModel>>();
    }

    [Theory]
    [InlineData("{\"IrasId\":\"123456\"}", false)]
    [InlineData("{\"ChiefInvestigatorName\":\"Dr. Smith\"}", false)]
    [InlineData("{\"FromDay\":\"01\",\"FromMonth\":\"01\",\"FromYear\":\"2020\"}", false)]
    [InlineData("{\"ToDay\":\"31\",\"ToMonth\":\"12\",\"ToYear\":\"2025\"}", false)]
    [InlineData("{}", true)]
    public async Task Index_SearchModel_FromSession_SetsCorrectEmptySearchPerformed(string json,
        bool expectedEmptySearchPerformed)
    {
        _http.Session.SetString(SessionKeys.ModificationsTasklist, json);

        var result = await Sut.Index(1, 20, null, "CreatedAt", "asc");

        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldNotBeNull();
        var viewModel = viewResult.Model.ShouldBeAssignableTo<ModificationsTasklistViewModel>();
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

        var json = JsonSerializer.Serialize(model);
        _http.Session.SetString(SessionKeys.ModificationsTasklist, json);

        var serviceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationResponse
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModifications(It.IsAny<ModificationSearchRequest>(), 1, 20, "CreatedAt", "asc"))
            .ReturnsAsync(serviceResponse);

        var result = await Sut.Index(1, 20, null, "CreatedAt", "asc");

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var modelResult = viewResult.Model.ShouldBeAssignableTo<ModificationsTasklistViewModel>();
        var modifications = modelResult?.Modifications.ShouldBeOfType<List<TaskListModificationViewModel>>();
    }

    // -------------------------
    // New tests for the GUID/leadNation flow
    // -------------------------

    [Theory, AutoData]
    public async Task Index_WithValidUserId_QueriesReviewBodies_And_ReviewBodyById(
        GetModificationsResponse modificationResponse)
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetUserIdClaim(userId);

        // Your DTOs may differ – adjust as needed:
        // A list with one item that has an Id used to fetch the review body
        var reviewBodyId = Guid.NewGuid();

        var userBodies = new List<ReviewBodyUserDto> // <-- replace with your actual item type if different
        {
            new ReviewBodyUserDto { Id = reviewBodyId } // must have Id: Guid
        };

        var userBodiesResponse = new ServiceResponse<List<ReviewBodyUserDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = userBodies
        };

        var reviewBodyDetail = new ReviewBodyDto // <-- replace with your actual detail type if different
        {
            Countries = new List<string> { "Wales" } // must have Countries: IEnumerable<string>
        };

        var reviewBodyByIdResponse = new ServiceResponse<ReviewBodyDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBodyDetail
        };

        // Modifications service (existing)
        var modsServiceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationResponse
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModifications(It.IsAny<ModificationSearchRequest>(), 1, 20, "CreatedAt", "asc"))
            .ReturnsAsync(modsServiceResponse);

        // Review body service (new path)
        var rbSvc = Mocker.GetMock<IReviewBodyService>();

        rbSvc.Setup(s => s.GetUserReviewBodies(userId))
            .ReturnsAsync(userBodiesResponse);

        rbSvc.Setup(s => s.GetReviewBodyById(reviewBodyId))
            .ReturnsAsync(reviewBodyByIdResponse);

        // Act
        var result = await Sut.Index(1, 20, null, "CreatedAt", "asc");

        // Assert: page still returns a view
        result.ShouldBeOfType<ViewResult>();

        // And we exercised the new calls with the parsed Guid
        rbSvc.Verify(s => s.GetUserReviewBodies(userId), Times.Once);
        rbSvc.Verify(s => s.GetReviewBodyById(reviewBodyId), Times.Once);
    }

    [Theory, AutoData]
    public async Task Index_WithInvalidUserId_DoesNotCallReviewBodyService(
        GetModificationsResponse modificationResponse)
    {
        // Arrange: set a non-GUID userId claim (or remove the claim entirely)
        SetUserIdClaim("not-a-guid");

        var modsServiceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationResponse
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModifications(It.IsAny<ModificationSearchRequest>(), 1, 20, "CreatedAt", "asc"))
            .ReturnsAsync(modsServiceResponse);

        var rbSvc = Mocker.GetMock<IReviewBodyService>();

        // Act
        var result = await Sut.Index(1, 20, null, "CreatedAt", "asc");

        // Assert
        result.ShouldBeOfType<ViewResult>();
        rbSvc.Verify(s => s.GetUserReviewBodies(It.IsAny<Guid>()), Times.Never);
        rbSvc.Verify(s => s.GetReviewBodyById(It.IsAny<Guid>()), Times.Never);
    }

    // ---------------
    // helpers
    // ---------------
    private void SetUserIdClaim(Guid userId)
        => SetUserIdClaim(userId.ToString());

    private void SetUserIdClaim(string userIdValue)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("userId", userIdValue)
        }, authenticationType: "TestAuth");

        _http.User = new ClaimsPrincipal(identity);
    }
}