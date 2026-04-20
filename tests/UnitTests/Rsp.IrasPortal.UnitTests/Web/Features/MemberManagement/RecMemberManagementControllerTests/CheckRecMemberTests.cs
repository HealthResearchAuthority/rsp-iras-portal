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
}