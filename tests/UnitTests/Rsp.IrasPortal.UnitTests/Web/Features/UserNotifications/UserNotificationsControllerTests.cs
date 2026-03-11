using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.UserNotifications.Controllers;
using Rsp.IrasPortal.Web.Features.UserNotifications.Models;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.UnitTests;

namespace Rsp.IrasPortal.UnitTests.Web.Features.UserNotifications;

public class UserNotificationsControllerTests : TestServiceBase<UserNotificationsController>
{
    private readonly DefaultHttpContext _http;

    public UserNotificationsControllerTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

    [Theory, AutoData]
    public async Task Dashboard_Returns_Notifications_When_Present(UserNotificationsResponse response, Guid uId)
    {
        Mocker.GetMock<IFeatureManager>()
            .Setup(f => f.IsEnabledAsync(FeatureFlags.UserNotifications))
            .ReturnsAsync(true);

        // Arrange
        _http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("userId", uId.ToString())
        }, "TestAuth"));

        var notificationType = "Action";
        var serviceResponse = new ServiceResponse<UserNotificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = response
        };

        Mocker.GetMock<IUserNotificationsService>()
            .Setup(s => s.GetUserNotification(It.IsAny<string>(), 1, 20,
                                             nameof(UserNotificationResponse.DateTimeCreated), SortDirections.Descending, notificationType))
            .ReturnsAsync(serviceResponse);

        var result = await Sut.UserNotificationsDashboard(notificationType, 1, 20, nameof(UserNotificationResponse.DateTimeCreated), SortDirections.Descending);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<UserNotificationsViewModel>();
        model.NotificationType.ShouldBe(notificationType);
        model.Notifications.ShouldNotBeNull();
        model.Notifications.Count().ShouldBe(response.Notifications.Count());
        model.Pagination.ShouldNotBeNull();
        model.Pagination.PageNumber.ShouldBe(1);
        model.Pagination.PageSize.ShouldBe(20);

        Mocker.GetMock<IUserNotificationsService>()
           .Verify(s => s.GetUserNotification(uId.ToString(), 1, 20,
                                             nameof(UserNotificationResponse.DateTimeCreated), SortDirections.Descending, notificationType), Times.Once);

        Mocker.GetMock<IUserNotificationsService>()
           .Verify(s => s.ReadUserNotifications(uId.ToString()), Times.Once);
    }

    [Theory, AutoData]
    public async Task Dashboard_Returns_Error_When_UserId_Not_Present(UserNotificationsResponse response, Guid uId)
    {
        Mocker.GetMock<IFeatureManager>()
            .Setup(f => f.IsEnabledAsync(FeatureFlags.UserNotifications))
            .ReturnsAsync(true);

        // Arrange

        var notificationType = "Action";
        var serviceResponse = new ServiceResponse<UserNotificationsResponse>
        {
            StatusCode = HttpStatusCode.BadGateway
        };

        Mocker.GetMock<IUserNotificationsService>()
            .Setup(s => s.GetUserNotification(It.IsAny<string>(), 1, 20,
                                             nameof(UserNotificationResponse.DateTimeCreated), SortDirections.Descending, notificationType))
            .ReturnsAsync(serviceResponse);

        var result = await Sut.UserNotificationsDashboard(notificationType, 1, 20, nameof(UserNotificationResponse.DateTimeCreated), SortDirections.Descending);

        var codeResult = result.ShouldBeOfType<StatusCodeResult>();
        codeResult.StatusCode.ShouldBe((int)HttpStatusCode.BadGateway);

        Mocker.GetMock<IUserNotificationsService>()
           .Verify(s => s.GetUserNotification(uId.ToString(), 1, 20,
                                             nameof(UserNotificationResponse.DateTimeCreated), SortDirections.Descending, notificationType), Times.Never);

        Mocker.GetMock<IUserNotificationsService>()
           .Verify(s => s.ReadUserNotifications(uId.ToString()), Times.Never);
    }
}