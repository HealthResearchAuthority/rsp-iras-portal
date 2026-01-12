using System.Net;
using AutoFixture.Xunit2;
using Moq;
using Refit;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.ServiceClients;
using Rsp.Portal.Services;
using Shouldly;

namespace Rsp.Portal.UnitTests.Services.UserManagementServiceTests;

public class UpdateRolesTests : TestServiceBase<UserManagementService>
{
    [Theory, AutoData]
    public async Task UpdateRoles_Should_Return_Success_Response_When_Client_Returns_Success(string email, string rolesToRemove, string rolesToAdd)
    {
        // Arrange
        var removeApiResponse = Mock.Of<IApiResponse>
        (
            apiResponse =>
                apiResponse.IsSuccessStatusCode &&
                apiResponse.StatusCode == HttpStatusCode.OK
        );

        var addApiResponse = Mock.Of<IApiResponse>
        (
            apiResponse =>
                apiResponse.IsSuccessStatusCode &&
                apiResponse.StatusCode == HttpStatusCode.OK
        );

        var client = Mocker.GetMock<IUserManagementServiceClient>();
        client
            .Setup(c => c.RemoveUsersFromRoles(email, rolesToRemove))
            .ReturnsAsync(removeApiResponse);
        client
            .Setup(c => c.AddUserToRoles(email, rolesToAdd))
            .ReturnsAsync(addApiResponse);

        // Act
        var result = await Sut.UpdateRoles(email, rolesToRemove, rolesToAdd);

        // Assert
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        client.Verify(c => c.RemoveUsersFromRoles(email, rolesToRemove), Times.Once());
        client.Verify(c => c.AddUserToRoles(email, rolesToAdd), Times.Once());
    }

    [Theory, AutoData]
    public async Task UpdateRoles_Should_Return_Failure_Response_When_Remove_Fails(string email, string rolesToRemove, string rolesToAdd)
    {
        // Arrange
        var removeApiResponse = Mock.Of<IApiResponse>
        (
            apiResponse => !
                apiResponse.IsSuccessStatusCode &&
                apiResponse.StatusCode == HttpStatusCode.NotFound);

        var addApiResponse = Mock.Of<IApiResponse>
        (
            apiResponse =>
                apiResponse.IsSuccessStatusCode &&
                apiResponse.StatusCode == HttpStatusCode.OK
        );

        var client = Mocker.GetMock<IUserManagementServiceClient>();
        client
            .Setup(c => c.RemoveUsersFromRoles(email, rolesToRemove))
            .ReturnsAsync(removeApiResponse);
        client
            .Setup(c => c.AddUserToRoles(email, rolesToAdd))
            .ReturnsAsync(addApiResponse);

        // Act
        var result = await Sut.UpdateRoles(email, rolesToRemove, rolesToAdd);

        // Assert
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Verify
        client.Verify(c => c.RemoveUsersFromRoles(email, rolesToRemove), Times.Once());
        client.Verify(c => c.AddUserToRoles(email, rolesToAdd), Times.Never());
    }

    [Theory, AutoData]
    public async Task UpdateRoles_Should_Return_Failure_Response_When_Add_Fails(string email, string rolesToRemove, string rolesToAdd)
    {
        // Arrange
        var removeApiResponse = Mock.Of<IApiResponse>
        (
            apiResponse =>
                apiResponse.IsSuccessStatusCode &&
                apiResponse.StatusCode == HttpStatusCode.OK
        );

        var addApiResponse = Mock.Of<IApiResponse>
        (
            apiResponse => !
                apiResponse.IsSuccessStatusCode &&
                apiResponse.StatusCode == HttpStatusCode.BadRequest
        );

        var client = Mocker.GetMock<IUserManagementServiceClient>();
        client
            .Setup(c => c.RemoveUsersFromRoles(email, rolesToRemove))
            .ReturnsAsync(removeApiResponse);
        client
            .Setup(c => c.AddUserToRoles(email, rolesToAdd))
            .ReturnsAsync(addApiResponse);

        // Act
        var result = await Sut.UpdateRoles(email, rolesToRemove, rolesToAdd);

        // Assert
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest
        );

        // Verify
        client.Verify(c => c.RemoveUsersFromRoles(email, rolesToRemove), Times.Once());
        client.Verify(c => c.AddUserToRoles(email, rolesToAdd), Times.Once());
    }
}