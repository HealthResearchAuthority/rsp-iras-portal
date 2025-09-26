using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Features.ExceptionHandling.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Features.ExceptionHandling;

public class HandleStatusCodeTests : TestServiceBase<ExceptionHandlingController>
{
    private class TestStatusCodeReExecuteFeature : IStatusCodeReExecuteFeature
    {
        public string? OriginalPath { get; set; }
        public string? OriginalQueryString { get; set; }
        public string? OriginalPathBase { get; set; }
    }

    [Fact]
    public void HandleStatusCode_With_ProblemDetails_Returns_Error_View_And_Logs()
    {
        // arrange
        var loggerMock = Mocker.GetMock<ILogger<ExceptionHandlingController>>();
        var ctx = PrepareHttpContext();
        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = "Test Problem",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "Some detail"
        };

        ctx.Items[ContextItemKeys.ProblemDetails] = problem;
        ctx.Features.Set<IStatusCodeReExecuteFeature>(new TestStatusCodeReExecuteFeature { OriginalPath = "/original/path" });

        // act
        var result = Sut.HandleStatusCode(StatusCodes.Status500InternalServerError);

        // assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe("Error");
        VerifyAtLeastOneErrorLog(loggerMock);
    }

    [Fact]
    public void HandleStatusCode_404_Without_ProblemDetails_Returns_NotFound_View_And_Logs()
    {
        // arrange
        var loggerMock = Mocker.GetMock<ILogger<ExceptionHandlingController>>();
        var ctx = PrepareHttpContext();
        ctx.Features.Set<IStatusCodeReExecuteFeature>(new TestStatusCodeReExecuteFeature { OriginalPath = "/missing/resource" });

        // act
        var result = Sut.HandleStatusCode(StatusCodes.Status404NotFound);

        // assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe("NotFound");
        VerifyAtLeastOneErrorLog(loggerMock);
    }

    [Fact]
    public void HandleStatusCode_403_Without_ProblemDetails_Returns_Forbidden_Default_View_And_Logs()
    {
        // arrange
        var loggerMock = Mocker.GetMock<ILogger<ExceptionHandlingController>>();
        PrepareHttpContext();

        // act
        var result = Sut.HandleStatusCode(StatusCodes.Status403Forbidden);

        // assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBeNull();
        loggerMock.Verify(logger => logger.IsEnabled(LogLevel.Error), Times.AtLeastOnce());
    }

    [Fact]
    public void HandleStatusCode_500_Without_ProblemDetails_Falls_Back_To_Error_Default_View_And_Logs()
    {
        // arrange
        var loggerMock = Mocker.GetMock<ILogger<ExceptionHandlingController>>();
        PrepareHttpContext();

        // act
        var result = Sut.HandleStatusCode(StatusCodes.Status500InternalServerError);

        // assert
        var view = result.ShouldBeOfType<ViewResult>();
        view.ViewName.ShouldBe("Error");
        VerifyAtLeastOneErrorLog(loggerMock);
    }

    private DefaultHttpContext PrepareHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return httpContext;
    }

    private static void VerifyAtLeastOneErrorLog(Mock<ILogger<ExceptionHandlingController>> loggerMock)
    {
        loggerMock.Verify(logger => logger.IsEnabled(LogLevel.Error), Times.AtLeastOnce());
    }
}