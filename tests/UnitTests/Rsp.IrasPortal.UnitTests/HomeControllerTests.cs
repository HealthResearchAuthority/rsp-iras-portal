using Bogus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq.AutoMock;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;
using Shouldly;

namespace Rsp.IrasPortal.UnitTests;

public class HomeControllerTests
{
    private readonly AutoMocker _mocker;
    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        _mocker = new AutoMocker();
        _controller = _mocker.CreateInstance<HomeController>();
    }

    [Fact]
    public void Index_ReturnsViewResult()
    {
        // Act
        var result = _controller.Index();

        // Assert
        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public void Privacy_ReturnsViewResult()
    {
        // Act
        var result = _controller.Privacy();

        // Assert
        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public void Error_ReturnsViewResult()
    {
        // Arrange
        var bogusRequestId = new Faker().Random.AlphaNumeric(10);
        var expectedModel = new ErrorViewModel { RequestId = bogusRequestId };

        var httpContext = _mocker.GetMock<IHttpContextAccessor>();
        httpContext
            .SetupGet(x => x.HttpContext)
            .Returns(new DefaultHttpContext
            {
                TraceIdentifier = bogusRequestId
            });

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext.Object.HttpContext!
        };

        // Act
        var result = _controller.Error();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.ViewData.Model.ShouldBeOfType<ErrorViewModel>();
        model.RequestId.ShouldBe(expectedModel.RequestId);
    }
}