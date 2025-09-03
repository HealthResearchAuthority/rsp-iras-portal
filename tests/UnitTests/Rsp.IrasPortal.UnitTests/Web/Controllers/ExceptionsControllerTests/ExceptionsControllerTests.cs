using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rsp.IrasPortal.Web.Controllers.Exceptions;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ExceptionsControllerTests;

public class ExceptionsControllerTests
{
    [Fact]
    public void Index_ShouldSetViewDataAndReturnView()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ExceptionController>>();
        var controller = new ExceptionController(loggerMock.Object);

        var exceptionId = "test-exception-id";

        // Act
        var result = controller.Index(exceptionId);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(exceptionId, controller.ViewData["exceptionId"]);
    }

    [Fact]
    public void ServiceException_ShouldLogAndRedirectToIndex()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ExceptionController>>();
        var controller = new ExceptionController(loggerMock.Object);

        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = "Service Error",
            Detail = "Something went wrong",
            Status = 500
        };

        // Act
        var result = controller.ServiceException(problemDetails);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.True(redirectResult.RouteValues!.ContainsKey("exceptionId"));

        loggerMock.Verify
            (
            l => l.Log
                (
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Service exception occurred")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
            );
    }
}