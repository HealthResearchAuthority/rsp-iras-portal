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
using Shouldly;
using Claim = System.Security.Claims.Claim;

namespace Rsp.Portal.UnitTests.Web.Features.MemberManagement.ResearchEthicsCommittees;

public class ResearchEthicsCommitteesControllerTests : TestServiceBase<ResearchEthicsCommitteesController>
{
    private readonly DefaultHttpContext _http;
    private readonly Guid LoggedInUserId = Guid.NewGuid();

    public ResearchEthicsCommitteesControllerTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

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

    [Fact]
    public async Task ResearchEthicsCommittees_ShouldReturnView_WithDefaultModel()
    {
        // Act
        var result = await Sut.ResearchEthicsCommittees();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeOfType<MemberManagementResearchEthicsCommitteesViewModel>();
    }

    [Theory, AutoData]
    public async Task SearchMyOrganisations_ShouldStoreSearchModelInSession_AndRedirectToResearchEthicsCommittees(
        MemberManagementResearchEthicsCommitteesViewModel model,
        string sortField,
        string sortDirection)
    {
        // Arrange
        model.Search ??= new MemberManagementResearchEthicsCommitteesSearchModel();

        // Act
        var result = await Sut.SearchMyOrganisations(model, sortField, sortDirection);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(ResearchEthicsCommitteesController.ResearchEthicsCommittees));
        redirectResult.RouteValues!["sortField"].ShouldBe(sortField);
        redirectResult.RouteValues["sortDirection"].ShouldBe(sortDirection);

        var sessionValue = _http.Session.GetString(SessionKeys.MemberManagementResearchEthicsCommitteesSearch);
        sessionValue.ShouldNotBeNull();

        var storedModel = JsonSerializer.Deserialize<MemberManagementResearchEthicsCommitteesSearchModel>(sessionValue!);
        storedModel.ShouldNotBeNull();
        storedModel.ShouldBeEquivalentTo(model.Search);
    }

    [Fact]
    public async Task SearchMyOrganisations_ShouldStoreEmptySearchModelInSession_WhenSearchIsNull()
    {
        // Arrange
        var model = new MemberManagementResearchEthicsCommitteesViewModel
        {
            Search = null
        };

        // Act
        var result = await Sut.SearchMyOrganisations(model);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(ResearchEthicsCommitteesController.ResearchEthicsCommittees));
        redirectResult.RouteValues!["sortField"].ShouldBe("ResearchEthicsCommitteeName");
        redirectResult.RouteValues["sortDirection"].ShouldBe("asc");

        var sessionValue = _http.Session.GetString(SessionKeys.MemberManagementResearchEthicsCommitteesSearch);
        sessionValue.ShouldNotBeNull();

        var storedModel = JsonSerializer.Deserialize<MemberManagementResearchEthicsCommitteesSearchModel>(sessionValue!);
        storedModel.ShouldNotBeNull();
        storedModel.ShouldBeOfType<MemberManagementResearchEthicsCommitteesSearchModel>();
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
                    IrasUserStatus.Disabled, // 🔴 kluczowe
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
            Countries = new List<string> { "Germany" } // 🔴 inny kraj
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
                    "United Kingdom", // 🇬🇧
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