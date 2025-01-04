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

public class GetRolesTests : TestServiceBase<UserManagementService>
{
    [Theory, AutoData]
    public async Task GetRoles_Should_Return_Success_Response_With_Roles_When_Client_Returns_Success(RolesResponse rolesResponse)
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);
        var apiResponse = new ApiResponse<RolesResponse>(httpResponse, rolesResponse, new());

        var client = Mocker.GetMock<IUserManagementServiceClient>();
        client
            .Setup(c => c.GetRoles(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetRoles();

        // Assert
        result.ShouldBeOfType<ServiceResponse<RolesResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Content.ShouldBe(rolesResponse);

        // Verify
        client.Verify(c => c.GetRoles(It.IsAny<int>(), It.IsAny<int>()), Times.Once());
    }

    [Fact]
    public async Task GetRoles_Should_Return_Failure_Response_When_Client_Returns_Failure()
    {
        // Arrange
        var httpResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var apiResponse = new ApiResponse<RolesResponse>(httpResponse, null, new());

        var client = Mocker.GetMock<IUserManagementServiceClient>();
        client
            .Setup(c => c.GetRoles(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetRoles();

        // Assert
        result.ShouldBeOfType<ServiceResponse<RolesResponse>>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        // Verify
        client.Verify(c => c.GetRoles(It.IsAny<int>(), It.IsAny<int>()), Times.Once());
    }
}