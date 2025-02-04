using System.Net;
using AutoFixture.Xunit2;
using Moq;
using Refit;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Shouldly;

namespace Rsp.IrasPortal.UnitTests.Services.UserManagementServiceTests;

public class GetUsersTests : TestServiceBase<UserManagementService>
{
    [Theory, AutoData]
    public async Task GetUsers_Should_Return_Success_Response_With_Users_When_Client_Returns_Success(UsersResponse usersResponse)
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        var apiResponse = new ApiResponse<UsersResponse>(httpResponse, usersResponse, new());

        var client = Mocker.GetMock<IUserManagementServiceClient>();
        client
            .Setup(c => c.GetUsers(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetUsers();

        // Assert
        result.ShouldBeOfType<ServiceResponse<UsersResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldBe(usersResponse);

        // Verify
        client.Verify(c => c.GetUsers(It.IsAny<int>(), It.IsAny<int>()), Times.Once());
    }

    [Fact]
    public async Task GetUsers_Should_Return_Failure_Response_When_Client_Returns_Failure()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var apiResponse = new ApiResponse<UsersResponse>(httpResponse, null, new());

        var client = Mocker.GetMock<IUserManagementServiceClient>();
        client
            .Setup(c => c.GetUsers(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetUsers();

        // Assert
        result.ShouldBeOfType<ServiceResponse<UsersResponse>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        // Verify
        client.Verify(c => c.GetUsers(It.IsAny<int>(), It.IsAny<int>()), Times.Once());
    }
}