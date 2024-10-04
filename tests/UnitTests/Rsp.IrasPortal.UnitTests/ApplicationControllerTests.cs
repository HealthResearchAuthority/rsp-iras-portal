using Bogus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.AutoMock;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;
using Shouldly;

namespace Rsp.IrasPortal.UnitTests;

public class ApplicationControllerTests
{
    private readonly AutoMocker _mocker;
    private readonly ApplicationController _controller;

    public ApplicationControllerTests()
    {
        _mocker = new AutoMocker();
        _controller = _mocker.CreateInstance<ApplicationController>();
    }

    [Fact]
    public async Task Index_ReturnsViewResult()
    {
        // Arrange
        var response = new ServiceResponse<IEnumerable<IrasApplicationResponse>>
        {
            Content = [],
            StatusCode = System.Net.HttpStatusCode.OK
        };

        var applicationService = _mocker.GetMock<IApplicationsService>();
        applicationService
            .Setup(service => service.GetApplications())
            .ReturnsAsync(response);

        // Act
        var result = await Task.FromResult(_controller.Welcome());

        // Assert
        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public void Error_ReturnsViewResult()
    {
        // Arrange
        var bogusRequestId = new Faker().Random.AlphaNumeric(10);
        var expectedModel = new ErrorViewModel { RequestId = bogusRequestId };

        _controller.ControllerContext = new()
        {
            HttpContext = new DefaultHttpContext
            {
                TraceIdentifier = bogusRequestId
            }
        };

        // Act
        var result = _controller.Error();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.ViewData.Model.ShouldBeOfType<ErrorViewModel>();
        model.RequestId.ShouldBe(expectedModel.RequestId);
    }
}