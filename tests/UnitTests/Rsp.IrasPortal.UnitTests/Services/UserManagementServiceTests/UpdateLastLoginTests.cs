using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Domain.Identity;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.UserManagementServiceTests;

public class UpdateLastLoginTests : TestServiceBase<UserManagementService>
{
    private readonly Mock<IUserManagementServiceClient> _userManagementServiceClient;

    public UpdateLastLoginTests()
    {
        _userManagementServiceClient = Mocker.GetMock<IUserManagementServiceClient>();
    }

    [Theory, AutoData]
    public async Task UpdateLastLogin_Should_Return_Success_When_User_Updated(string email, UserResponse userResponse)
    {
        // Arrange

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        var getUserResponse = new ApiResponse<UserResponse>(httpResponse, userResponse, new());
        var updateUserResponse = Mock.Of<IApiResponse>
        (
            apiResponse =>
                apiResponse.IsSuccessStatusCode &&
                apiResponse.StatusCode == HttpStatusCode.OK
        );

        _userManagementServiceClient
            .Setup(c => c.GetUser(null, It.IsAny<string>(), null))
            .ReturnsAsync(getUserResponse);

        _userManagementServiceClient
            .Setup(c => c.UpdateUser(email, It.IsAny<User>()))
            .ReturnsAsync(updateUserResponse);

        // Act
        var result = await Sut.UpdateLastLogin(email);

        // Assert
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        _userManagementServiceClient.Verify(c => c.UpdateUser(email, It.IsAny<User>()), Times.Once());
    }

    [Theory, AutoData]
    public async Task UpdateLastLogin_Should_Return_Failure_When_GetUser_Fails(string email)
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound);
        var getUserResponse = new ApiResponse<UserResponse>(httpResponse, null, new());

        _userManagementServiceClient.Setup(x => x.GetUser(null, email, null))
            .ReturnsAsync(getUserResponse);

        // Act
        var result = await Sut.UpdateLastLogin(email);

        // Assert
        result.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}