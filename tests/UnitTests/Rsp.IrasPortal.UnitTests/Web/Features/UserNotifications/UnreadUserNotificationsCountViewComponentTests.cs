using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.FeatureManagement;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.UserNotifications.ViewComponents;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.UnitTests;

namespace Rsp.IrasPortal.UnitTests.Web.Features.UserNotifications;

public class UnreadUserNotificationsCountViewComponentTests : TestServiceBase<UnreadUserNotificationsCountViewComponent>
{
    private readonly DefaultHttpContext _http;

    public UnreadUserNotificationsCountViewComponentTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ViewComponentContext = new ViewComponentContext
        {
            ViewContext = new ViewContext
            {
                HttpContext = _http
            }
        };
    }

    [Theory, AutoData]
    public async Task Return_View_When_Sucesful(Guid uId)
    {
        _http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("userId", uId.ToString())
        }, "TestAuth"));

        Mocker.GetMock<IFeatureManager>()
            .Setup(f => f.IsEnabledAsync(FeatureFlags.UserNotifications))
            .ReturnsAsync(true);

        // Arrange
        var serviceResponse = new ServiceResponse<int>
        {
            StatusCode = HttpStatusCode.OK,
            Content = 5
        };

        Mocker.GetMock<IUserNotificationsService>()
            .Setup(s => s.GetUnreadUserNotificationsCount(uId.ToString()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.InvokeAsync();

        // Assert
        var componentResult = result.ShouldBeOfType<ViewViewComponentResult>();

        // Verify
        var model = componentResult?.ViewData?.Model.ShouldBeOfType<int>();
        model.ShouldBe(5);

        Mocker.GetMock<IUserNotificationsService>()
            .Verify(s => s.GetUnreadUserNotificationsCount(uId.ToString()), Times.Once);
    }

    [Theory, AutoData]
    public async Task Return_View_With_Zero_Results_When_Not_Sucesful(Guid uId)
    {
        _http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("userId", uId.ToString())
        }, "TestAuth"));

        Mocker.GetMock<IFeatureManager>()
            .Setup(f => f.IsEnabledAsync(FeatureFlags.UserNotifications))
            .ReturnsAsync(true);

        // Arrange
        var serviceResponse = new ServiceResponse<int>
        {
            StatusCode = HttpStatusCode.NotAcceptable
        };

        Mocker.GetMock<IUserNotificationsService>()
            .Setup(s => s.GetUnreadUserNotificationsCount(uId.ToString()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.InvokeAsync();

        // Assert
        var componentResult = result.ShouldBeOfType<ViewViewComponentResult>();

        // Verify
        var model = componentResult?.ViewData?.Model.ShouldBeOfType<int>();
        model.ShouldBe(0);

        Mocker.GetMock<IUserNotificationsService>()
            .Verify(s => s.GetUnreadUserNotificationsCount(uId.ToString()), Times.Once);
    }
}