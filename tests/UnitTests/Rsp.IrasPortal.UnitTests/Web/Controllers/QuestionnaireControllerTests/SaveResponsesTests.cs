using System.Text;
using System.Text.Json;
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

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.QuestionnaireControllerTests;

public class SaveResponsesTests : TestServiceBase<QuestionnaireController>
{
    [Theory, AutoData]
    public async Task SaveResponses_Should_Save_Responses_And_Redirect_To_SubmitApplication_When_Submit_Is_True
    (
        QuestionnaireViewModel model,
        List<QuestionSectionsResponse> questionSectionsResponse

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

        var sessionData = new Dictionary<string, byte[]?>
        {
            { $"{SessionKeys.Application}", JsonSerializer.SerializeToUtf8Bytes(application) },
            { $"{SessionKeys.Questionnaire}:{model.CurrentStage}", JsonSerializer.SerializeToUtf8Bytes(questions) }
        };

        session
            .Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]?>.IsAny))
            .Returns((string key, out byte[]? value) =>
            {
                if (sessionData.ContainsKey(key))
                {
                    value = sessionData[key];
                    return true;
                }

                value = null;
                return false;
            });

        var responseQuestionSections = new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse
        };

        var responseQuestionSection = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse[0]
        };

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetQuestionSections()).ReturnsAsync(responseQuestionSections);

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        var context = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());

        context.Items[ContextItemKeys.RespondentId] = "RespondentId1";

        Sut.ControllerContext = new ControllerContext { HttpContext = context };

        Mocker
            .GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.SaveResponses(model, submit: true);

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
        string categoryId,
        List<QuestionSectionsResponse> questionSectionsResponse
    )
    {
        // Arrange
        model.CurrentStage = QuestionCategories.D; // Next stage is empty
        var submit = false;
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

        var sessionData = new Dictionary<string, byte[]?>
        {
            { $"{SessionKeys.Application}", JsonSerializer.SerializeToUtf8Bytes(application) },
            { $"{SessionKeys.Questionnaire}:{model.CurrentStage}", JsonSerializer.SerializeToUtf8Bytes(questions) }
        };

        session
            .Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]?>.IsAny))
            .Returns((string key, out byte[]? value) =>
            {
                if (sessionData.ContainsKey(key))
                {
                    value = sessionData[key];
                    return true;
                }

                value = null;
                return false;
            });

        var responseQuestionSections = new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse
        };

        var responseQuestionSection = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse[0]
        };

        var responseQuestionSectionNull = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
        };

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetQuestionSections()).ReturnsAsync(responseQuestionSections);

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSectionNull);

        var context = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());

        context.Items[ContextItemKeys.RespondentId] = "RespondentId1";

        Sut.ControllerContext = new ControllerContext { HttpContext = context };

        Mocker
            .GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

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
    string categoryId,
    List<QuestionSectionsResponse> questionSectionsResponse)
    {
        // Arrange
        var submit = false;
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

        var sessionData = new Dictionary<string, byte[]?>
        {
            { $"{SessionKeys.Application}", JsonSerializer.SerializeToUtf8Bytes(application) },
            { $"{SessionKeys.Questionnaire}:{model.CurrentStage}", JsonSerializer.SerializeToUtf8Bytes(questions) }
        };

        session
            .Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]?>.IsAny))
            .Returns((string key, out byte[]? value) =>
            {
                if (sessionData.ContainsKey(key))
                {
                    value = sessionData[key];
                    return true;
                }

                value = null;
                return false;
            });

        var responseQuestionSections = new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse
        };

        var responseQuestionSection = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse[0]
        };

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetQuestionSections()).ReturnsAsync(responseQuestionSections);

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        var context = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());

        context.Items[ContextItemKeys.RespondentId] = "RespondentId1";

        Sut.ControllerContext = new ControllerContext { HttpContext = context };

        Mocker
            .GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

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
    public async Task Should_RedirectToProjectOverview_When_SaveForLaterIsTrue(
    QuestionnaireViewModel model,
    string categoryId,
    string shortProjectTitle,
    List<QuestionSectionsResponse> questionSectionsResponse)
    {
        // Arrange
        var submit = false;
        var saveAndContinue = bool.FalseString;
        var saveForLater = bool.TrueString;

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

        var sessionData = new Dictionary<string, byte[]?>
    {
        { $"{SessionKeys.Application}", JsonSerializer.SerializeToUtf8Bytes(application) },
        { $"{SessionKeys.Questionnaire}:{model.CurrentStage}", JsonSerializer.SerializeToUtf8Bytes(questions) },
        { "ShortProjectTitle", Encoding.UTF8.GetBytes(shortProjectTitle) }
    };

        session
            .Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]?>.IsAny))
            .Returns((string key, out byte[]? value) =>
            {
                if (sessionData.ContainsKey(key))
                {
                    value = sessionData[key];
                    return true;
                }

                value = null;
                return false;
            });

        var context = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());
        context.Items[ContextItemKeys.RespondentId] = "RespondentId1";
        Sut.ControllerContext = new ControllerContext { HttpContext = context };

        var responseQuestionSections = new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse
        };

        var responseQuestionSection = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse[0]
        };

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetQuestionSections()).ReturnsAsync(responseQuestionSections);

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker
            .GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.SaveResponses(model, categoryId, submit, saveAndContinue, saveForLater);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(ApplicationController.ProjectOverview));

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
        string categoryId,
        List<QuestionSectionsResponse> questionSectionsResponse
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

        var sessionData = new Dictionary<string, byte[]?>
        {
            { $"{SessionKeys.Application}", JsonSerializer.SerializeToUtf8Bytes(application) },
            { $"{SessionKeys.Questionnaire}:{model.CurrentStage}", JsonSerializer.SerializeToUtf8Bytes(questions) }
        };

        session
            .Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]?>.IsAny))
            .Returns((string key, out byte[]? value) =>
            {
                if (sessionData.ContainsKey(key))
                {
                    value = sessionData[key];
                    return true;
                }

                value = null;
                return false;
            });

        var responseQuestionSections = new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse
        };

        var responseQuestionSection = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse[0]
        };

        var responseQuestionSectionNull = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
        };

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetQuestionSections()).ReturnsAsync(responseQuestionSections);

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        var context = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());

        context.Items[ContextItemKeys.RespondentId] = "RespondentId1";

        Sut.ControllerContext = new ControllerContext { HttpContext = context };

        Mocker
            .GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.SaveResponses(model, categoryId, false, bool.FalseString);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(QuestionnaireController.Resume));
        redirectResult.RouteValues?["applicationId"].ShouldBe(application.ApplicationId);

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
        QuestionnaireViewModel model,
        List<QuestionSectionsResponse> questionSectionsResponse
    )
    {
        // Arrange
        var categoryId = string.Empty;
        var submit = false;
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

        var sessionData = new Dictionary<string, byte[]?>
        {
            { $"{SessionKeys.Application}", JsonSerializer.SerializeToUtf8Bytes(application) },
            { $"{SessionKeys.Questionnaire}:{model.CurrentStage}", JsonSerializer.SerializeToUtf8Bytes(questions) }
        };

        session
            .Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]?>.IsAny))
            .Returns((string key, out byte[]? value) =>
            {
                if (sessionData.ContainsKey(key))
                {
                    value = sessionData[key];
                    return true;
                }

                value = null;
                return false;
            });

        var responseQuestionSections = new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse
        };

        var responseQuestionSection = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse[0]
        };

        var responseQuestionSectionNull = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
        };

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetQuestionSections()).ReturnsAsync(responseQuestionSections);

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker.GetMock<IQuestionSetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSectionNull);

        var context = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());

        context.Items[ContextItemKeys.RespondentId] = "RespondentId1";

        Sut.ControllerContext = new ControllerContext { HttpContext = context };

        Mocker
            .GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

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