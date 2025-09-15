using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rsp.IrasPortal.Web.Features.ExceptionHandling.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Features.ExceptionHandling;

public class ForbiddenTests : TestServiceBase<ExceptionHandlingController>
{
    private void PrepareHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public void Forbidden_Returns_Default_View_And_Logs()
    {
        // arrange
        var loggerMock = Mocker.GetMock<ILogger<ExceptionHandlingController>>();
        PrepareHttpContext();

        // act
        var result = Sut.Forbidden();

        // assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBeNull();
        loggerMock.Verify(logger => logger.IsEnabled(LogLevel.Error), Times.AtLeastOnce());
    }
}