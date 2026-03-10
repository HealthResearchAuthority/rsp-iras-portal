using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Services;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.UnitTests;

namespace Rsp.IrasPortal.UnitTests.Services.UserNotificationsServiceTests;

public class UserNotificationsTests : TestServiceBase<UserNotificationsService>
{
    [Theory]
    [AutoData]
    public async Task GetUserNotifications_Should_Return_Response_When_Client_Returns_Success(Guid userId, UserNotificationsResponse response)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<UserNotificationsResponse>>(r => r.IsSuccessStatusCode &&
                                                                              r.StatusCode == HttpStatusCode.OK &&
                                                                              r.Content == response
        );

        var client = Mocker.GetMock<IUserNotificationsServiceClient>();
        client
            .Setup(c => c.GetUserNotifications(userId.ToString(), 1, 20, It.IsAny<string>(), "asc", null))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetUserNotification(userId.ToString(), 1, 20, It.IsAny<string>(), "asc", null);

        // Assert
        result.ShouldBeOfType<ServiceResponse<UserNotificationsResponse>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        client.Verify(c => c.GetUserNotifications(userId.ToString(), 1, 20, It.IsAny<string>(), "asc", null), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task GetUserNotificationCount_Should_Return_Response_When_Client_Returns_Success(Guid userId)
    {
        var notificationsCount = 5;
        // Arrange
        var apiResponse = Mock.Of<IApiResponse<int>>(r => r.IsSuccessStatusCode &&
                                                                              r.StatusCode == HttpStatusCode.OK &&
                                                                              r.Content == notificationsCount
        );

        var client = Mocker.GetMock<IUserNotificationsServiceClient>();
        client
            .Setup(c => c.GetUnreadUserNotificationsCount(userId.ToString()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.GetUnreadUserNotificationsCount(userId.ToString());

        // Assert
        var model = result.ShouldBeOfType<ServiceResponse<int>>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);
        model.Content.ShouldBe(notificationsCount);

        // Verify
        client.Verify(c => c.GetUnreadUserNotificationsCount(userId.ToString()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task ReadUserNotificationCount_Should_Return_Response_When_Client_Returns_Success(Guid userId)
    {
        // Arrange
        var apiResponse = Mock.Of<IApiResponse>(r => r.IsSuccessStatusCode &&
                                                                              r.StatusCode == HttpStatusCode.OK
        );

        var client = Mocker.GetMock<IUserNotificationsServiceClient>();
        client
            .Setup(c => c.ReadUserNotifications(userId.ToString()))
            .ReturnsAsync(apiResponse);

        // Act
        var result = await Sut.ReadUserNotifications(userId.ToString());

        // Assert
        var model = result.ShouldBeOfType<ServiceResponse>();
        result.IsSuccessStatusCode.ShouldBeTrue();
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify
        client.Verify(c => c.ReadUserNotifications(userId.ToString()), Times.Once);
    }
}