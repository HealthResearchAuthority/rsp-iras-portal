using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rsp.IrasPortal.Web.Features.MemberManagement.Models;
using Rsp.IrasPortal.Web.Features.MemberManagement.ResearchEthicsCommittees.Models;
using Rsp.IrasPortal.Web.Helpers;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.Identity;
using Rsp.Portal.Web.Features.MemberManagement.ResearchEthicsCommittees.Controllers;
using Rsp.Portal.Web.Features.MemberManagement.ResearchEthicsCommittees.Models;
using Xunit;
using Claim = System.Security.Claims.Claim;

namespace Rsp.Portal.UnitTests.Web.Features.MemberManagement.ResearchEthicsCommittees;

public class ResearchEthicsCommitteesControllerTests
    : TestServiceBase<ResearchEthicsCommitteesController>
{
    private readonly DefaultHttpContext _http;
    private readonly Guid LoggedInUserId = Guid.NewGuid();

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

        SetupDefaultUser();
        SetupDefaultReviewBodies();
    }

    private void SetupDefaultUser()
    {
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
                        DateTime.Now)
                }
            });
    }

    private void SetupDefaultReviewBodies()
    {
        Mocker.GetMock<IReviewBodyService>()
            .Setup(x => x.GetAllReviewBodies(
                It.IsAny<ReviewBodySearchRequest>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<AllReviewBodiesResponse>
            {
                Content = new AllReviewBodiesResponse
                {
                    TotalCount = 0,
                    ReviewBodies = new List<ReviewBodyDto>()
                }
            });
    }

    private void SetUser(Guid userId, params string[] roles)
    {
        _http.User = CreateUserPrincipal(userId, roles);
    }

    private void SetUserResponse(Guid userId)
    {
        var userResponse = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = CreateUserResponse(
                userId,
                email: "some@email.com",
                country: "United Kingdom",
                status: IrasUserStatus.Active)
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(userId.ToString(), null, null))
            .ReturnsAsync(userResponse);
    }

    private static ClaimsPrincipal CreateUserPrincipal(Guid userId, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(CustomClaimTypes.UserId, userId.ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"));
    }

    private static UserResponse CreateUserResponse(
        Guid userId,
        string country = "United Kingdom",
        string status = IrasUserStatus.Active,
        string email = "test@test.com")
    {
        return new UserResponse
        {
            User = new User(
                userId.ToString(),
                "aad",
                "Mr",
                "Test",
                "User",
                email,
                "Dev",
                "Org",
                "123",
                country,
                status,
                DateTime.UtcNow,
                DateTime.UtcNow,
                DateTime.UtcNow)
        };
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
            Search = new MemberManagementResearchEthicsCommitteesSearchModel()
            {
                
            }
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

    [Theory]
    [AutoData]
    public async Task ResearchEthicsCommitteesProfile_ShouldReturnView_WithModel_WhenUserHasAccess(
        Guid recId,
        ReviewBodyDto reviewBody,
        AddRecMemberViewModel viewModel,
        Guid userId)
    {
        // Arrange
        SetUser(LoggedInUserId);
        SetUserResponse(LoggedInUserId);

        var userEmail = "user@example.com";
        var targetUser = CreateUserResponse(
            userId,
            email: userEmail,
            country: "United Kingdom",
            status: IrasUserStatus.Active);

        viewModel.Email = userEmail;
        viewModel.RecId = recId;
        viewModel.RecName = reviewBody.RegulatoryBodyName;

        reviewBody.Id = recId;
        reviewBody.Countries = new List<string> { "United Kingdom" };
        reviewBody.Users = new List<ReviewBodyUserDto>
        {
            new()
            {
                UserId = userId,
                Id = recId,
                Email = userEmail
            }
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(recId))
            .ReturnsAsync(new ServiceResponse<ReviewBodyDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = reviewBody
            });

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(userId.ToString(), null, null))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = targetUser
            });

        // Act
        var result = await Sut.ResearchEthicsCommitteesProfile(recId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeOfType<MemberManagementResearchEthicsCommitteesProfileViewModel>();
    }

    [Fact]
    public async Task UserHasAccess_ShouldReturnTrue_WhenUserIsSystemAdministrator()
    {
        // Arrange
        var rec = new ReviewBodyDto
        {
            Countries = new List<string> { "United Kingdom" }
        };

        var user = CreateUserPrincipal(Guid.NewGuid(), Roles.SystemAdministrator);
        var userServiceMock = new Mock<IUserManagementService>();

        // Act
        var result = await MemberManagementHelper.UserHasAccess(rec, user, userServiceMock.Object);

        // Assert
        result.ShouldBeTrue();

        userServiceMock.Verify(
            x => x.GetUser(It.IsAny<string>(), null, null),
            Times.Never);
    }

    [Fact]
    public async Task UserHasAccess_ShouldReturnFalse_WhenUserIsNotActive()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var rec = new ReviewBodyDto
        {
            Countries = new List<string> { "United Kingdom" }
        };

        var user = CreateUserPrincipal(userId);

        var userServiceMock = new Mock<IUserManagementService>();
        userServiceMock
            .Setup(s => s.GetUser(userId.ToString(), null, null))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                Content = CreateUserResponse(
                    userId,
                    country: "United Kingdom",
                    status: IrasUserStatus.Disabled)
            });

        // Act
        var result = await MemberManagementHelper.UserHasAccess(rec, user, userServiceMock.Object);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task UserHasAccess_ShouldReturnFalse_WhenUserDoesNotBelongToRecCountry()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var rec = new ReviewBodyDto
        {
            Countries = new List<string> { "Germany" }
        };

        var user = CreateUserPrincipal(userId);

        var userServiceMock = new Mock<IUserManagementService>();
        userServiceMock
            .Setup(s => s.GetUser(userId.ToString(), null, null))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                Content = CreateUserResponse(
                    userId,
                    country: "United Kingdom",
                    status: IrasUserStatus.Active)
            });

        // Act
        var result = await MemberManagementHelper.UserHasAccess(rec, user, userServiceMock.Object);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task UserHasAccess_ShouldReturnFalse_WhenUserCountryDoesNotMatchRecCountries()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var rec = new ReviewBodyDto
        {
            Countries = new List<string> { "Germany" }
        };

        var user = CreateUserPrincipal(userId);

        var userServiceMock = new Mock<IUserManagementService>();
        userServiceMock
            .Setup(s => s.GetUser(userId.ToString(), null, null))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                Content = CreateUserResponse(
                    userId,
                    country: "United Kingdom",
                    status: IrasUserStatus.Active)
            });

        // Act
        var result = await MemberManagementHelper.UserHasAccess(rec, user, userServiceMock.Object);

        // Assert
        result.ShouldBeFalse();
    }
}