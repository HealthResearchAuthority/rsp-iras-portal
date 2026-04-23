using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Features.MemberManagement.Models;
using Rsp.IrasPortal.Web.Features.MemberManagement.ResearchEthicsCommittees.Models;
using Rsp.IrasPortal.Web.Helpers;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.Identity;
using Rsp.Portal.Web.Features.MemberManagement.ResearchEthicsCommittees.Controllers;
using Rsp.Portal.Web.Features.MemberManagement.ResearchEthicsCommittees.Models;
using Claim = System.Security.Claims.Claim;

namespace Rsp.Portal.UnitTests.Web.Features.MemberManagement.ResearchEthicsCommittees;

public class ResearchEthicsCommitteesControllerTests : TestServiceBase<ResearchEthicsCommitteesController>
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

    private void SetUser(Guid userId)
    {
        _http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(CustomClaimTypes.UserId, userId.ToString())
        }));
    }

    private void SetUserResponse(Guid loggedInUser)
    {
        var user = new UserResponse
        {
            User = new User(loggedInUser.ToString(),
                            "azure-ad-12345",
                            "Mr",
                            "Test",
                            "Test",
                            "some@email.com",
                            "Software Developer",
                            "orgName", // IMPORTANT: match org if your action filters by org
                            "+44 7700 900123",
                            "United Kingdom",
                            IrasUserStatus.Active,
                            DateTime.UtcNow,
                            DateTime.UtcNow.AddDays(-2),
                            DateTime.UtcNow)
        };

        var userResponse = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = user
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(loggedInUser.ToString(), null, null))
            .ReturnsAsync(userResponse);
    }

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

    [Theory, AutoData]
    public async Task ResearchEthicsCommitteesProfile_ShouldReturnView_WithModel_WhenUserHasAccess(
       Guid recId,
       ReviewBodyDto reviewBody,
       AddRecMemberViewModel viewModel,
       Guid userId)
    {
        SetUser(LoggedInUserId);
        SetUserResponse(LoggedInUserId);

        var userEmail = "user@example.com";
        var user = new UserResponse
        {
            User = new User(userId.ToString(),
                            "azure-ad-12345",
                            "Mr",
                            "Test",
                            "Test",
                            userEmail,
                            "Software Developer",
                            "orgName",
                            "+44 7700 900123",
                            "United Kingdom",
                            IrasUserStatus.Active,
                            DateTime.UtcNow,
                            DateTime.UtcNow.AddDays(-2),
                            DateTime.UtcNow)
        };

        viewModel.Email = userEmail;
        viewModel.RecId = recId;
        viewModel.RecName = reviewBody.RegulatoryBodyName;

        reviewBody.Id = recId;
        reviewBody.Countries = new List<string> { "United Kingdom" };
        reviewBody.Users = new List<ReviewBodyUserDto>
        {
            new ReviewBodyUserDto
            {
                UserId = userId,
                Id = recId,
                Email = userEmail
            }
        };

        // Arrange
        var reviewBodyResponse = new ServiceResponse<ReviewBodyDto>
        {
            StatusCode = HttpStatusCode.BadGateway,
            Content = reviewBody
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(recId))
            .ReturnsAsync(reviewBodyResponse);

        var userResponse = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = user
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(userId.ToString(), null, null))
            .ReturnsAsync(userResponse);

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

        var claims = new[]
        {
        new Claim(ClaimTypes.Role, Roles.SystemAdministrator),
        new Claim(CustomClaimTypes.UserId, Guid.NewGuid().ToString())
    };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        var userServiceMock = new Mock<IUserManagementService>();

        // Act
        var result = await MemberManagementHelper.UserHasAccess(rec, user, userServiceMock.Object);

        // Assert
        result.ShouldBeTrue();

        // userService nie powinien być nawet wywołany
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

        var claims = new[]
        {
        new Claim(CustomClaimTypes.UserId, userId.ToString())
    };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        var userResponse = new ServiceResponse<UserResponse>
        {
            Content = new UserResponse
            {
                User = new User(
                    userId.ToString(),
                    "aad",
                    "Mr",
                    "Test",
                    "User",
                    "test@test.com",
                    "Dev",
                    "Org",
                    "123",
                    "United Kingdom",
                    IrasUserStatus.Disabled,
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    DateTime.UtcNow)
            }
        };

        var userServiceMock = new Mock<IUserManagementService>();
        userServiceMock
            .Setup(s => s.GetUser(userId.ToString(), null, null))
            .ReturnsAsync(userResponse);

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

        var claims = new[]
        {
        new Claim(CustomClaimTypes.UserId, userId.ToString())
    };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var user = new ClaimsPrincipal(identity);

        var userResponse = new ServiceResponse<UserResponse>
        {
            Content = new UserResponse
            {
                User = new User(
                    userId.ToString(),
                    "aad",
                    "Mr",
                    "Test",
                    "User",
                    "test@test.com",
                    "Dev",
                    "Org",
                    "123",
                    "United Kingdom",
                    IrasUserStatus.Active,
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    DateTime.UtcNow)
            }
        };

        var userServiceMock = new Mock<IUserManagementService>();
        userServiceMock
            .Setup(s => s.GetUser(userId.ToString(), null, null))
            .ReturnsAsync(userResponse);

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

        var claims = new[]
        {
        new Claim(CustomClaimTypes.UserId, userId.ToString())
    };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var userResponse = new ServiceResponse<UserResponse>
        {
            Content = new UserResponse
            {
                User = new User(
                    userId.ToString(),
                    "aad",
                    "Mr",
                    "Test",
                    "User",
                    "test@test.com",
                    "Dev",
                    "Org",
                    "123",
                    "United Kingdom",
                    IrasUserStatus.Active,
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    DateTime.UtcNow)
            }
        };

        var userServiceMock = new Mock<IUserManagementService>();
        userServiceMock
            .Setup(s => s.GetUser(userId.ToString(), null, null))
            .ReturnsAsync(userResponse);

        // Act
        var result = await MemberManagementHelper.UserHasAccess(rec, user, userServiceMock.Object);

        // Assert
        result.ShouldBeFalse();
    }
}