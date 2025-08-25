using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rsp.IrasPortal.Infrastructure.ExceptionHandlers;

namespace Rsp.IrasPortal.UnitTests.Infrastructure.GlobalExceptionHandlerTests;

public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ShouldRedirectAndLog_WhenExceptionOccurs()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<GlobalExceptionHandler>>();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRouting();
        var provider = services.BuildServiceProvider();
        var linkGenerator = provider.GetRequiredService<LinkGenerator>();

        var handler = new GlobalExceptionHandler(loggerMock.Object, linkGenerator);

        var context = new DefaultHttpContext();
        context.Request.Path = "/test";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var exception = new Exception("Something went wrong");

        // Act
        var result = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnFalse_WhenStatusCodeIs404()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<GlobalExceptionHandler>>();
        var linkGeneratorMock = new Mock<LinkGenerator>();
        var handler = new GlobalExceptionHandler(loggerMock.Object, linkGeneratorMock.Object);

        var context = new DefaultHttpContext();
        context.Response.StatusCode = StatusCodes.Status404NotFound;

        var exception = new Exception("Not Found");

        // Act
        var result = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.False(result);
        loggerMock.Verify
            (
            l => l.Log
                (
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Never
            );
    }
}