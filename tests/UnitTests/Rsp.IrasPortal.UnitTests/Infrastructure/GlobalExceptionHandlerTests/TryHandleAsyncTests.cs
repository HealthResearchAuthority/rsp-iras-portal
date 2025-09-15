using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Rsp.IrasPortal.Infrastructure.ExceptionHandlers;

namespace Rsp.IrasPortal.UnitTests.Infrastructure.GlobalExceptionHandlerTests;

public class TryHandleAsyncTests : TestServiceBase<GlobalExceptionHandler>
{
    [Fact]
    public async Task TryHandleAsync_ShouldRedirectAndLog_WhenExceptionOccurs()
    {
        // Arrange
        //var loggerMock = new Mock<ILogger<GlobalExceptionHandler>>();

        //var services = new ServiceCollection();
        //services.AddLogging();
        //services.AddRouting();
        //var provider = services.BuildServiceProvider();
        //var linkGenerator = provider.GetRequiredService<LinkGenerator>();

        //var handler = new GlobalExceptionHandler(loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Request.Path = "/test";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var exception = new Exception("Something went wrong");

        // Act
        var result = await Sut.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        result.ShouldBeFalse();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);

        var logger = Mocker.GetMock<ILogger<GlobalExceptionHandler>>();

        logger.Verify(logger => logger.IsEnabled(It.IsAny<LogLevel>()), Times.Once());
    }
}