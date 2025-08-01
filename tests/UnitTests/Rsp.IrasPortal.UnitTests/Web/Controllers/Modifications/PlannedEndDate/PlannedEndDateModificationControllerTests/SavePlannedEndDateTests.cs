using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;
using Shouldly;
using Xunit;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.Modifications.PlannedEndDate.PlannedEndDateModificationControllerTests;

public class SavePlannedEndDateTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public async Task SavePlannedEndDate_ReturnsViewWithErrors_WhenValidationFails()
    {
        // Arrange
        var model = new PlannedEndDateViewModel
        {
            NewPlannedEndDate = new DateViewModel { Day = "1", Month = "1", Year = "2020" }
        };
        var validationResult = new ValidationResult(new[] { new ValidationFailure("NewPlannedEndDate.Date", "Invalid date") });
        Mocker.GetMock<IValidator<DateViewModel>>()
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
        Mocker.GetMock<IValidator<DateViewModel>>()
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
    }
}
