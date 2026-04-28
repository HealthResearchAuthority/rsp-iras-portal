using System.Security.Claims;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Web.Features.MemberManagement.Controllers;
using Rsp.IrasPortal.Web.Features.MemberManagement.Models;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.Identity;
using Rsp.Portal.UnitTests;
using Claim = System.Security.Claims.Claim;

namespace Rsp.IrasPortal.UnitTests.Web.Features.MemberManagement.RecMemberManagementControllerTests;

public class CheckRecMemberTests : TestServiceBase<RecMemberManagementController>
{
    private readonly DefaultHttpContext _http;
    private readonly Guid LoggedInUserId = Guid.NewGuid();

    public CheckRecMemberTests()
    {
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        Sut.TempData = tempData;
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

    [Theory, AutoData]
    public async Task CheckRecMember_Returns_For_HappyPath_View(Guid recId,
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
                            "orgName", // IMPORTANT: match org if your action filters by org
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

        // arrange
        var userResponse = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = user
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(userId.ToString(), null, null))
            .ReturnsAsync(userResponse);

        var reviewBodyResponse = new ServiceResponse<ReviewBodyDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBody
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(recId))
            .ReturnsAsync(reviewBodyResponse);

        // act
        var result = await Sut.CheckRecMember(recId, userId);

        // assert
        var redirectResult = result.ShouldBeOfType<ViewResult>();
        var model = redirectResult.Model.ShouldBeAssignableTo<RecMemberViewModel>();
        model.ShouldNotBeNull();
        model.IsEditMode.ShouldBeTrue();
        model.UserId.ShouldBe(user.User.Id);
        model.RecId.ShouldBe(recId);
        model.EmailAddress.ShouldBe(user.User.Email);

        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetReviewBodyById(recId), Times.Once);
        Mocker.GetMock<IUserManagementService>()
            .Verify(s => s.GetUser(userId.ToString(), null, null), Times.Once);
    }

    [Theory, AutoData]
    public async Task CheckRecMember_Returns_Error_When_User_Is_Null(
        Guid recId,
        Guid userId,
        ReviewBodyDto reviewBody)
    {
        SetUser(LoggedInUserId);
        SetUserResponse(LoggedInUserId);

        var userEmail = "user@example.com";
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
        var reviewBodyResponse = new ServiceResponse<ReviewBodyDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBody
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(recId))
            .ReturnsAsync(reviewBodyResponse);

        var userResponse = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.BadGateway,
            Content = null
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(userId.ToString(), null, null))
            .ReturnsAsync(userResponse);

        // act
        var result = await Sut.CheckRecMember(recId, userId);

        // assert
        var redirectResult = result.ShouldBeOfType<StatusCodeResult>();
        redirectResult.StatusCode.ShouldBe((int)HttpStatusCode.BadGateway);

        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetReviewBodyById(recId), Times.Once);
        Mocker.GetMock<IUserManagementService>()
            .Verify(s => s.GetUser(userId.ToString(), null, null), Times.Once);
    }

    [Theory, AutoData]
    public async Task CheckRecMember_Returns_Error_When_User_Does_Is_Not_Member(
        Guid recId,
        Guid userId,
        ReviewBodyDto reviewBody)
    {
        SetUser(LoggedInUserId);
        SetUserResponse(LoggedInUserId);

        reviewBody.Id = recId;
        reviewBody.Countries = new List<string> { "United Kingdom" };
        reviewBody.Users = new List<ReviewBodyUserDto>();
        var reviewBodyResponse = new ServiceResponse<ReviewBodyDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBody
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(recId))
            .ReturnsAsync(reviewBodyResponse);

        var userResponse = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.BadGateway,
            Content = null
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(userId.ToString(), null, null))
            .ReturnsAsync(userResponse);

        // act
        var result = await Sut.CheckRecMember(recId, userId);

        // assert
        var redirectResult = result.ShouldBeOfType<NotFoundResult>();
        redirectResult.StatusCode.ShouldBe((int)HttpStatusCode.NotFound);

        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetReviewBodyById(recId), Times.Once);
        Mocker.GetMock<IUserManagementService>()
            .Verify(s => s.GetUser(userId.ToString(), null, null), Times.Never);
    }

    [Theory, AutoData]
    public async Task CheckRecMember_Returns_Error_When_Review_Body_Is_Null(Guid recId,
      Guid userId)
    {
        var reviewBodyResponse = new ServiceResponse<ReviewBodyDto>
        {
            StatusCode = HttpStatusCode.BadGateway,
            Content = null
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(recId))
            .ReturnsAsync(reviewBodyResponse);

        // act
        var result = await Sut.CheckRecMember(recId, userId);

        // assert
        var redirectResult = result.ShouldBeOfType<StatusCodeResult>();
        redirectResult.StatusCode.ShouldBe((int)HttpStatusCode.BadGateway);

        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetReviewBodyById(recId), Times.Once);
        Mocker.GetMock<IUserManagementService>()
            .Verify(s => s.GetUser(userId.ToString(), null, null), Times.Never);
    }

    [Theory, AutoData]
    public async Task CheckRecMember_Post_Returns_View_When_Valid(RecMemberViewModel model)
    {
        Mocker.GetMock<IValidator<RecMemberViewModel>>()
           .Setup(v => v.ValidateAsync(It.IsAny<RecMemberViewModel>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new ValidationResult());

        // act
        var result = await Sut.CheckRecMember(model);

        // assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldNotBeNull();
        viewResult.Model.ShouldBeOfType<RecMemberViewModel>();
        viewResult.Model.ShouldBeEquivalentTo(model);
    }

    [Theory, AutoData]
    public async Task CheckRecMember_Post_Returns_View_When_Invalid(RecMemberViewModel model)
    {
        Mocker.GetMock<IValidator<RecMemberViewModel>>()
           .Setup(v => v.ValidateAsync(It.IsAny<RecMemberViewModel>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new ValidationResult { Errors = new List<ValidationFailure> { new ValidationFailure("Error", "Error") } });

        // act
        var result = await Sut.CheckRecMember(model);

        // assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("AddRecMember");
        viewResult.Model.ShouldNotBeNull();
        var m = viewResult.Model.ShouldBeOfType<RecMemberViewModel>();
        m.ShouldBeEquivalentTo(model);
        Sut.ModelState.ErrorCount.ShouldBe(1);
        Sut.ModelState["Error"].Errors[0].ErrorMessage.ShouldBe("Error");
    }

    // arrange logged in user response
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
    public async Task ResearchEthicsCommitteeMembers_ShouldReturnNotFound_WhenReviewBodyNotFound()
    {
        var recId = Guid.NewGuid();

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(recId))
            .ReturnsAsync(new ServiceResponse<ReviewBodyDto>
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = null
            });

        var result = await Sut.ResearchEthicsCommitteeMembers(recId, null, null);

        var statusCoderesult = result.ShouldBeOfType<StatusCodeResult>();
        statusCoderesult.StatusCode.ShouldBe((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResearchEthicsCommitteeMembers_ShouldReturnForbid_WhenUserHasNoAccess()
    {
        var recId = Guid.NewGuid();

        var rec = new ReviewBodyDto
        {
            Id = recId,
            Countries = new List<string> { "Germany" }
        };

        SetUser(LoggedInUserId);
        SetUserResponse(LoggedInUserId);

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(recId))
            .ReturnsAsync(new ServiceResponse<ReviewBodyDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = rec
            });

        var result = await Sut.ResearchEthicsCommitteeMembers(recId, null, null);

        var statusCoderesult = result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public async Task ResearchEthicsCommitteeMembers_ShouldReturnView_WithMembers_WhenNoSorting()
    {
        var recId = Guid.NewGuid();
        SetUser(LoggedInUserId);
        SetUserResponse(LoggedInUserId);

        var rec = new ReviewBodyDto
        {
            Id = recId,
            RegulatoryBodyName = "Test REC",
            Countries = new() { "United Kingdom" },
            Users = new List<ReviewBodyUserDto>()
        {
            new() { UserId = LoggedInUserId}
        }
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(recId))
            .ReturnsAsync(new ServiceResponse<ReviewBodyDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = rec
            });

        var result = await Sut.ResearchEthicsCommitteeMembers(recId, null, null);

        var view = result.ShouldBeOfType<ViewResult>();
        var model = view.Model.ShouldBeOfType<RecMembersViewModel>();

        model.RecUsers.ShouldNotBeNull();
        model.RecUsers!.Count().ShouldBe(1);
    }

    [Fact]
    public async Task ResearchEthicsCommitteeMembers_ShouldSortMembers_Ascending()
    {
        var recId = Guid.NewGuid();
        SetUser(LoggedInUserId);
        SetUserResponse(LoggedInUserId);

        var users = new List<ReviewBodyUserDto>
    {
        new() { UserId = LoggedInUserId },
        new() { UserId = Guid.NewGuid() }
    };

        var rec = new ReviewBodyDto
        {
            Id = recId,
            Countries = new() { "United Kingdom" },
            Users = users
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(recId))
            .ReturnsAsync(new ServiceResponse<ReviewBodyDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = rec
            });

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(It.IsAny<string>(), null, null))
            .ReturnsAsync((string userId, string? a, string? b) =>
                new ServiceResponse<UserResponse>
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new UserResponse
                    {
                        User = new User(
                            userId,
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
                });

        var result = await Sut.ResearchEthicsCommitteeMembers(
            recId,
            nameof(RecMemberViewModel.FirstName),
            SortDirections.Ascending);

        var model = result.ShouldBeOfType<ViewResult>()
                          .Model.ShouldBeOfType<RecMembersViewModel>();

        model.Pagination!.SortField.ShouldBe(nameof(RecMemberViewModel.FirstName));
    }

    [Fact]
    public async Task ResearchEthicsCommitteeMembers_ShouldReturnServiceError_WhenUserDetailsFail()
    {
        var recId = Guid.NewGuid();
        SetUser(LoggedInUserId);
        SetUserResponse(LoggedInUserId);

        var rec = new ReviewBodyDto
        {
            Id = recId,
            Countries = new() { "United Kingdom" },
            Users = new List<ReviewBodyUserDto>()
        {
            new() { UserId = LoggedInUserId}
        }
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(recId))
            .ReturnsAsync(new ServiceResponse<ReviewBodyDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = rec
            });

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(LoggedInUserId.ToString(), null, null))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.NotFound
            });

        var result = await Sut.ResearchEthicsCommitteeMembers(recId, null, null);

        result.ShouldBeOfType<ForbidResult>();
    }

    [Fact]
    public async Task SortMembers_ShouldReturnUnsorted_WhenSortFieldIsNull()
    {
        var recId = Guid.NewGuid();
        SetUser(LoggedInUserId);

        var userId = Guid.NewGuid();
        var rec = new ReviewBodyDto
        {
            Id = recId,
            Countries = new() { "United Kingdom" },
            Users = new List<ReviewBodyUserDto> { new ReviewBodyUserDto { UserId = userId } }
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(recId))
            .ReturnsAsync(new ServiceResponse<ReviewBodyDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = rec
            });

        UserResponse user = getUserResponse(userId);

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(It.IsAny<string>(), null, null))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = user
            });

        var result = await Sut.ResearchEthicsCommitteeMembers(recId, null, null);

        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task SortMembers_ShouldSortByLastName_Descending()
    {
        var recId = Guid.NewGuid();
        SetUser(LoggedInUserId);

        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var rec = new ReviewBodyDto
        {
            Id = recId,
            Countries = new() { "United Kingdom" },
            Users = new List<ReviewBodyUserDto>
        {
            new() { UserId = user1 },
            new() { UserId = user2 }
        }
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(recId))
            .ReturnsAsync(new ServiceResponse<ReviewBodyDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = rec
            });
        UserResponse user = getUserResponse(userId);

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(It.IsAny<string>(), null, null))
            .ReturnsAsync((string id, string? a, string? b) =>
                new ServiceResponse<UserResponse>
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = user
                });

        var result = await Sut.ResearchEthicsCommitteeMembers(
            recId,
            nameof(RecMemberViewModel.LastName),
            SortDirections.Descending);

        var model = result.ShouldBeOfType<ViewResult>()
            .Model.ShouldBeOfType<RecMembersViewModel>();

        model.RecUsers.ShouldNotBeNull();
    }

    [Fact]
    public async Task SortMembers_ShouldUseDefault_WhenSortFieldIsUnknown()
    {
        var recId = Guid.NewGuid();
        SetUser(LoggedInUserId);

        var userId = Guid.NewGuid();

        var rec = new ReviewBodyDto
        {
            Id = recId,
            Countries = new() { "United Kingdom" },
            Users = new List<ReviewBodyUserDto> { new ReviewBodyUserDto { UserId = userId } }
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(recId))
            .ReturnsAsync(new ServiceResponse<ReviewBodyDto>
            {
                StatusCode = HttpStatusCode.OK,
                Content = rec
            });

        UserResponse user = getUserResponse(userId);

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(It.IsAny<string>(), null, null))
            .ReturnsAsync(new ServiceResponse<UserResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = user
            });

        var result = await Sut.ResearchEthicsCommitteeMembers(
            recId,
            "UnknownField",
            SortDirections.Ascending);

        result.ShouldBeOfType<ViewResult>();
    }

    private static UserResponse getUserResponse(Guid userId)
    {
        var user = new UserResponse
        {
            User = new User(userId.ToString(),
                                    "azure-ad-12345",
                                    "Mr",
                                    "Test",
                                    "Test",
                                    "userEmail@email.com",
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
        return user;
    }
}