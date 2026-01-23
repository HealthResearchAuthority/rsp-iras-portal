using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.UserManagementServiceTests;

public class GetUserTests : TestServiceBase<UserManagementService>
{
    [Theory, AutoData]
    public async Task GetUser_Should_Return_Success_Response_With_User_When_Client_Returns_Success(string userId, string email, UserResponse userResponse)
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        var apiResponse = new ApiResponse<UserResponse>(httpResponse, userResponse, new());

        var client = Mocker.GetMock<IUserManagementServiceClient>();
        client
            .Setup(c => c.GetUser(userId, email, null))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetUser(userId, email);

        // Assert
        result.ShouldBeOfType<ServiceResponse<UserResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldBe(userResponse);

        // Verify
        client.Verify(c => c.GetUser(userId, email, null), Times.Once());
    }

    [Theory, AutoData]
    public async Task GetUser_Should_Return_Failure_Response_When_Client_Returns_Failure(string userId, string email)
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound);
        var apiResponse = new ApiResponse<UserResponse>(httpResponse, null, new());

        var client = Mocker.GetMock<IUserManagementServiceClient>();
        client
            .Setup(c => c.GetUser(userId, email, null))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetUser(userId, email);

        // Assert
        result.ShouldBeOfType<ServiceResponse<UserResponse>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Verify
        client.Verify(c => c.GetUser(userId, email, null), Times.Once());
    }
}