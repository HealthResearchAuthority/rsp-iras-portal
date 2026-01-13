using System.Security.Claims;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationControllerTests;

public class ProjectClosureTests : TestServiceBase<ApplicationController>
{
    private readonly Mock<IApplicationsService> _applicationsService = new();
    private readonly Mock<IProjectClosuresService> _projectClosuresService = new();

    public ProjectClosureTests()
    {
        // Setup TempData
        var httpContext = new DefaultHttpContext { Session = new InMemorySession() };
        httpContext.User = new ClaimsPrincipal(
        new ClaimsIdentity());
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.IrasId] = "123456",
            [TempDataKeys.ShortProjectTitle] = "Test Project"
        };
        Sut.TempData = tempData;
        var givenName = "Jane";
        var familyName = "Doe";
        // User claims so GetRespondentFromContext returns deterministic name
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("given_name", givenName),
            new Claim("family_name", familyName)
        }, "TestAuth");
        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }

    [Fact]
    public async Task CloseProject_ShouldReturnValidateProjectClosureView_WhenModificationsInTransactionState()
    {
        // Arrange
        var projectRecordId = "123456";

        Mocker
            .GetMock<IProjectModificationsService>()
            .Setup(s => s.GetModificationsForProject(It.IsAny<string>(), It.IsAny<ModificationSearchRequest>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<GetModificationsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new() { TotalCount = 1, Modifications = [new ModificationsDto { Id = Guid.NewGuid().ToString(), ModificationId = "MOD1", ModificationType = "Type", ReviewType = "Review", Category = "A", Status = ModificationStatus.InDraft }] }
            });

        // Act
        var result = await Sut.CloseProject(projectRecordId);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("/Features/ProjectOverview/Views/ValidateProjectClosure.cshtml", viewResult.ViewName);
        Assert.Null(viewResult.Model);
    }

    [Fact]
    public async Task CloseProject_ShouldReturnCloseProjectView_WhenNoModificationsInTransactionState()
    {
        // Arrange
        var projectRecordId = "123";

        Mocker
           .GetMock<IProjectModificationsService>()
           .Setup(s => s.GetModificationsForProject(It.IsAny<string>(), It.IsAny<ModificationSearchRequest>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
           .ReturnsAsync(new ServiceResponse<GetModificationsResponse>
           {
               StatusCode = HttpStatusCode.OK,
               Content = new()
               {
                   TotalCount = 3,
                   Modifications = new List<ModificationsDto>
                    {
                new() { Id = "ABC", ProjectRecordId = "PR-1", ModificationId = "100/1", ShortProjectTitle = "One", CreatedAt = DateTime.UtcNow, Status = "Approved" },
                new() { Id = "def", ProjectRecordId = "PR-2", ModificationId = "100/2", ShortProjectTitle = "Two", CreatedAt = DateTime.UtcNow, Status = "Not approved" },
                new() { Id = "xyz", ProjectRecordId = "PR-3", ModificationId = "100/3", ShortProjectTitle = "Three", CreatedAt = DateTime.UtcNow, Status = "Approved" },
                    }
               }
           });

        // Act
        var result = await Sut.CloseProject(projectRecordId);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ProjectClosuresModel>(viewResult.Model);
        Assert.Equal("/Features/ProjectOverview/Views/CloseProject.cshtml", viewResult.ViewName);
        Assert.Equal(projectRecordId, model.ProjectRecordId);
        Assert.Equal("Test Project", model.ShortProjectTitle);
    }

    [Fact]
    public async Task ConfirmProjectClosure_WhenValidationFails_RedirectsToCloseProject_AndSetsTempData()
    {
        // Arrange
        var model = ValidModel();
        var plannedEndDate = new DateTime(2025, 02, 28);

        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure("ClosureDate", "The date should be a valid date and it must be present or past date"),
        });

        SetupValidatorResult(validationResult);

        // Act
        var result = await Sut.ConfirmProjectClosure(model, plannedEndDate);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        redirect.ActionName.ShouldBe(nameof(ApplicationController.CloseProject));
        redirect.RouteValues!["projectRecordId"].ShouldBe(model.ProjectRecordId);

        // No service calls in validation failure branch
        _projectClosuresService.Verify(s => s.CreateProjectClosure(It.IsAny<ProjectClosureRequest>()), Times.Never);
        _applicationsService.Verify(s => s.UpdateProjectRecordStatus(It.IsAny<IrasApplicationRequest>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmProjectClosure_WhenCreateClosureFails_ReturnsServiceError()
    {
        // Arrange
        SetupValidatorResult(new ValidationResult());
        var model = ValidModel();
        var plannedEndDate = new DateTime(2025, 02, 28);

        Mocker.GetMock<IProjectClosuresService>()
            .Setup(s => s.CreateProjectClosure(It.IsAny<ProjectClosureRequest>()))
            .ReturnsAsync(new ServiceResponse<ProjectClosuresResponse>
            {
                StatusCode = HttpStatusCode.BadGateway,
                Error = "Create project closure failed"
            });

        // Act
        _ = await Sut.ConfirmProjectClosure(model, plannedEndDate);
        //Assert
        Mocker.GetMock<IProjectClosuresService>().Verify(s => s.CreateProjectClosure(It.IsAny<ProjectClosureRequest>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task ConfirmProjectClosure_WhenUpdateProjectRecordStatusFails_ReturnsServiceError(ProjectClosuresResponse closuresResponse, IrasApplicationResponse irasApplicationResponse)
    {
        // Arrange
        SetupValidatorResult(new ValidationResult());
        var model = ValidModel();
        var plannedEndDate = new DateTime(2025, 02, 28);

        Mocker.GetMock<IProjectClosuresService>()
             .Setup(s => s.CreateProjectClosure(It.IsAny<ProjectClosureRequest>()))
             .ReturnsAsync(new ServiceResponse<ProjectClosuresResponse>
             {
                 StatusCode = HttpStatusCode.OK,
                 Content = closuresResponse
             });

        // Get project record
        Mocker.GetMock<IApplicationsService>()
           .Setup(s => s.GetProjectRecord(It.IsAny<string>()))
           .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.OK, Content = irasApplicationResponse });

        // Update status fails
        Mocker.GetMock<IApplicationsService>()
            .Setup(s => s.UpdateProjectRecordStatus(It.IsAny<IrasApplicationRequest>()))
            .ReturnsAsync(new ServiceResponse
            {
                StatusCode = HttpStatusCode.BadGateway,
                Error = "Update status failed"
            });

        // Act
        _ = await Sut.ConfirmProjectClosure(model, plannedEndDate);
        //Assert
        Mocker.GetMock<IProjectClosuresService>().Verify(s => s.CreateProjectClosure(It.IsAny<ProjectClosureRequest>()), Times.Once);
        Mocker.GetMock<IApplicationsService>().Verify(s => s.UpdateProjectRecordStatus(It.IsAny<IrasApplicationRequest>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task ConfirmProjectClosure_WhenCreateClosurePass_UpdateProjectRecordStatus(ProjectClosuresResponse closuresResponse, IrasApplicationResponse irasApplicationResponse)
    {
        // Arrange
        SetupValidatorResult(new ValidationResult());
        var model = ValidModel();
        var plannedEndDate = new DateTime(2025, 02, 28);

        Mocker.GetMock<IProjectClosuresService>()
            .Setup(s => s.CreateProjectClosure(It.IsAny<ProjectClosureRequest>()))
            .ReturnsAsync(new ServiceResponse<ProjectClosuresResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = closuresResponse
            });

        // Get project record
        Mocker.GetMock<IApplicationsService>()
           .Setup(s => s.GetProjectRecord(It.IsAny<string>()))
           .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.OK, Content = irasApplicationResponse });

        // Update status fails
        Mocker.GetMock<IApplicationsService>()
            .Setup(s => s.UpdateProjectRecordStatus(It.IsAny<IrasApplicationRequest>()))
            .ReturnsAsync(new ServiceResponse
            {
                StatusCode = HttpStatusCode.OK,
            });

        // Act
        var result = await Sut.ConfirmProjectClosure(model, plannedEndDate);
        // Assert
        var view = Assert.IsType<ViewResult>(result);
        view.ViewName.ShouldBe("/Features/ProjectOverview/Views/ConfirmProjectClosure.cshtml");

        Mocker.GetMock<IProjectClosuresService>().Verify(s => s.CreateProjectClosure(It.IsAny<ProjectClosureRequest>()), Times.Once);
        Mocker.GetMock<IApplicationsService>().Verify(s => s.UpdateProjectRecordStatus(It.IsAny<IrasApplicationRequest>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task ConfirmProjectClosure_GetProjectRecordById_Fails_Return_Response(ProjectClosuresResponse closuresResponse, IrasApplicationResponse irasApplicationResponse)
    {
        // Arrange
        SetupValidatorResult(new ValidationResult());
        var model = ValidModel();
        var plannedEndDate = new DateTime(2025, 02, 28);

        Mocker.GetMock<IProjectClosuresService>()
            .Setup(s => s.CreateProjectClosure(It.IsAny<ProjectClosureRequest>()))
            .ReturnsAsync(new ServiceResponse<ProjectClosuresResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = closuresResponse
            });

        // Get project record
        Mocker
        .GetMock<IApplicationsService>()
            .Setup(s => s.GetProjectRecord(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var result = await Sut.ConfirmProjectClosure(model, plannedEndDate);

        // Assert
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);

        Mocker.GetMock<IProjectClosuresService>().Verify(s => s.CreateProjectClosure(It.IsAny<ProjectClosureRequest>()), Times.Once);
    }

    private void SetupValidatorResult(ValidationResult result)
    {
        var mockValidator = Mocker.GetMock<IValidator<ProjectClosuresModel>>();
        mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ProjectClosuresModel>(), default))
            .ReturnsAsync(result);
    }

    private static ProjectClosuresModel ValidModel() => new()
    {
        ClosureDate = DateTime.UtcNow.AddDays(-2),
        DateActioned = DateTime.UtcNow,
        IrasId = 123456,
        ProjectRecordId = "PR-1",
        ShortProjectTitle = "Test",
        Status = "With sponsor",
        SentToSponsorDate = DateTime.UtcNow,
        ActualClosureDate = new DateViewModel() { Day = "01", Month = "01", Year = "2026" }
    };
}