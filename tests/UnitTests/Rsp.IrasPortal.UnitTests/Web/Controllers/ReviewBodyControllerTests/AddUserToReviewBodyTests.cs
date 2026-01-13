using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class AddUserToReviewBodyTests : TestServiceBase<ReviewBodyController>
{
    [Theory, AutoData]
    public async Task ViewReviewBodyUsers_ShouldReturnView(
        Guid id,
        ReviewBodyDto reviewBody,
        UsersResponse usersResponse)
    {
        reviewBody.Id = id;

        // Arrange
        var reviewBodyResponse = new ServiceResponse<ReviewBodyDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBody
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(id))
            .ReturnsAsync(reviewBodyResponse);

        var usersServiceResponse = new ServiceResponse<UsersResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = usersResponse
        };

        var userIds = reviewBody?.Users?.Select(x => x.UserId.ToString()).ToList();

        Mocker.GetMock<IUserManagementService>()
           .Setup(s => s.GetUsersByIds(userIds!, null, 1, 20))
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
        ReviewBodyDto reviewBody,
        UsersResponse usersResponse,
        string searchQuery)
    {
        reviewBody.Id = id;

        // Arrange
        var reviewBodyResponse = new ServiceResponse<ReviewBodyDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBody
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(id))
            .ReturnsAsync(reviewBodyResponse);

        var usersServiceResponse = new ServiceResponse<UsersResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = usersResponse
        };

        var userIds = reviewBody?.Users?.Select(x => x.UserId.ToString()).ToList();

        Mocker.GetMock<IUserManagementService>()
           .Setup(s => s.SearchUsers(searchQuery, userIds, 1, 20))
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
            .Verify(s => s.SearchUsers(searchQuery, userIds, 1, 20), Times.Once);
    }

    [Theory, AutoData]
    public async Task ConfirmAddUser_ShouldReturnView(
        Guid reviewBodyId,
        Guid userId,
        ReviewBodyDto reviewBody,
        UserResponse userResponse)
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
           .Setup(s => s.GetUser(userId.ToString(), null, null))
           .ReturnsAsync(userServiceResponse);

        // Act
        var result = await Sut.ConfirmAddUser(reviewBodyId, userId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeAssignableTo<ConfirmAddRemoveReviewBodyUserModel>();

        // Verify
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetReviewBodyById(reviewBodyId), Times.Once);

        Mocker.GetMock<IUserManagementService>()
            .Verify(s => s.GetUser(userId.ToString(), null, null), Times.Once);
    }

    [Theory, AutoData]
    public async Task SubmitAddUser_ShouldReturnView(
        Guid reviewBodyId,
        Guid userId,
        ReviewBodyDto reviewBody,
        ReviewBodyUserDto reviewBodyUser,
        UserResponse userResponse)
    {
        reviewBodyUser.UserId = userId;
        reviewBodyUser.Id = reviewBodyId;
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
           .Setup(s => s.GetUser(userId.ToString(), null, null))
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
        viewResult.Model.ShouldBeAssignableTo<ConfirmAddRemoveReviewBodyUserModel>();

        // Verify
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetReviewBodyById(reviewBodyId), Times.Once);

        Mocker.GetMock<IUserManagementService>()
            .Verify(s => s.GetUser(userId.ToString(), null, null), Times.Once);
    }
}