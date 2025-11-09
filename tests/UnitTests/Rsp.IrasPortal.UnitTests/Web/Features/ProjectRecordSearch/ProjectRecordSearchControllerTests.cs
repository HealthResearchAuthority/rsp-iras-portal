using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Approvals.ProjectRecord.Controllers;
using Rsp.IrasPortal.Web.Features.Approvals.ProjectRecord.Models;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.ProjectRecordSearch;

public class ProjectRecordSearchControllerTests : TestServiceBase<ProjectRecordSearchController>
{
    private readonly Mock<IApplicationsService> _applicationService;
    private readonly Mock<IRtsService> _rtsService;

    private readonly DefaultHttpContext _http;

    public ProjectRecordSearchControllerTests()
    {
        _applicationService = Mocker.GetMock<IApplicationsService>();
        _rtsService = Mocker.GetMock<IRtsService>();

        _http = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = _http
        };
    }

    [Fact]
    public async Task Search_ShouldReturnDefaultView_WhenNoSessionExists()
    {
        // Act
        var result = await Sut.Index();

        // Assert
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ProjectRecordSearchViewModel>(view.Model);

        Assert.Empty(model.Applications);
        Assert.False(model.EmptySearchPerformed);
    }

    [Fact]
    public async Task Search_ShouldReturnEmptySearchPerformed_WhenFiltersAreEmpty()
    {
        // Arrange
        var searchModel = new ApprovalsSearchModel();
        _http.Session.SetString(SessionKeys.ProjectRecordSearch, JsonSerializer.Serialize(searchModel));

        // Act
        var result = await Sut.Index();

        // Assert
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ProjectRecordSearchViewModel>(view.Model);

        Assert.True(model.EmptySearchPerformed);
        Assert.Empty(model.Applications);
    }

    [Theory, AutoData]
    public async Task Search_ShouldReturnModifications_WhenSearchModelIsValid(PaginatedResponse<CompleteProjectRecordResponse> mockResponse)
    {
        // Arrange
        var searchModel = new ApprovalsSearchModel { ShortProjectTitle = "TestTitle" };
        _http.Session.SetString(SessionKeys.ProjectRecordSearch, JsonSerializer.Serialize(searchModel));

        var serviceResponse = new ServiceResponse<PaginatedResponse<CompleteProjectRecordResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = mockResponse
        };

        _applicationService
            .Setup(s => s.GetPaginatedApplications(It.IsAny<ProjectRecordSearchRequest>(), 1, 20, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.Index();

        // Assert
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ProjectRecordSearchViewModel>(view.Model);
        Assert.NotEmpty(model.Applications);
        Assert.Equal(mockResponse.Items.Count(), model.Applications.Count());
    }

    [Fact]
    public async Task ApplyFilters_ShouldRedirectToSearch_WhenModelIsValid()
    {
        // Arrange
        var searchModel = new ApprovalsSearchModel { ChiefInvestigatorName = "Dr. Valid" };
        var viewModel = new ProjectRecordSearchViewModel { Search = searchModel };

        var httpContext = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = Sut.ApplyFilters(viewModel);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("Index");

        var storedJson = httpContext.Session.GetString(SessionKeys.ProjectRecordSearch);
        storedJson.ShouldNotBeNullOrWhiteSpace();

        var storedModel = JsonSerializer.Deserialize<ApprovalsSearchModel>(storedJson!);
        storedModel!.ChiefInvestigatorName.ShouldBe("Dr. Valid");
    }

    [Fact]
    public void ClearFilters_ShouldRedirectToSearch()
    {
        // No session set-up needed; controller should still redirect.
        var result = Sut.ClearFilters();

        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(Sut.Index));
    }

    [Fact]
    public void ClearFilters_ShouldRetainOnlyIrasIdAndRedirect()
    {
        // Arrange
        var originalSearch = new ApprovalsSearchModel
        {
            IrasId = "IRAS123",
            ShortProjectTitle = "TestOrg"
        };

        _http.Session.SetString(SessionKeys.ProjectRecordSearch, JsonSerializer.Serialize(originalSearch));

        // Act
        var result = Sut.ClearFilters();

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(Sut.Index));

        var updatedJson = _http.Session.GetString(SessionKeys.ProjectRecordSearch);
        updatedJson.ShouldNotBeNull();

        var updatedSearch = JsonSerializer.Deserialize<ApprovalsSearchModel>(updatedJson!);
        updatedSearch.ShouldNotBeNull();
        updatedSearch!.IrasId.ShouldBe("IRAS123");
        updatedSearch.ShortProjectTitle.ShouldBeNull(); // all other filters cleared
    }

    [Fact]
    public void RemoveFilter_ProjectTitle_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel { ShortProjectTitle = "Cancer Study" };
        SetSessionModel(model);

        var result = Sut.RemoveFilter("shortprojecttitle", null);

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Index");

        var updated = GetSessionModel();
        updated.ShortProjectTitle.ShouldBeNull();
    }

    private void SetSessionModel(ApprovalsSearchModel model)
    {
        _http.Session.SetString(SessionKeys.ProjectRecordSearch, JsonSerializer.Serialize(model));
    }

    private ApprovalsSearchModel GetSessionModel()
    {
        var json = _http.Session.GetString(SessionKeys.ProjectRecordSearch);
        json.ShouldNotBeNullOrWhiteSpace();
        return JsonSerializer.Deserialize<ApprovalsSearchModel>(json!)!;
    }
}