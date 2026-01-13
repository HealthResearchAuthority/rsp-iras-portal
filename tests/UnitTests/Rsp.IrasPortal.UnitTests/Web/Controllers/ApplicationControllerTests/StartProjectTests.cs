using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.CmsQuestionset; // added for SectionModel
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services; // removed ServiceClients usage
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Features.ProjectRecord.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.ApplicationControllerTests;

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
            new() { IrasId =1234, Id = "a1", ShortProjectTitle = "Test" }
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
    public async Task StartProject_ReturnsServiceError_IfQuestionSetHasNoSections()
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

        // HARP validation must succeed to reach question set branch
        Mocker
            .GetMock<IProjectRecordValidationService>()
            .Setup(s => s.ValidateProjectRecord(It.IsAny<int>()))
            .ReturnsAsync(new ServiceResponse<ProjectRecordValidationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ProjectRecordValidationResponse()
            });

        // Question set service returns empty sections provoking service error (400)
        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetQuestionSet(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { Sections = [] }
            });

        // Act
        var result = await Sut.StartProject(model);

        // Assert
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task StartProject_RedirectsToFirstSectionRoute_IfEverythingSucceeds()
    {
        // Arrange
        var model = new IrasIdViewModel { IrasId = "5678" };

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
                Content = new ProjectRecordValidationResponse() // Data can be null, serialization handles it
            });

        // Question set with one section to redirect to
        var section = new SectionModel
        {
            Id = "section-1",
            StaticViewName = "confirmprojectdetails"
        };

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetQuestionSet(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse
                {
                    Sections = new List<SectionModel> { section }
                }
            });

        // Act
        var result = await Sut.StartProject(model);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe($"prc:{section.StaticViewName}");
        redirect.RouteValues!["sectionId"].ShouldBe(section.Id);
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