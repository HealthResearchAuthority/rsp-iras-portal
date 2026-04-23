using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.Identity;
using Rsp.Portal.Web.Features.MemberManagement.ResearchEthicsCommittees.Controllers;
using Rsp.Portal.Web.Features.MemberManagement.ResearchEthicsCommittees.Models;

namespace Rsp.Portal.UnitTests.Web.Features.MemberManagement.ResearchEthicsCommittees;

public class ResearchEthicsCommitteesControllerTests : TestServiceBase<ResearchEthicsCommitteesController>
{
    private readonly DefaultHttpContext _http;

    public ResearchEthicsCommitteesControllerTests()
    {
        _http = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };

        _http.Items[ContextItemKeys.UserId] = "user-123";

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = _http
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(x => x.GetUser("user-123", null, null))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                Content = new UserResponse
                {
                    User = new User(
                        "user-123",
                        null,
                        null,
                        "Test",
                        "User",
                        "test.user@email.com",
                        null,
                        null,
                        "",
                        "England,Scotland",
                        "Active",
                        DateTime.Now,
                        DateTime.Now,
                        DateTime.Now
                        )
                }
            });

        Mocker.GetMock<IReviewBodyService>()
            .Setup(x => x.GetAllReviewBodies(
                It.IsAny<ReviewBodySearchRequest>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<AllReviewBodiesResponse>
            {
                Content = new AllReviewBodiesResponse()
                {
                    TotalCount = 0,
                    ReviewBodies = new List<ReviewBodyDto>()
                }
            });
    }

    [Fact]
    public async Task ResearchEthicsCommittees_ShouldReturnView_WithDefaultModel()
    {
        // Act
        var result = await Sut.ResearchEthicsCommittees();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<MemberManagementResearchEthicsCommitteesViewModel>();

        model.ShouldNotBeNull();
        model.Search.ShouldNotBeNull();

        var sessionValue = _http.Session.GetString(SessionKeys.MemberManagementResearchEthicsCommitteesSearch);
        sessionValue.ShouldNotBeNull();
    }

    [Theory]
    [AutoData]
    public async Task ApplyFilters_ShouldReturnView_AndStoreSearchModelInSession(
        MemberManagementResearchEthicsCommitteesViewModel model,
        string sortField,
        string sortDirection)
    {
        // Arrange
        model.Search ??= new MemberManagementResearchEthicsCommitteesSearchModel();

        _http.Request.Method = HttpMethods.Post;

        // Act
        var result = await Sut.ApplyFilters(model, sortField, sortDirection);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var viewModel = viewResult.Model.ShouldBeOfType<MemberManagementResearchEthicsCommitteesViewModel>();

        viewModel.Search.ShouldBeEquivalentTo(model.Search);
        viewModel.Pagination.ShouldNotBeNull();
        viewModel.Pagination.RouteName.ShouldBe("mm:researchethicscommittees");
        viewModel.Pagination.SortField.ShouldBe(sortField);
        viewModel.Pagination.SortDirection.ShouldBe(sortDirection);

        var sessionValue = _http.Session.GetString(SessionKeys.MemberManagementResearchEthicsCommitteesSearch);
        sessionValue.ShouldNotBeNull();

        var storedModel =
            JsonSerializer.Deserialize<MemberManagementResearchEthicsCommitteesSearchModel>(sessionValue!);
        storedModel.ShouldNotBeNull();
        storedModel.ShouldBeEquivalentTo(model.Search);
    }

    [Fact]
    public async Task ApplyFilters_ShouldStoreEmptySearchModelInSession_WhenSearchIsNull()
    {
        // Arrange
        var model = new MemberManagementResearchEthicsCommitteesViewModel
        {
            Search = null
        };

        _http.Request.Method = HttpMethods.Post;

        // Act
        var result = await Sut.ApplyFilters(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var viewModel = viewResult.Model.ShouldBeOfType<MemberManagementResearchEthicsCommitteesViewModel>();

        viewModel.Search.ShouldNotBeNull();
        viewModel.Pagination.ShouldNotBeNull();
        viewModel.Pagination.RouteName.ShouldBe("mm:researchethicscommittees");
        viewModel.Pagination.SortField.ShouldBe(nameof(ReviewBodyDto.RegulatoryBodyName));
        viewModel.Pagination.SortDirection.ShouldBe(SortDirections.Ascending);

        var sessionValue = _http.Session.GetString(SessionKeys.MemberManagementResearchEthicsCommitteesSearch);
        sessionValue.ShouldNotBeNull();

        var storedModel =
            JsonSerializer.Deserialize<MemberManagementResearchEthicsCommitteesSearchModel>(sessionValue!);
        storedModel.ShouldNotBeNull();
        storedModel.ShouldBeOfType<MemberManagementResearchEthicsCommitteesSearchModel>();
    }

    [Fact]
    public async Task ResearchEthicsCommittees_ShouldRestoreSearchFromSession_OnGet()
    {
        // Arrange
        var storedSearch = new MemberManagementResearchEthicsCommitteesSearchModel
        {
            SearchTerm = "test search"
        };

        _http.Request.Method = HttpMethods.Get;
        _http.Session.SetString(
            SessionKeys.MemberManagementResearchEthicsCommitteesSearch,
            JsonSerializer.Serialize(storedSearch));

        // Act
        var result = await Sut.ResearchEthicsCommittees();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<MemberManagementResearchEthicsCommitteesViewModel>();

        model.Search.ShouldNotBeNull();
        model.Search.SearchTerm.ShouldBe("test search");
    }

    [Fact]
    public async Task ApplyFilters_ShouldRestoreSearchFromSession_OnGet()
    {
        // Arrange
        var storedSearch = new MemberManagementResearchEthicsCommitteesSearchModel
        {
            SearchTerm = "saved search"
        };

        _http.Request.Method = HttpMethods.Get;
        _http.Session.SetString(
            SessionKeys.MemberManagementResearchEthicsCommitteesSearch,
            JsonSerializer.Serialize(storedSearch));

        var model = new MemberManagementResearchEthicsCommitteesViewModel();

        // Act
        var result = await Sut.ApplyFilters(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var viewModel = viewResult.Model.ShouldBeOfType<MemberManagementResearchEthicsCommitteesViewModel>();

        viewModel.Search.ShouldNotBeNull();
        viewModel.Search.SearchTerm.ShouldBe("saved search");
    }
}