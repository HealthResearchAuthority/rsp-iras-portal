using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.ServiceClients;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationControllerTests;

public class StartProjectTests : TestServiceBase<ApplicationController>
{
    public StartProjectTests()
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

        var tempDataProvider = new Mock<ITempDataProvider>();

        Sut.TempData = new TempDataDictionary(httpContext, tempDataProvider.Object);
    }

    [Fact]
    public async Task StartProject_ReturnsView_WithModel_WhenValidationFails()
    {
        // Arrange
        var model = new IrasIdViewModel { IrasId = "1234" };
        Mocker
            .GetMock<IValidator<IrasIdViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<IrasIdViewModel>>(), default))
            .ReturnsAsync(new ValidationResult(new List<ValidationFailure>
            {
                new("IrasId", "Required")
            }));

        // Act
        var result = await Sut.StartProject(model);

        // Assert
        result.ShouldBeOfType<ViewResult>();
        Sut.ModelState["IrasId"]?.Errors.ShouldContain(e => e.ErrorMessage == "Required");
    }

    [Fact]
    public async Task StartProject_ReturnsView_WithModelError_WhenIrasIdStartsWithZero()
    {
        // Arrange
        var model = new IrasIdViewModel { IrasId = "0000" };

        Mocker
            .GetMock<IValidator<IrasIdViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<IrasIdViewModel>>(), default))
            .ReturnsAsync(new ValidationResult(new List<ValidationFailure>
            {
                new ValidationFailure("IrasId", "IRAS ID cannot start with '0'")
            }));

        // Act
        var result = await Sut.StartProject(model);

        // Assert
        result.ShouldBeOfType<ViewResult>();
        Sut.ModelState["IrasId"]?.Errors.ShouldContain(e => e.ErrorMessage == "IRAS ID cannot start with '0'");
    }

    [Fact]
    public async Task StartProject_ReturnsServiceError_IfGetApplicationsFails()
    {
        // Arrange
        var model = new IrasIdViewModel { IrasId = "1234" };

        Mocker
            .GetMock<IValidator<IrasIdViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<IrasIdViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplications())
            .ReturnsAsync(new ServiceResponse<IEnumerable<IrasApplicationResponse>>
            {
                StatusCode = HttpStatusCode.InternalServerError,
            });

        // Act
        var result = await Sut.StartProject(model);

        // Assert
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task StartProject_ReturnsView_IfIrasIdAlreadyExists()
    {
        // Arrange
        var model = new IrasIdViewModel { IrasId = "1234" };
        var existingApps = new List<IrasApplicationResponse>
        {
            new() { IrasId =1234, Id = "a1", Title = "Test" }
        };

        Mocker
            .GetMock<IValidator<IrasIdViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<IrasIdViewModel>>(), default))
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
        var result = await Sut.StartProject(model);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("prc:projectrecordexists");
    }

    [Fact]
    public async Task StartProject_ReturnsServiceError_IfCreateApplicationFails()
    {
        // Arrange
        var model = new IrasIdViewModel { IrasId = "9999" };

        Mocker
            .GetMock<IValidator<IrasIdViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<IrasIdViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplications())
            .ReturnsAsync(new ServiceResponse<IEnumerable<IrasApplicationResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        // HARP validation must succeed to reach creation branch
        Mocker
            .GetMock<IProjectRecordValidationService>()
            .Setup(s => s.ValidateProjectRecord(It.IsAny<int>()))
            .ReturnsAsync(new ServiceResponse<ProjectRecordValidationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectRecordValidationResponse()
            });

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.CreateApplication(It.IsAny<IrasApplicationRequest>()))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.OK });

        // Act
        var result = await Sut.StartProject(model);

        // Assert
        result.ShouldBeOfType<StatusCodeResult>();
    }

    [Fact]
    public async Task StartProject_RedirectsToResume_IfEverythingSucceeds()
    {
        // Arrange
        var model = new IrasIdViewModel { IrasId = "5678" };
        var createdApp = new IrasApplicationResponse { Id = "abc", IrasId = 5678, Title = "Test" };

        Mocker
            .GetMock<IValidator<IrasIdViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<IrasIdViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplications())
            .ReturnsAsync(new ServiceResponse<IEnumerable<IrasApplicationResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        // Ensure HARP validation passes
        Mocker
            .GetMock<IProjectRecordValidationService>()
            .Setup(s => s.ValidateProjectRecord(It.IsAny<int>()))
            .ReturnsAsync(new ServiceResponse<ProjectRecordValidationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectRecordValidationResponse()
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
            .GetMock<ICmsQuestionSetServiceClient>()
            .Setup(s => s.GetQuestionCategories())
            .ReturnsAsync(new ApiResponse<IEnumerable<CategoryDto>>(
                new HttpResponseMessage(HttpStatusCode.OK),
                new List<CategoryDto> { new() { CategoryId = "cat1" } },
                null));

        // Act
        var result = await Sut.StartProject(model);

        // Assert
        result.ShouldBeOfType<RedirectToActionResult>();
        var redirect = result as RedirectToActionResult;
        redirect!.ActionName.ShouldBe(nameof(QuestionnaireController.Resume));
        redirect.RouteValues!["projectRecordId"].ShouldBe("abc");
    }

    [Fact]
    public async Task StartProject_RedirectsToNotEligible_When_ValidationService_Returns_NotFound()
    {
        // Arrange
        var model = new IrasIdViewModel { IrasId = "7777" };

        Mocker
            .GetMock<IValidator<IrasIdViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<IrasIdViewModel>>(), default))
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
            .GetMock<IProjectRecordValidationService>()
            .Setup(s => s.ValidateProjectRecord(It.IsAny<int>()))
            .ReturnsAsync(new ServiceResponse<ProjectRecordValidationResponse>
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // Act
        var result = await Sut.StartProject(model);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("prc:projectnoteligible");
    }

    [Fact]
    public async Task StartProject_ReturnsServiceError_When_ValidationService_Fails_With_ServerError()
    {
        // Arrange
        var model = new IrasIdViewModel { IrasId = "8888" };

        Mocker
            .GetMock<IValidator<IrasIdViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<IrasIdViewModel>>(), default))
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
            .GetMock<IProjectRecordValidationService>()
            .Setup(s => s.ValidateProjectRecord(It.IsAny<int>()))
            .ReturnsAsync(new ServiceResponse<ProjectRecordValidationResponse>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var result = await Sut.StartProject(model);

        // Assert
        result.ShouldBeOfType<StatusCodeResult>();
        (result as StatusCodeResult)!.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }
}