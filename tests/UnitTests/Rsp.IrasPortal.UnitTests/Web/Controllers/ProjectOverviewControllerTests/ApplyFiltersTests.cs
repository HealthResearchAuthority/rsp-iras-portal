using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers.ProjectOverview;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ProjectOverviewControllerTests;

public class ApplyFiltersTests : TestServiceBase<ProjectOverviewController>
{
    private readonly Mock<ITempDataDictionary> MockTempData;

    public ApplyFiltersTests()
    {
        MockTempData = new Mock<ITempDataDictionary>();
    }

    [Fact]
    public async Task ApplyFilters_ShouldNot_Redirect_To_PostApproval()
    {
        // Arrange
        SetupValidatorResult(new ValidationResult());
        var applicationService = Mocker.GetMock<IApplicationsService>();
        applicationService
            .Setup(s => s.GetProjectRecord(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.InternalServerError, });
        Sut.TempData = MockTempData.Object;
        var httpContext = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var searchModel = new ApprovalsSearchModel { ChiefInvestigatorName = "Abc" };
        var viewModel = new PostApprovalViewModel { Search = searchModel };

        // Act
        var result = await Sut.ApplyFilters(viewModel);

        // Assert
        var redirect = result.ShouldBeOfType<StatusCodeResult>();
        redirect.StatusCode.ShouldBe(500);
    }

    [Fact]
    public async Task ApplyFilters_ShouldRedirectTo_PostApproval_With_Valid_View()
    {
        // Arrange
        SetupValidatorResult(new ValidationResult());

        _ = MockTempData.Setup(a => a.Peek(It.IsAny<string>())).Returns("rec-1");
        Sut.TempData = MockTempData.Object;

        // Arrange
        var applicationService = Mocker.GetMock<IApplicationsService>();
        var respondentService = Mocker.GetMock<IRespondentService>();
        var answers = new List<RespondentAnswerDto>
            {
                new() { QuestionId = QuestionIds.ShortProjectTitle, AnswerText = "Project X" },
                new() { QuestionId = QuestionIds.ProjectPlannedEndDate, AnswerText = "01/01/2025" }
            };

        applicationService
            .Setup(s => s.GetProjectRecord("rec-1"))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.OK, Content = new IrasApplicationResponse { Id = "rec-1", IrasId = 1 } });

        respondentService
            .Setup(s => s.GetRespondentAnswers("rec-1", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = HttpStatusCode.OK, Content = answers });

        var httpContext = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var searchModel = new ApprovalsSearchModel { ChiefInvestigatorName = "Abc" };
        var viewModel = new PostApprovalViewModel { Search = searchModel };

        // Act
        var result = await Sut.ApplyFilters(viewModel);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pov:postapproval");
    }

    private void SetupValidatorResult(ValidationResult result)
    {
        var mockValidator = Mocker.GetMock<IValidator<ApprovalsSearchModel>>();
        mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ApprovalsSearchModel>(), default))
            .ReturnsAsync(result);
    }
}