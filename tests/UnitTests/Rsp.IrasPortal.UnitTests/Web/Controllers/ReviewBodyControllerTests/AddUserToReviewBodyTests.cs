using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class AddUserToReviewBodyTests : TestServiceBase<ReviewBodyController>
{
    [Theory, AutoData]
    public async Task ViewReviewBodyUsers_ShouldReturnView(
        Guid id,
        List<ReviewBodyDto> reviewBodies,
        UsersResponse usersResponse)
    {
        foreach (var body in reviewBodies)
        {
            body.Id = id;
        }

        // Arrange
        var reviewBodyResponse = new ServiceResponse<IEnumerable<ReviewBodyDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBodies
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(id))
            .ReturnsAsync(reviewBodyResponse);

        var usersServiceResponse = new ServiceResponse<UsersResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = usersResponse
        };

        var userIds = reviewBodies?.FirstOrDefault()?.Users?.Select(x => x.UserId.ToString()).ToList();

        Mocker.GetMock<IUserManagementService>()
           .Setup(s => s.GetUsersByIds(userIds!, null, 1, 10))
           .ReturnsAsync(usersServiceResponse);

        // Act
        var result = await Sut.ViewReviewBodyUsers(id);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeAssignableTo<ReviewBodyListUsersModel>();

        // Verify
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetReviewBodyById(id), Times.Once);
    }

    [Theory, AutoData]
    public async Task AddUser_ShouldReturnView(
        Guid id,
        List<ReviewBodyDto> reviewBodies,
        UsersResponse usersResponse,
        string searchQuery)
    {
        foreach (var body in reviewBodies)
        {
            body.Id = id;
        }

        // Arrange
        var reviewBodyResponse = new ServiceResponse<IEnumerable<ReviewBodyDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBodies
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(id))
            .ReturnsAsync(reviewBodyResponse);

        var usersServiceResponse = new ServiceResponse<UsersResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = usersResponse
        };

        var userIds = reviewBodies?.FirstOrDefault()?.Users?.Select(x => x.UserId.ToString()).ToList();

        Mocker.GetMock<IUserManagementService>()
           .Setup(s => s.SearchUsers(searchQuery, userIds, 1, 10))
           .ReturnsAsync(usersServiceResponse);

        // Act
        var result = await Sut.ViewAddUser(id, searchQuery);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeAssignableTo<ReviewBodyListUsersModel>();

        // Verify
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetReviewBodyById(id), Times.Once);

        Mocker.GetMock<IUserManagementService>()
            .Verify(s => s.SearchUsers(searchQuery, userIds, 1, 10), Times.Once);
    }

    [Theory, AutoData]
    public async Task ConfirmAddUser_ShouldReturnView(
        Guid reviewBodyId,
        Guid userId,
        List<ReviewBodyDto> reviewBodies,
        UserResponse userResponse)
    {
        foreach (var body in reviewBodies)
        {
            body.Id = reviewBodyId;
        }

        // Arrange
        var reviewBodyResponse = new ServiceResponse<IEnumerable<ReviewBodyDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBodies
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
        var result = await Sut.ConfirmAddUser(reviewBodyId, userId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeAssignableTo<ConfirmAddReviewBodyUserModel>();

        // Verify
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetReviewBodyById(reviewBodyId), Times.Once);

        Mocker.GetMock<IUserManagementService>()
            .Verify(s => s.GetUser(userId.ToString(), null), Times.Once);
    }

    [Theory, AutoData]
    public async Task SubmitAddUser_ShouldReturnView(
        Guid reviewBodyId,
        Guid userId,
        List<ReviewBodyDto> reviewBodies,
        ReviewBodyUserDto reviewBodyUser,
        UserResponse userResponse)
    {
        reviewBodyUser.UserId = userId;
        reviewBodyUser.ReviewBodyId = reviewBodyId;
        reviewBodyUser.DateAdded = DateTime.UtcNow;

        foreach (var body in reviewBodies)
        {
            body.Id = reviewBodyId;
        }

        // Arrange
        var reviewBodyResponse = new ServiceResponse<IEnumerable<ReviewBodyDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBodies
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

        var submitAdduUserServiceResponse = new ServiceResponse<ReviewBodyUserDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBodyUser
        };

        Mocker.GetMock<IReviewBodyService>()
           .Setup(s => s.AddUserToReviewBody(reviewBodyUser))
           .ReturnsAsync(submitAdduUserServiceResponse);

        // Act
        var result = await Sut.SubmitAddUser(reviewBodyId, userId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeAssignableTo<ConfirmAddReviewBodyUserModel>();

        // Verify
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetReviewBodyById(reviewBodyId), Times.Once);

        Mocker.GetMock<IUserManagementService>()
            .Verify(s => s.GetUser(userId.ToString(), null), Times.Once);
    }
}