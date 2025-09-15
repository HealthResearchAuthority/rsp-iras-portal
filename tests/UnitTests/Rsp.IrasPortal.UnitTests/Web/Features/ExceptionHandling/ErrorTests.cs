using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rsp.IrasPortal.Web.Features.ExceptionHandling.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Features.ExceptionHandling;

public class ErrorTests : TestServiceBase<ExceptionHandlingController>
{
    private void PrepareHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public void Error_Returns_Default_View_And_Does_Not_Log()
    {
        // arrange
        PrepareHttpContext();
        var loggerMock = Mocker.GetMock<ILogger<ExceptionHandlingController>>();

        // act
        var result = Sut.Error();

        // assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBeNull(); // default view
        loggerMock.Invocations.Count.ShouldBe(0);
    }
}