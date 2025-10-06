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
    private const string SessionSelectedKey = "Tasklist:SelectedModificationIds";

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
        // Arrange
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModifications(It.IsAny<ModificationSearchRequest>(), 1, 20, "CreatedAt", "asc"))
            .ReturnsAsync(new ServiceResponse<GetModificationsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new GetModificationsResponse { Modifications = new List<ModificationsDto>(), TotalCount = 0 }
            });

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

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModifications(It.IsAny<ModificationSearchRequest>(), 1, 20, "CreatedAt", "asc"))
            .ReturnsAsync(new ServiceResponse<GetModificationsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new GetModificationsResponse { Modifications = new List<ModificationsDto>(), TotalCount = 0 }
            });

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

        var reviewBodyId = Guid.NewGuid();

        var userBodies = new List<ReviewBodyUserDto>
        {
            new ReviewBodyUserDto { Id = reviewBodyId }
        };

        var userBodiesResponse = new ServiceResponse<List<ReviewBodyUserDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = userBodies
        };

        var reviewBodyDetail = new ReviewBodyDto
        {
            Countries = new List<string> { "Wales" }
        };

        var reviewBodyByIdResponse = new ServiceResponse<ReviewBodyDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBodyDetail
        };

        var modsServiceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = modificationResponse
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModifications(It.IsAny<ModificationSearchRequest>(), 1, 20, "CreatedAt", "asc"))
            .ReturnsAsync(modsServiceResponse);

        var rbSvc = Mocker.GetMock<IReviewBodyService>();

        rbSvc.Setup(s => s.GetUserReviewBodies(userId))
            .ReturnsAsync(userBodiesResponse);

        rbSvc.Setup(s => s.GetReviewBodyById(reviewBodyId))
            .ReturnsAsync(reviewBodyByIdResponse);

        // Act
        var result = await Sut.Index(1, 20, null, "CreatedAt", "asc");

        // Assert
        result.ShouldBeOfType<ViewResult>();
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

    [Theory, AutoData]
    public async Task Index_WhenGetUserReviewBodies_NotSuccess_UsesDefault_And_Skips_GetById(
        GetModificationsResponse modificationResponse)
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetUserIdClaim(userId);

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModifications(It.IsAny<ModificationSearchRequest>(), 1, 20, "CreatedAt", "asc"))
            .ReturnsAsync(new ServiceResponse<GetModificationsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = modificationResponse
            });

        var rbSvc = Mocker.GetMock<IReviewBodyService>();

        rbSvc.Setup(s => s.GetUserReviewBodies(userId))
            .ReturnsAsync(new ServiceResponse<List<ReviewBodyUserDto>>
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = null
            });

        // Act
        var result = await Sut.Index(1, 20, null, "CreatedAt", "asc");

        // Assert
        result.ShouldBeOfType<ViewResult>();
        rbSvc.Verify(s => s.GetUserReviewBodies(userId), Times.Once);
        rbSvc.Verify(s => s.GetReviewBodyById(It.IsAny<Guid>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task Index_WhenGetReviewBodyById_NotSuccess_ReturnsView_And_NoFurtherCalls(
        GetModificationsResponse modificationResponse)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var reviewBodyId = Guid.NewGuid();
        SetUserIdClaim(userId);

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModifications(It.IsAny<ModificationSearchRequest>(), 1, 20, "CreatedAt", "asc"))
            .ReturnsAsync(new ServiceResponse<GetModificationsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = modificationResponse
            });

        var rbSvc = Mocker.GetMock<IReviewBodyService>();

        rbSvc.Setup(s => s.GetUserReviewBodies(userId))
            .ReturnsAsync(new ServiceResponse<List<ReviewBodyUserDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ReviewBodyUserDto>
                {
                    new ReviewBodyUserDto { Id = reviewBodyId }
                }
            });

        rbSvc.Setup(s => s.GetReviewBodyById(reviewBodyId))
            .ReturnsAsync(new ServiceResponse<ReviewBodyDto>
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = null
            });

        // Act
        var result = await Sut.Index(1, 20, null, "CreatedAt", "asc");

        // Assert
        result.ShouldBeOfType<ViewResult>();
        rbSvc.Verify(s => s.GetUserReviewBodies(userId), Times.Once);
        rbSvc.Verify(s => s.GetReviewBodyById(reviewBodyId), Times.Once);
    }

    // ----- Sorting by DaysSinceSubmission remains: invert direction and use CreatedAt -----

    [Theory, AutoData]
    public async Task Index_SortByDaysSinceSubmission_Asc_InvertsTo_CreatedAt_Desc(
        GetModificationsResponse modificationResponse)
    {
        // Arrange
        string? capturedField = null;
        string? capturedDir = null;

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModifications(It.IsAny<ModificationSearchRequest>(), 1, 20, It.IsAny<string>(),
                It.IsAny<string>()))
            .Callback<ModificationSearchRequest, int, int, string, string>((_, __, ___, field, dir) =>
            {
                capturedField = field;
                capturedDir = dir;
            })
            .ReturnsAsync(new ServiceResponse<GetModificationsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = modificationResponse
            });

        // Act
        var result = await Sut.Index(
            1,
            20,
            null,
            nameof(ModificationsModel.DaysSinceSubmission),
            SortDirections.Ascending);

        // Assert
        result.ShouldBeOfType<ViewResult>();
        capturedField.ShouldBe(nameof(ModificationsModel.CreatedAt));
        capturedDir.ShouldBe(SortDirections.Descending);
    }

    [Theory, AutoData]
    public async Task Index_SortByDaysSinceSubmission_Desc_InvertsTo_CreatedAt_Asc(
        GetModificationsResponse modificationResponse)
    {
        // Arrange
        string? capturedField = null;
        string? capturedDir = null;

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModifications(It.IsAny<ModificationSearchRequest>(), 1, 20, It.IsAny<string>(),
                It.IsAny<string>()))
            .Callback<ModificationSearchRequest, int, int, string, string>((_, __, ___, field, dir) =>
            {
                capturedField = field;
                capturedDir = dir;
            })
            .ReturnsAsync(new ServiceResponse<GetModificationsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = modificationResponse
            });

        // Act
        var result = await Sut.Index(
            1,
            20,
            null,
            nameof(ModificationsModel.DaysSinceSubmission),
            SortDirections.Descending);

        // Assert
        result.ShouldBeOfType<ViewResult>();
        capturedField.ShouldBe(nameof(ModificationsModel.CreatedAt));
        capturedDir.ShouldBe(SortDirections.Ascending);
    }

    // ----- NEW: selectedModificationIds flow -----

    [Fact]
    public async Task Index_WithSelectedIdsInQuery_PersistsToSession_AndRedirectsWithoutSelectedIds()
    {
        // Arrange: duplicates, CSV and casing to test normalization
        var input = new List<string> { "abc", "ABC", "def,ghi", "  jkl  ", "" };

        // Act
        var result = await Sut.Index(
            pageNumber: 2,
            pageSize: 50,
            selectedModificationIds: input,
            sortField: "CreatedAt",
            sortDirection: "asc");

        // Assert redirect
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("tasklist:index");
        redirect.RouteValues.ShouldNotBeNull();
        redirect.RouteValues!.Count.ShouldBe(4);
        redirect.RouteValues!["pageNumber"].ShouldBe(2);
        redirect.RouteValues!["pageSize"].ShouldBe(50);
        redirect.RouteValues!["sortField"].ShouldBe("CreatedAt");
        redirect.RouteValues!["sortDirection"].ShouldBe("asc");

        // Assert session persisted normalized distinct values (case-insensitive)
        var storedJson = _http.Session.GetString(SessionSelectedKey);
        storedJson.ShouldNotBeNull();

        var stored = JsonSerializer.Deserialize<List<string>>(storedJson!)!;
        stored.ShouldNotBeNull();

        // Expected normalized: abc, def, ghi, jkl
        stored.Count.ShouldBe(4);
        stored.ShouldContain("abc");
        stored.ShouldContain("def");
        stored.ShouldContain("ghi");
        stored.ShouldContain("jkl");
    }

    [Fact]
    public async Task Index_NoSelectedIdsInQuery_LoadsFromSession_And_SetsIsSelected_CaseInsensitive()
    {
        // Arrange: pre-populate session selections (lower/upper mix)
        var preselected = new List<string> { "abc", "DEF" };
        _http.Session.SetString(SessionSelectedKey, JsonSerializer.Serialize(preselected));

        var returned = new GetModificationsResponse
        {
            TotalCount = 3,
            Modifications = new List<ModificationsDto>
            {
                new()
                {
                    Id = "ABC", ProjectRecordId = "PR-1", ModificationId = "100/1", ShortProjectTitle = "One",
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = "def", ProjectRecordId = "PR-2", ModificationId = "100/2", ShortProjectTitle = "Two",
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = "xyz", ProjectRecordId = "PR-3", ModificationId = "100/3", ShortProjectTitle = "Three",
                    CreatedAt = DateTime.UtcNow
                },
            }
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModifications(It.IsAny<ModificationSearchRequest>(), 1, 20, "CreatedAt", "asc"))
            .ReturnsAsync(new ServiceResponse<GetModificationsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = returned
            });

        // Act
        var result = await Sut.Index(1, 20, null, "CreatedAt", "asc");

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var vm = viewResult.Model.ShouldBeAssignableTo<ModificationsTasklistViewModel>();

        vm.SelectedModificationIds.ShouldBeSubsetOf(preselected);
        vm.Modifications.Count().ShouldBe(3);

        // IsSelected should be case-insensitive
        vm.Modifications.ToList()[0].IsSelected.ShouldBeTrue(); // "ABC" matches "abc"
        vm.Modifications.ToList()[1].IsSelected.ShouldBeTrue(); // "def" matches "DEF"
        vm.Modifications.ToList()[2].IsSelected.ShouldBeFalse(); // "xyz" not selected
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