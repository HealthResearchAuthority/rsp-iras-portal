using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationControllerTests;

public class DeleteProjectTests : TestServiceBase<ApplicationController>
{
    public DeleteProjectTests()
    {
        var mockSession = new Mock<ISession>();
        var httpContext = new DefaultHttpContext { Session = mockSession.Object };
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        var tempDataProvider = new Mock<ITempDataProvider>();
        Sut.TempData = new TempDataDictionary(httpContext, tempDataProvider.Object);
    }

    [Fact]
    public async Task DeleteProject_SuccessfulDelete_RedirectsAndSetsBanner()
    {
        // Arrange
        var projectRecordId = "123";
        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.DeleteProject(projectRecordId))
            .ReturnsAsync(new ServiceResponse<object> { StatusCode = System.Net.HttpStatusCode.OK });

        // Act
        var result = await Sut.DeleteProject(projectRecordId);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToRouteResult>();
        redirectResult.RouteName.ShouldBe("app:welcome");
        Sut.TempData[TempDataKeys.ShowProjectDeletedBanner].ShouldBe(true);
    }

    [Fact]
    public async Task DeleteProject_FailedDelete_ReturnsServiceError()
    {
        // Arrange
        var projectRecordId = "123";
        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.DeleteProject(projectRecordId))
            .ReturnsAsync(new ServiceResponse<object> { StatusCode = System.Net.HttpStatusCode.InternalServerError });

        // Act
        var result = await Sut.DeleteProject(projectRecordId);

        // Assert
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }
}