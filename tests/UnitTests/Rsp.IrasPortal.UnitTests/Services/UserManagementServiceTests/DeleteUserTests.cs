﻿using System.Net;
using AutoFixture.Xunit2;
using Moq;
using Refit;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Shouldly;

namespace Rsp.IrasPortal.UnitTests.Services.UserManagementServiceTests;

public class DeleteUserTests : TestServiceBase<UserManagementService>
{
    [Theory, AutoData]
    public async Task DeleteUser_Should_Return_Success_Response_When_Client_Returns_Success(string userId, string email)
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
            .Setup(c => c.DeleteUser(userId, email))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.DeleteUser(userId, email);

        // Assert
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        client.Verify(c => c.DeleteUser(userId, email), Times.Once());
    }

    [Theory, AutoData]
    public async Task DeleteUser_Should_Return_Failure_Response_When_Client_Returns_Failure(string userId, string email)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse>
        (
            apiResponse => !
                apiResponse.IsSuccessStatusCode &&
                apiResponse.StatusCode == HttpStatusCode.NotFound);

        var client = Mocker.GetMock<IUserManagementServiceClient>();
        client
            .Setup(c => c.DeleteUser(userId, email))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.DeleteUser(userId, email);

        // Assert
        result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeFalse();
        result.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Verify
        client.Verify(c => c.DeleteUser(userId, email), Times.Once());
    }
}