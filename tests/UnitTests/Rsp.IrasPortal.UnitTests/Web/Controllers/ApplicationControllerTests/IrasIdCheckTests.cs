﻿using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationControllerTests;

public class IrasIdCheckTests : TestServiceBase<ApplicationController>
{
    public IrasIdCheckTests()
    {
        var mockSession = new Mock<ISession>();

        var httpContext = new DefaultHttpContext
        {
            Session = mockSession.Object
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task IrasIdCheck_ReturnsView_WithModel_WhenValidationFails()
    {
        // Arrange
        var model = new IrasIdCheckViewModel { IrasId = "1234" };
        Mocker
            .GetMock<IValidator<IrasIdCheckViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<IrasIdCheckViewModel>>(), default))
            .ReturnsAsync(new ValidationResult(new List<ValidationFailure>
            {
                new("IrasId", "Required")
            }));

        // Act
        var result = await Sut.IrasIdCheck(model);

        // Assert
        result.ShouldBeOfType<ViewResult>();
        Sut.ModelState["IrasId"]?.Errors.ShouldContain(e => e.ErrorMessage == "Required");
    }

    [Fact]
    public async Task IrasIdCheck_ReturnsServiceError_IfGetApplicationsFails()
    {
        // Arrange
        var model = new IrasIdCheckViewModel { IrasId = "1234" };

        Mocker
            .GetMock<IValidator<IrasIdCheckViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<IrasIdCheckViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplications())
            .ReturnsAsync(new ServiceResponse<IEnumerable<IrasApplicationResponse>>
            {
                StatusCode = HttpStatusCode.InternalServerError,
            });

        // Act
        var result = await Sut.IrasIdCheck(model);

        // Assert
        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task IrasIdCheck_ReturnsView_IfIrasIdAlreadyExists()
    {
        // Arrange
        var model = new IrasIdCheckViewModel { IrasId = "1234" };
        var existingApps = new List<IrasApplicationResponse>
        {
            new() { IrasId = 1234, ApplicationId = "a1", Title = "Test" }
        };

        Mocker
            .GetMock<IValidator<IrasIdCheckViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<IrasIdCheckViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplications())
            .ReturnsAsync(new ServiceResponse<IEnumerable<IrasApplicationResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = existingApps
            });

        // Act
        var result = await Sut.IrasIdCheck(model);

        // Assert
        result.ShouldBeOfType<ViewResult>();
        Sut.ModelState[nameof(model.IrasId)]?.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task IrasIdCheck_ReturnsServiceError_IfCreateApplicationFails()
    {
        // Arrange
        var model = new IrasIdCheckViewModel { IrasId = "9999" };

        Mocker
            .GetMock<IValidator<IrasIdCheckViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<IrasIdCheckViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplications())
            .ReturnsAsync(new ServiceResponse<IEnumerable<IrasApplicationResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.CreateApplication(It.IsAny<IrasApplicationRequest>()))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.OK });

        // Act
        var result = await Sut.IrasIdCheck(model);

        // Assert
        result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task IrasIdCheck_RedirectsToResume_IfEverythingSucceeds()
    {
        // Arrange
        var mockSession = new Mock<ISession>();

        var httpContext = new DefaultHttpContext
        {
            Session = mockSession.Object
        };

        var model = new IrasIdCheckViewModel { IrasId = "5678" };
        var createdApp = new IrasApplicationResponse { ApplicationId = "abc", IrasId = 5678, Title = "Test" };

        Mocker
            .GetMock<IValidator<IrasIdCheckViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<IrasIdCheckViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplications())
            .ReturnsAsync(new ServiceResponse<IEnumerable<IrasApplicationResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.CreateApplication(It.IsAny<IrasApplicationRequest>()))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = createdApp
            });

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(s => s.GetQuestionCategories())
            .ReturnsAsync(new ServiceResponse<IEnumerable<CategoryDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = [new() { CategoryId = "cat1" }]
            });

        // Act
        var result = await Sut.IrasIdCheck(model);

        // Assert
        result.ShouldBeOfType<RedirectToActionResult>();
        var redirect = result as RedirectToActionResult;
        redirect!.ActionName.ShouldBe(nameof(QuestionnaireController.Resume));
        redirect.RouteValues!["ApplicationId"].ShouldBe("abc");
    }
}