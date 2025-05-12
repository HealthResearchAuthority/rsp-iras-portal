using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class RemoveUserFromReviewBodyTests : TestServiceBase<ReviewBodyController>
{
    [Theory, AutoData]
    public async Task ConfirmRemoveUser_ShouldReturnView
    (
        Guid reviewBodyId,
        Guid userId,
        ReviewBodyDto reviewBody,
        UserResponse userResponse
    )
    {
        reviewBody.Id = reviewBodyId;

        // Arrange
        var reviewBodyResponse = new ServiceResponse<ReviewBodyDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBody
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(reviewBodyId))
            .ReturnsAsync(reviewBodyResponse);

        var userServiceResponse = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = userResponse
        };

        Mocker.GetMock<IUserManagementService>()
           .Setup(s => s.GetUser(userId.ToString(), null))
           .ReturnsAsync(userServiceResponse);

        // Act
        var result = await Sut.ConfirmRemoveUser(reviewBodyId, userId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeAssignableTo<ConfirmAddRemoveReviewBodyUserModel>();

        // Verify
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetReviewBodyById(reviewBodyId), Times.Once);

        Mocker.GetMock<IUserManagementService>()
            .Verify(s => s.GetUser(userId.ToString(), null), Times.Once);
    }

    [Theory, AutoData]
    public async Task SubmitRemoveUser_ShouldReturnView
    (
        Guid reviewBodyId,
        Guid userId,
        ReviewBodyDto reviewBody,
        ReviewBodyUserDto reviewBodyUser,
        UserResponse userResponse
    )
    {
        reviewBodyUser.UserId = userId;
        reviewBodyUser.ReviewBodyId = reviewBodyId;
        reviewBodyUser.DateAdded = DateTime.UtcNow;

        reviewBody.Id = reviewBodyId;

        // Arrange
        var reviewBodyResponse = new ServiceResponse<ReviewBodyDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBody
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(reviewBodyId))
            .ReturnsAsync(reviewBodyResponse);

        var userServiceResponse = new ServiceResponse<UserResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = userResponse
        };

        Mocker.GetMock<IUserManagementService>()
           .Setup(s => s.GetUser(userId.ToString(), null))
           .ReturnsAsync(userServiceResponse);

        var submitRemoveUserServiceResponse = new ServiceResponse<ReviewBodyUserDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBodyUser
        };

        Mocker.GetMock<IReviewBodyService>()
           .Setup(s => s.RemoveUserFromReviewBody(reviewBodyId, userId))
           .ReturnsAsync(submitRemoveUserServiceResponse);

        // Act
        var result = await Sut.SubmitRemoveUser(reviewBodyId, userId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeAssignableTo<ConfirmAddRemoveReviewBodyUserModel>();

        // Verify
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetReviewBodyById(reviewBodyId), Times.Once);

        Mocker.GetMock<IUserManagementService>()
            .Verify(s => s.GetUser(userId.ToString(), null), Times.Once);
    }
}