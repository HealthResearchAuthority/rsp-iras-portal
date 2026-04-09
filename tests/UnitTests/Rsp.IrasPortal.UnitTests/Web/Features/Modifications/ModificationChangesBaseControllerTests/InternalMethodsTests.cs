using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
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
    }
}