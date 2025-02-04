using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.QuestionnaireControllerTests;

public class SaveResponsesTests : TestServiceBase<QuestionnaireController>
{
    [Theory, AutoData]
    public async Task SaveResponses_Should_Save_Responses_And_Redirect_To_SubmitApplication_When_Submit_Is_True
    (
        QuestionnaireViewModel model
    )
    {
        // Arrange
        var questions = new List<QuestionViewModel>
        {
            new() { Index = 0, QuestionId = "Q1", SelectedOption = "Option1" },
            new() { Index = 1, QuestionId = "Q2", AnswerText = "Answer2" }
        };

        var application = new IrasApplicationResponse
        {
            ApplicationId = "App1"
        };

        var session = new Mock<ISession>();
        session
            .Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]?>.IsAny))
            .Returns((string key, out byte[]? value) =>
            {
                switch (key)
                {
                    case SessionKeys.Application:
                        value = JsonSerializer.SerializeToUtf8Bytes(application);
                        return true;

                    case SessionKeys.Questionnaire:
                        value = JsonSerializer.SerializeToUtf8Bytes(questions);
                        return true;

                    default:
                        value = null;
                        return false;
                }
            });

        var context = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());

        context.Items[ContextItemKeys.RespondentId] = "RespondentId1";

        Sut.ControllerContext = new ControllerContext { HttpContext = context };

        // Act
        var result = await Sut.SaveResponses(model, submit: bool.TrueString);

        // Assert

        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(QuestionnaireController.SubmitApplication));

        Mocker
           .GetMock<IRespondentService>()
           .Verify(s => s.SaveRespondentAnswers(It.Is<RespondentAnswersRequest>(r =>
               r.ApplicationId == "App1" &&
               r.RespondentId == "RespondentId1" &&
               r.RespondentAnswers.Count == 2)), Times.Once);
    }

    [Theory, AutoData]
    public async Task SaveResponses_Should_RedirectToSubmitApplication_When_SaveAndContinueIsTrue_And_NextStageIsEmpty
    (
        QuestionnaireViewModel model,
        string categoryId
    )
    {
        // Arrange
        model.CurrentStage = QuestionCategories.D; // Next stage is empty
        var submit = bool.FalseString;
        var saveAndContinue = bool.TrueString;
        var questions = new List<QuestionViewModel>
    {
        new() { Index = 0, QuestionId = "Q1", SelectedOption = "Option1" },
        new() { Index = 1, QuestionId = "Q2", AnswerText = "Answer2" }
    };

        var application = new IrasApplicationResponse
        {
            ApplicationId = "App1"
        };

        var session = new Mock<ISession>();
        session
            .Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]?>.IsAny))
            .Returns((string key, out byte[]? value) =>
            {
                switch (key)
                {
                    case SessionKeys.Application:
                        value = JsonSerializer.SerializeToUtf8Bytes(application);
                        return true;

                    case SessionKeys.Questionnaire:
                        value = JsonSerializer.SerializeToUtf8Bytes(questions);
                        return true;

                    default:
                        value = null;
                        return false;
                }
            });

        var context = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());

        context.Items[ContextItemKeys.RespondentId] = "RespondentId1";

        Sut.ControllerContext = new ControllerContext { HttpContext = context };

        // Act
        var result = await Sut.SaveResponses(model, categoryId, submit, saveAndContinue);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(QuestionnaireController.SubmitApplication));

        Mocker
           .GetMock<IRespondentService>()
           .Verify(s => s.SaveRespondentAnswers(It.Is<RespondentAnswersRequest>(r =>
               r.ApplicationId == "App1" &&
               r.RespondentId == "RespondentId1" &&
               r.RespondentAnswers.Count == 2)), Times.Once);
    }

    [Theory, AutoData]
    public async Task Should_RedirectToResume_When_SaveAndContinueIsTrue_And_NextStageIsNotEmpty(
    QuestionnaireViewModel model,
    string categoryId)
    {
        // Arrange
        var submit = bool.FalseString;
        var saveAndContinue = bool.TrueString;
        model.CurrentStage = QuestionCategories.A;
        var questions = new List<QuestionViewModel>
    {
        new() { Index = 0, QuestionId = "Q1", SelectedOption = "Option1" },
        new() { Index = 1, QuestionId = "Q2", AnswerText = "Answer2" }
    };

        var application = new IrasApplicationResponse
        {
            ApplicationId = "App1"
        };

        var session = new Mock<ISession>();
        session
            .Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]?>.IsAny))
            .Returns((string key, out byte[]? value) =>
            {
                switch (key)
                {
                    case SessionKeys.Application:
                        value = JsonSerializer.SerializeToUtf8Bytes(application);
                        return true;

                    case SessionKeys.Questionnaire:
                        value = JsonSerializer.SerializeToUtf8Bytes(questions);
                        return true;

                    default:
                        value = null;
                        return false;
                }
            });

        var context = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());

        context.Items[ContextItemKeys.RespondentId] = "RespondentId1";

        Sut.ControllerContext = new ControllerContext { HttpContext = context };

        // Act
        var result = await Sut.SaveResponses(model, categoryId, submit, saveAndContinue);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(QuestionnaireController.Resume));

        Mocker
           .GetMock<IRespondentService>()
           .Verify(s => s.SaveRespondentAnswers(It.Is<RespondentAnswersRequest>(r =>
               r.ApplicationId == "App1" &&
               r.RespondentId == "RespondentId1" &&
               r.RespondentAnswers.Count == 2)), Times.Once);
    }

    [Theory, AutoData]
    public async Task Should_RedirectToResume_When_CategoryIdIsProvided
    (
        QuestionnaireViewModel model,
        string categoryId
    )
    {
        // Arrange
        var questions = new List<QuestionViewModel>
    {
        new() { Index = 0, QuestionId = "Q1", SelectedOption = "Option1" },
        new() { Index = 1, QuestionId = "Q2", AnswerText = "Answer2" }
    };

        var application = new IrasApplicationResponse
        {
            ApplicationId = "App1"
        };

        var session = new Mock<ISession>();
        session
            .Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]?>.IsAny))
            .Returns((string key, out byte[]? value) =>
            {
                switch (key)
                {
                    case SessionKeys.Application:
                        value = JsonSerializer.SerializeToUtf8Bytes(application);
                        return true;

                    case SessionKeys.Questionnaire:
                        value = JsonSerializer.SerializeToUtf8Bytes(questions);
                        return true;

                    default:
                        value = null;
                        return false;
                }
            });

        var context = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());

        context.Items[ContextItemKeys.RespondentId] = "RespondentId1";

        Sut.ControllerContext = new ControllerContext { HttpContext = context };

        // Act
        var result = await Sut.SaveResponses(model, categoryId, bool.FalseString, bool.FalseString);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(QuestionnaireController.Resume));
        redirectResult.RouteValues?["applicationId"].ShouldBe(application.ApplicationId);
        redirectResult.RouteValues?["categoryId"].ShouldBe(categoryId);

        Mocker
           .GetMock<IRespondentService>()
           .Verify(s => s.SaveRespondentAnswers(It.Is<RespondentAnswersRequest>(r =>
               r.ApplicationId == "App1" &&
               r.RespondentId == "RespondentId1" &&
               r.RespondentAnswers.Count == 2)), Times.Once);
    }

    [Theory, AutoData]
    public async Task Should_RedirectToDisplayQuestionnaire_When_NoSpecificActionButtonsAreClicked
    (
        QuestionnaireViewModel model
    )
    {
        // Arrange
        var categoryId = string.Empty;
        var submit = bool.FalseString;
        var saveAndContinue = bool.FalseString;
        var questions = new List<QuestionViewModel>
    {
        new() { Index = 0, QuestionId = "Q1", SelectedOption = "Option1" },
        new() { Index = 1, QuestionId = "Q2", AnswerText = "Answer2" }
    };

        var application = new IrasApplicationResponse
        {
            ApplicationId = "App1"
        };

        var session = new Mock<ISession>();
        session
            .Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]?>.IsAny))
            .Returns((string key, out byte[]? value) =>
            {
                switch (key)
                {
                    case SessionKeys.Application:
                        value = JsonSerializer.SerializeToUtf8Bytes(application);
                        return true;

                    case SessionKeys.Questionnaire:
                        value = JsonSerializer.SerializeToUtf8Bytes(questions);
                        return true;

                    default:
                        value = null;
                        return false;
                }
            });

        var context = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());

        context.Items[ContextItemKeys.RespondentId] = "RespondentId1";

        Sut.ControllerContext = new ControllerContext { HttpContext = context };

        // Act
        var result = await Sut.SaveResponses(model, categoryId, submit, saveAndContinue);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(QuestionnaireController.DisplayQuestionnaire));

        Mocker
           .GetMock<IRespondentService>()
           .Verify(s => s.SaveRespondentAnswers(It.Is<RespondentAnswersRequest>(r =>
               r.ApplicationId == "App1" &&
               r.RespondentId == "RespondentId1" &&
               r.RespondentAnswers.Count == 2)), Times.Once);
    }
}