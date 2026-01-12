using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.UserManagementServiceTests;

public class CreateRoleTests : TestServiceBase<UserManagementService>
{
    [Theory, AutoData]
    public async Task CreateRole_Should_Return_Success_Response_When_Client_Returns_Success(string roleName)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse>
        (
            apiResponse =>
                apiResponse.IsSuccessStatusCode &&
                apiResponse.StatusCode == HttpStatusCode.OK
        );

        var client = Mocker.GetMock<IUserManagementServiceClient>();
        client
            .Setup(c => c.CreateRole(roleName))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.CreateRole(roleName);

        // Assert
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        client.Verify(c => c.CreateRole(roleName), Times.Once());
    }

    [Theory, AutoData]
    public async Task CreateRole_Should_Return_Failure_Response_When_Client_Returns_Failure(string roleName)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse>
        (
            apiResponse => !
                apiResponse.IsSuccessStatusCode &&
                apiResponse.StatusCode == HttpStatusCode.BadRequest
        );

        var client = Mocker.GetMock<IUserManagementServiceClient>();
        client
            .Setup(c => c.CreateRole(roleName))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.CreateRole(roleName);

        // Assert
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Verify
        client.Verify(c => c.CreateRole(roleName), Times.Once());
    }
}