using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.CmsQuestionset;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications.ModificationChangesBaseControllerTests;

public class InternalMethodsTests
{
    [Fact]
    public async Task SaveModificationChangeAnswers_MapsQuestionTypes_AndCallsService()
    {
        // Arrange
        var respondentService = new Mock<IRespondentService>();
        ProjectModificationChangeAnswersRequest? capturedRequest = null;

        respondentService
            .Setup(s => s.SaveModificationChangeAnswers(It.IsAny<ProjectModificationChangeAnswersRequest>()))
            .Callback<ProjectModificationChangeAnswersRequest>(r => capturedRequest = r)
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        var controller = new TestModificationChangesBaseController(
            respondentService.Object,
            Mock.Of<ICmsQuestionsetService>(),
            Mock.Of<IModificationRankingService>(),
            Mock.Of<IValidator<QuestionnaireViewModel>>());

        var http = new DefaultHttpContext();
        http.Items[ContextItemKeys.UserId] = "user-1";
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        controller.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var questions = new List<QuestionViewModel>
        {
            new()
            {
                QuestionId = "Q1",
                DataType = "Checkbox",
                Answers =
                [
                    new AnswerViewModel { AnswerId = "A1", IsSelected = true },
                    new AnswerViewModel { AnswerId = "A2", IsSelected = false }
                ]
            },
            new()
            {
                QuestionId = "Q2",
                DataType = "Dropdown",
                SelectedOption = "OPT1"
            }
        };

        // Act
        await controller.InvokeSaveModificationChangeAnswers(Guid.NewGuid(), "PR-1", questions);

        // Assert
        capturedRequest.ShouldNotBeNull();
        capturedRequest!.ProjectRecordId.ShouldBe("PR-1");
        capturedRequest.UserId.ShouldBe("user-1");
        capturedRequest.ModificationChangeAnswers.Count.ShouldBe(2);
        capturedRequest.ModificationChangeAnswers.Single(a => a.QuestionId == "Q1").OptionType.ShouldBe("Multiple");
        capturedRequest.ModificationChangeAnswers.Single(a => a.QuestionId == "Q1").Answers.ShouldBe(["A1"]);
        capturedRequest.ModificationChangeAnswers.Single(a => a.QuestionId == "Q2").OptionType.ShouldBe("Single");
    }

    [Fact]
    public void CheckModification_AndGetSpecificArea_ReadFromTempData()
    {
        // Arrange
        var modificationId = Guid.NewGuid();
        var modificationChangeId = Guid.NewGuid();
        var specificAreaOfChangeId = Guid.NewGuid();

        var controller = new TestModificationChangesBaseController(
            Mock.Of<IRespondentService>(),
            Mock.Of<ICmsQuestionsetService>(),
            Mock.Of<IModificationRankingService>(),
            Mock.Of<IValidator<QuestionnaireViewModel>>());

        var http = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        controller.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = modificationId,
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = modificationChangeId,
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeId] = specificAreaOfChangeId
        };

        // Act
        var result = controller.InvokeCheckModification();
        var specificArea = controller.InvokeGetSpecificAreaOfChangeId();

        // Assert
        result.ModificationId.ShouldBe(modificationId);
        result.ModificationChangeId.ShouldBe(modificationChangeId);
        specificArea.ShouldBe(specificAreaOfChangeId);
    }

    [Fact]
    public void GetSpecificAreaOfChangeId_WhenTempDataMissing_ReturnsGuidEmpty()
    {
        // Arrange
        var controller = new TestModificationChangesBaseController(
            Mock.Of<IRespondentService>(),
            Mock.Of<ICmsQuestionsetService>(),
            Mock.Of<IModificationRankingService>(),
            Mock.Of<IValidator<QuestionnaireViewModel>>());

        var http = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        controller.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        // Act
        var specificArea = controller.InvokeGetSpecificAreaOfChangeId();

        // Assert
        specificArea.ShouldBe(Guid.Empty);
    }

    [Fact]
    public async Task SetStage_WhenSpecificAreaMissing_ReturnsEmptyNavigation_AndDoesNotCallCms()
    {
        // Arrange
        var cmsService = new Mock<ICmsQuestionsetService>();

        var controller = new TestModificationChangesBaseController(
            Mock.Of<IRespondentService>(),
            cmsService.Object,
            Mock.Of<IModificationRankingService>(),
            Mock.Of<IValidator<QuestionnaireViewModel>>());

        var http = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        controller.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        // Act
        var result = await controller.InvokeSetStage("SEC1");

        // Assert
        result.ShouldNotBeNull();
        result.CurrentStage.ShouldBeNull();

        cmsService.Verify(s => s.GetModificationQuestionSet(It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        cmsService.Verify(s => s.GetModificationPreviousQuestionSection(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        cmsService.Verify(s => s.GetModificationNextQuestionSection(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task SetStage_MapsPreviousAndNext_WhenServiceResponsesAreNotSuccessful()
    {
        // Arrange
        var cmsService = new Mock<ICmsQuestionsetService>();
        var specificAreaId = Guid.NewGuid();

        cmsService
            .Setup(s => s.GetModificationQuestionSet("SEC1", null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse
                {
                    Sections =
                    [
                        new SectionModel { Id = "SEC1", SectionId = "CURRENT-SEC", CategoryId = "CAT-1" }
                    ]
                }
            });

        cmsService
            .Setup(s => s.GetModificationPreviousQuestionSection("SEC1", null, null))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.BadRequest,
                Error = "fail"
            });

        cmsService
            .Setup(s => s.GetModificationNextQuestionSection("SEC1", null, null))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.BadRequest,
                Error = "fail"
            });

        var controller = new TestModificationChangesBaseController(
            Mock.Of<IRespondentService>(),
            cmsService.Object,
            Mock.Of<IModificationRankingService>(),
            Mock.Of<IValidator<QuestionnaireViewModel>>());

        var http = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        controller.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeId] = specificAreaId
        };

        // Act
        var result = await controller.InvokeSetStage("SEC1");

        // Assert
        result.CurrentStage.ShouldBe("CURRENT-SEC");
        result.CurrentCategory.ShouldBe("CAT-1");
        result.PreviousStage.ShouldBeEmpty();
        result.PreviousCategory.ShouldBeEmpty();
        result.NextStage.ShouldBeEmpty();
        result.NextCategory.ShouldBeEmpty();

        controller.TempData[TempDataKeys.PreviousStage].ShouldBe(string.Empty);
        controller.TempData[TempDataKeys.PreviousCategory].ShouldBe(string.Empty);
        controller.TempData[TempDataKeys.CurrentStage].ShouldBe("CURRENT-SEC");
    }

    private sealed class TestModificationChangesBaseController : ModificationChangesBaseController
    {
        public TestModificationChangesBaseController(
            IRespondentService respondentService,
            ICmsQuestionsetService cmsQuestionsetService,
            IModificationRankingService modificationRankingService,
            IValidator<QuestionnaireViewModel> validator)
            : base(respondentService, cmsQuestionsetService, modificationRankingService, validator)
        {
        }

        public (Guid ModificationId, Guid ModificationChangeId) InvokeCheckModification() => CheckModification();

        public Guid InvokeGetSpecificAreaOfChangeId() => GetSpecificAreaOfChangeId();

        public Task InvokeSaveModificationChangeAnswers(Guid projectModificationChangeId, string projectRecordId, List<QuestionViewModel> questions)
            => SaveModificationChangeAnswers(projectModificationChangeId, projectRecordId, questions);

        public Task<NavigationDto> InvokeSetStage(string sectionId, string? parentQuestionId = null, string? parentAnswerOption = null)
            => SetStage(sectionId, parentQuestionId, parentAnswerOption);
    }
}