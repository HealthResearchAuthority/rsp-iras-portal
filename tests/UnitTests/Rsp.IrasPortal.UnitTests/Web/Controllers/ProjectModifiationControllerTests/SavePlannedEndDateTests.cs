using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ProjectModifiationControllerTests;

public class SavePlannedEndDateTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public async Task SavePlannedEndDate_ReturnsViewWithErrors_WhenDateIsInvalid()
    {
        // Arrange
        var model = new PlannedEndDateViewModel
        {
            NewPlannedEndDate = new DateViewModel { Day = "1", Month = "1", Year = "2020" }
        };

        var validationResult = new ValidationResult(new[] { new ValidationFailure("NewPlannedEndDate.Date", "Invalid date") });

        Mocker
            .GetMock<IValidator<DateViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<DateViewModel>>(), default))
            .ReturnsAsync(validationResult);

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        // Act
        var result = await Sut.SavePlannedEndDate(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("PlannedEndDate");
        viewResult.Model.ShouldBe(model);
        Sut.ModelState.ContainsKey("NewPlannedEndDate.Date").ShouldBeTrue();
    }

    [Fact]
    public async Task SavePlannedEndDate_ReturnsErrorView_WhenOriginalResponseNotFound()
    {
        // Arrange
        var model = new PlannedEndDateViewModel
        {
            NewPlannedEndDate = new DateViewModel { Day = "1", Month = "1", Year = "2020" }
        };

        var validationResult = new ValidationResult();

        Mocker
            .GetMock<IValidator<DateViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<DateViewModel>>(), default))
            .ReturnsAsync(validationResult);

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordResponses] = JsonSerializer.Serialize(new List<RespondentAnswerDto>())
        };

        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.RespondentId] = "respId";

        // Act
        var result = await Sut.SavePlannedEndDate(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Error");
        var problem = viewResult.Model.ShouldBeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problem.Detail.ShouldContain("Couldn't find the original response");
        problem.Status.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task SavePlannedEndDate_ReturnsServiceError_WhenSaveFails()
    {
        // Arrange
        var model = new PlannedEndDateViewModel
        {
            NewPlannedEndDate = new DateViewModel { Day = "1", Month = "1", Year = "2020" }
        };
        var respondentAnswer = new RespondentAnswerDto
        {
            QuestionId = "QID",
            VersionId = "VID",
            SectionId = "SID"
        };

        var validationResult = new ValidationResult();

        Mocker
            .GetMock<IValidator<DateViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<DateViewModel>>(), default))
            .ReturnsAsync(validationResult);

        var failResponse = new ServiceResponse { StatusCode = HttpStatusCode.InternalServerError };

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.SaveModificationAnswers(It.IsAny<ProjectModificationAnswersRequest>()))
            .ReturnsAsync(failResponse);

        Mocker
            .GetMock<ProjectModificationController>()
            .CallBase = true;

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordResponses] = JsonSerializer.Serialize(new List<RespondentAnswerDto> { respondentAnswer }),
            [TempDataKeys.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "recId"
        };

        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.RespondentId] = "respId";

        // Act
        var result = await Sut.SavePlannedEndDate(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Error");
    }

    [Fact]
    public async Task SavePlannedEndDate_RedirectsToProjectOverview_WhenSuccessful()
    {
        // Arrange
        var model = new PlannedEndDateViewModel
        {
            NewPlannedEndDate = new DateViewModel { Day = "1", Month = "1", Year = "2020" }
        };

        var respondentAnswer = new RespondentAnswerDto
        {
            QuestionId = QuestionIds.ProjectPlannedEndDate, // Use correct QuestionId
            VersionId = "VID",
            SectionId = "SID"
        };

        var validationResult = new ValidationResult();

        Mocker
            .GetMock<IValidator<DateViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<DateViewModel>>(), default))
            .ReturnsAsync(validationResult);

        var okResponse = new ServiceResponse { StatusCode = HttpStatusCode.OK };

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.SaveModificationAnswers(It.IsAny<ProjectModificationAnswersRequest>()))
            .ReturnsAsync(okResponse);

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordResponses] = JsonSerializer.Serialize(new List<RespondentAnswerDto> { respondentAnswer }),
            [TempDataKeys.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "recId"
        };

        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.RespondentId] = "respId";

        // Act
        var result = await Sut.SavePlannedEndDate(model);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("app:projectoverview");
    }
}