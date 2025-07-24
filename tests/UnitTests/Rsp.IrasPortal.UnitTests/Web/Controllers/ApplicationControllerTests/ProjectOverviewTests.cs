using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationControllerTests;

public class ProjectOverview : TestServiceBase<ApplicationController>
{
    [Fact]
    public async Task ProjectOverview_UsesTempData_AndReturnsViewResult()
    {
        // Arrange
        var tempDataProvider = new Mock<ITempDataProvider>();
        var answers = new List<RespondentAnswerDto>
            {
                new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project X" },
                new() { QuestionId = QuestionIds.ProjectPlannedEndDate, AnswerText = "01/01/2025" }
            };
        var respondentService = Mocker.GetMock<IRespondentService>();
        respondentService
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = answers });

        // Ensure HttpContext/Request is set up
        var httpContext = new DefaultHttpContext();

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        Sut.TempData = new TempDataDictionary(httpContext, tempDataProvider.Object)
        {
            [TempDataKeys.ShortProjectTitle] = "Test Project",
            [TempDataKeys.CategoryId] = QuestionCategories.ProjectRecrod,
            [TempDataKeys.ProjectRecordId] = "456"
        };

        // Act
        var result = await Sut.ProjectOverview(null, null);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<ProjectOverviewModel>();

        model.ProjectTitle.ShouldBe("Test Project");
        model.CategoryId.ShouldBe(QuestionCategories.ProjectRecrod);
        model.ProjectRecordId.ShouldBe("456");
    }

    [Fact]
    public async Task ProjectOverview_SetsNotificationBanner_WhenProjectModificationIdExists()
    {
        // Arrange
        var tempDataProvider = new Mock<ITempDataProvider>();
        var answers = new List<RespondentAnswerDto>
            {
                new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project X" },
                new() { QuestionId = QuestionIds.ProjectPlannedEndDate, AnswerText = "01/01/2025" }
            };
        var respondentService = Mocker.GetMock<IRespondentService>();
        respondentService
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = answers });

        // Ensure HttpContext/Request is set up
        var httpContext = new DefaultHttpContext();

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var tempData = new TempDataDictionary(new DefaultHttpContext(), tempDataProvider.Object)
        {
            [TempDataKeys.ProjectModificationId] = "mod-1"
        };
        Sut.TempData = tempData;

        // Act
        await Sut.ProjectOverview(null, null);

        // Assert
        tempData[TempDataKeys.ShowNotificationBanner].ShouldBe(true);
    }

    [Fact]
    public async Task ProjectOverview_RemovesModificationRelatedTempDataKeys()
    {
        // Arrange
        var tempDataProvider = new Mock<ITempDataProvider>();
        var answers = new List<RespondentAnswerDto>
            {
                new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project X" },
                new() { QuestionId = QuestionIds.ProjectPlannedEndDate, AnswerText = "01/01/2025" }
            };
        var respondentService = Mocker.GetMock<IRespondentService>();
        respondentService
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = answers });

        // Ensure HttpContext/Request is set up
        var httpContext = new DefaultHttpContext();

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var tempData = new TempDataDictionary(new DefaultHttpContext(), tempDataProvider.Object)
        {
            [TempDataKeys.ProjectModificationId] = "mod-1",
            [TempDataKeys.ProjectModificationIdentifier] = "ident-1",
            [TempDataKeys.ProjectModificationChangeId] = "chg-1",
            [TempDataKeys.ProjectModificationSpecificArea] = "area-1"
        };
        Sut.TempData = tempData;

        // Act
        await Sut.ProjectOverview(null, null);

        // Assert
        tempData.ContainsKey(TempDataKeys.ProjectModificationId).ShouldBeFalse();
        tempData.ContainsKey(TempDataKeys.ProjectModificationIdentifier).ShouldBeFalse();
        tempData.ContainsKey(TempDataKeys.ProjectModificationChangeId).ShouldBeFalse();
        tempData.ContainsKey(TempDataKeys.ProjectModificationSpecificArea).ShouldBeFalse();
    }

    [Fact]
    public async Task ProjectOverview_SetsProjectOverviewTempDataKey()
    {
        // Arrange
        var tempDataProvider = new Mock<ITempDataProvider>();
        var answers = new List<RespondentAnswerDto>
            {
                new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project X" },
                new() { QuestionId = QuestionIds.ProjectPlannedEndDate, AnswerText = "01/01/2025" }
            };
        var respondentService = Mocker.GetMock<IRespondentService>();
        respondentService
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = answers });

        // Ensure HttpContext/Request is set up
        var httpContext = new DefaultHttpContext();

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var tempData = new TempDataDictionary(new DefaultHttpContext(), tempDataProvider.Object);
        Sut.TempData = tempData;

        // Act
        await Sut.ProjectOverview(null, null);

        // Assert
        tempData[TempDataKeys.ProjectOverview].ShouldBe(true);
    }
}