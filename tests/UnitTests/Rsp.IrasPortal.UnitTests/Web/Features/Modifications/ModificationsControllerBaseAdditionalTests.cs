using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.FeatureManagement;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications;
using Rsp.Portal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications;

public class ModificationsControllerBaseAdditionalTests
{
    [Fact]
    public void BuildDocumentRequest_ReturnsExpectedValues_FromTempDataAndHttpContext()
    {
        // Arrange
        var projectModificationId = Guid.NewGuid();
        var controller = CreateController();
        var http = new DefaultHttpContext();
        http.Items[ContextItemKeys.UserId] = "user-1";

        controller.ControllerContext = new ControllerContext { HttpContext = http };
        controller.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = projectModificationId,
            [TempDataKeys.ProjectRecordId] = "PR-1"
        };

        // Act
        var request = controller.InvokeBuildDocumentRequest();

        // Assert
        request.ProjectModificationId.ShouldBe(projectModificationId);
        request.ProjectRecordId.ShouldBe("PR-1");
        request.UserId.ShouldBe("user-1");
    }

    [Fact]
    public async Task EvaluateDocumentCompletion_ReturnsTrue_WhenNoAnswersExist()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var controller = CreateController();

        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        controller.RespondentService
            .Setup(s => s.GetModificationDocumentAnswers(documentId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        controller.Validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        controller.FeatureManager
            .Setup(f => f.IsEnabledAsync(FeatureFlags.SupersedingDocuments))
            .ReturnsAsync(false);

        var questionnaire = new QuestionnaireViewModel
        {
            Questions = [new QuestionViewModel { QuestionId = "Q1", DataType = "Text", IsMandatory = true }]
        };

        // Act
        var result = await controller.InvokeEvaluateDocumentCompletion(documentId, questionnaire, addModelErrors: false);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task EvaluateDocumentCompletion_ReturnsFalse_WhenAnswersExistAndValidationPasses()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var controller = CreateController();

        controller.RespondentService
            .Setup(s => s.GetModificationDocumentAnswers(documentId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = [new ProjectModificationDocumentAnswerDto { QuestionId = "Q1", AnswerText = "A" }]
            });

        controller.Validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        controller.FeatureManager
            .Setup(f => f.IsEnabledAsync(FeatureFlags.SupersedingDocuments))
            .ReturnsAsync(false);

        var questionnaire = new QuestionnaireViewModel
        {
            Questions = [new QuestionViewModel { QuestionId = "Q1", DataType = "Text", IsMandatory = true }]
        };

        // Act
        var result = await controller.InvokeEvaluateDocumentCompletion(documentId, questionnaire, addModelErrors: false);

        // Assert
        result.ShouldBeFalse();
    }

    private static TestableModificationsController CreateController()
    {
        var respondentService = new Mock<IRespondentService>();
        var projectModificationsService = new Mock<IProjectModificationsService>();
        var cmsQuestionsetService = new Mock<ICmsQuestionsetService>();
        var validator = new Mock<IValidator<QuestionnaireViewModel>>();
        var featureManager = new Mock<IFeatureManager>();

        return new TestableModificationsController(
            respondentService,
            projectModificationsService,
            cmsQuestionsetService,
            validator,
            featureManager);
    }

    private sealed class TestableModificationsController : ModificationsControllerBase
    {
        public TestableModificationsController(
            Mock<IRespondentService> respondentService,
            Mock<IProjectModificationsService> projectModificationsService,
            Mock<ICmsQuestionsetService> cmsQuestionsetService,
            Mock<IValidator<QuestionnaireViewModel>> validator,
            Mock<IFeatureManager> featureManager)
            : base(
                respondentService.Object,
                projectModificationsService.Object,
                cmsQuestionsetService.Object,
                validator.Object,
                featureManager.Object)
        {
            RespondentService = respondentService;
            Validator = validator;
            FeatureManager = featureManager;
        }

        public Mock<IRespondentService> RespondentService { get; }

        public Mock<IValidator<QuestionnaireViewModel>> Validator { get; }

        public Mock<IFeatureManager> FeatureManager { get; }

        public ProjectModificationDocumentRequest InvokeBuildDocumentRequest() => BuildDocumentRequest();

        public Task<bool> InvokeEvaluateDocumentCompletion(Guid documentId, QuestionnaireViewModel questionnaire, bool addModelErrors)
            => EvaluateDocumentCompletion(documentId, questionnaire, addModelErrors);
    }
}