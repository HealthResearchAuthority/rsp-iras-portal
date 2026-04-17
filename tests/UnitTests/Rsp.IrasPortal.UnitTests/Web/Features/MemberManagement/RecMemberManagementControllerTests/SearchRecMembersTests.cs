using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Features.MemberManagement.Controllers;
using Rsp.IrasPortal.Web.Features.MemberManagement.Models;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.Identity;
using Rsp.Portal.UnitTests;

namespace Rsp.IrasPortal.UnitTests.Web.Features.MemberManagement.RecMemberManagementControllerTests;

public class SearchRecMembersTests : TestServiceBase<RecMemberManagementController>
{
    [Theory, AutoData]
    public async Task SearchRecMember_Returns_View(Guid recId, ReviewBodyDto reviewBody)
    {
        reviewBody.Id = recId;

        // arrange
        var reviewBodyResponse = new ServiceResponse<ReviewBodyDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBody
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(recId))
            .ReturnsAsync(reviewBodyResponse);

        // act
        var result = await Sut.SearchRecMember(recId);

        // assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var resultModel = viewResult.Model.ShouldBeOfType<AddRecMemberViewModel>();
        resultModel.RecId.ShouldBe(recId);
        resultModel.RecName.ShouldBe(reviewBody.RegulatoryBodyName);
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetReviewBodyById(recId), Times.Once);
    }

    [Theory, AutoData]
    public async Task SearchRecMember_Returns_NotFound(Guid recId)
    {
        // arrange
        var reviewBodyResponse = new ServiceResponse<ReviewBodyDto>
        {
            StatusCode = HttpStatusCode.NotFound,
            Content = null
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(recId))
            .ReturnsAsync(reviewBodyResponse);

        // act
        var result = await Sut.SearchRecMember(recId);

        // assert
        result.ShouldBeOfType<NotFoundResult>();
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetReviewBodyById(recId), Times.Once);
    }

    [Theory, AutoData]
    public async Task PostSearchRecMember_Redirects_When_User_Exists_View(Guid recId,
        ReviewBodyDto reviewBody,
        AddRecMemberViewModel viewModel,
        Guid userId)
    {
        var userEmail = "user.does.exist@example.com";
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
                            "Active",
                            DateTime.UtcNow,
                            DateTime.UtcNow.AddDays(-2),
                            DateTime.UtcNow)
        };

        viewModel.Email = userEmail;
        viewModel.RecId = recId;
        viewModel.RecName = reviewBody.RegulatoryBodyName;

        reviewBody.Id = recId;
        reviewBody.Users = new List<ReviewBodyUserDto>
        {
            new ReviewBodyUserDto
            {
                UserId = userId,
                Id = recId,
                Email = userEmail
            }
        };

        Mocker.GetMock<IValidator<AddRecMemberViewModel>>()
           .Setup(v => v.ValidateAsync(It.IsAny<AddRecMemberViewModel>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new ValidationResult());

        // arrange
        var userResponse = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = user
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(null, user.User.Email, null))
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
        var result = await Sut.SearchRecMember(viewModel);

        // assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe("MemberExistsInRec");
        redirectResult.RouteValues?.GetValueOrDefault("recId").ShouldBe(recId.ToString());

        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetReviewBodyById(recId), Times.Once);
        Mocker.GetMock<IUserManagementService>()
            .Verify(s => s.GetUser(null, user.User.Email, null), Times.Once);
    }

    [Theory, AutoData]
    public async Task PostSearchRecMember_HappyPath_Returns_View(Guid recId,
        ReviewBodyDto reviewBody,
        AddRecMemberViewModel viewModel,
        Guid userId)
    {
        var userEmail = "user.does.not.exist@example.com";
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
        reviewBody.Users = new List<ReviewBodyUserDto>
        {
            new ReviewBodyUserDto
            {
                UserId = userId,
                Id = recId,
                Email = "someone@hra.com" // different email to ensure we are not hitting the "MemberExistsInRec" path
            }
        };

        Mocker.GetMock<IValidator<AddRecMemberViewModel>>()
           .Setup(v => v.ValidateAsync(It.IsAny<AddRecMemberViewModel>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new ValidationResult());

        // arrange
        var userResponse = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = user
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(null, user.User.Email, null))
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
        var result = await Sut.SearchRecMember(viewModel);

        // assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var resultModel = viewResult.Model.ShouldBeOfType<AddRecMemberViewModel>();
        resultModel.RecId.ShouldBe(recId);
        resultModel.RecName.ShouldBe(reviewBody.RegulatoryBodyName);
        resultModel.Users.Count().ShouldBe(1);
        resultModel.Users.FirstOrDefault()?.Id.ShouldBe(user.User.Id);

        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetReviewBodyById(recId), Times.Once);
        Mocker.GetMock<IUserManagementService>()
            .Verify(s => s.GetUser(null, user.User.Email, null), Times.Once);
    }

    [Theory, AutoData]
    public async Task PostSearchRecMember_Redirects_When_User_Not_Active(Guid recId,
    ReviewBodyDto reviewBody,
    AddRecMemberViewModel viewModel,
    Guid userId)
    {
        var userEmail = "user.does.not.exist@example.com";
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
                            IrasUserStatus.Disabled,
                            DateTime.UtcNow,
                            DateTime.UtcNow.AddDays(-2),
                            DateTime.UtcNow)
        };

        viewModel.Email = userEmail;
        viewModel.RecId = recId;
        viewModel.RecName = reviewBody.RegulatoryBodyName;

        reviewBody.Id = recId;
        reviewBody.Users = new List<ReviewBodyUserDto>
        {
            new ReviewBodyUserDto
            {
                UserId = userId,
                Id = recId,
                Email = "someone@hra.com" // different email to ensure we are not hitting the "MemberExistsInRec" path
            }
        };

        Mocker.GetMock<IValidator<AddRecMemberViewModel>>()
           .Setup(v => v.ValidateAsync(It.IsAny<AddRecMemberViewModel>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new ValidationResult());

        // arrange
        var userResponse = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = user
        };

        Mocker.GetMock<IUserManagementService>()
            .Setup(s => s.GetUser(null, user.User.Email, null))
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
        var result = await Sut.SearchRecMember(viewModel);

        // assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe("RecMemberNotActive");
        redirectResult.RouteValues?.GetValueOrDefault("recId").ShouldBe(recId.ToString());

        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetReviewBodyById(recId), Times.Once);
        Mocker.GetMock<IUserManagementService>()
            .Verify(s => s.GetUser(null, user.User.Email, null), Times.Once);
    }

    [Theory, AutoData]
    public void EditNewRecMember_Returns_View(RecMemberViewModel model)
    {
        // act
        var result = Sut.EditNewRecMember(model);

        // assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("AddRecMember");
        viewResult.Model.ShouldBeOfType<RecMemberViewModel>();
        viewResult.Model.ShouldBe(model);
    }

    [Theory, AutoData]
    public void MemberExistsInRec_Returns_View(string recId)
    {
        // act
        var result = Sut.MemberExistsInRec(recId);

        // assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("MemberExistsInRec");
        viewResult.Model.ShouldBeOfType<string>();
        viewResult.Model.ShouldBe(recId);
    }

    [Theory, AutoData]
    public void RecMemberNotFound_Returns_View(string recId)
    {
        // act
        var result = Sut.RecMemberNotFound(recId);

        // assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("RecMemberNotFound");
        viewResult.Model.ShouldBeOfType<string>();
        viewResult.Model.ShouldBe(recId);
    }

    [Theory, AutoData]
    public void RecMemberNotActive_Returns_View(string recId)
    {
        // act
        var result = Sut.RecMemberNotActive(recId);

        // assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("RecMemberNotActive");
        viewResult.Model.ShouldBeOfType<string>();
        viewResult.Model.ShouldBe(recId);
    }
}