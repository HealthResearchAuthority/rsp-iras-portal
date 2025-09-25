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
using Rsp.IrasPortal.Web.Controllers.ProjectOverview;
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
            Id = "App1"
        };

        var session = new Mock<ISession>();

        var sessionData = new Dictionary<string, byte[]?>
        {
            { $"{SessionKeys.ProjectRecord}", JsonSerializer.SerializeToUtf8Bytes(application) },
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

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetQuestionSections(false)).ReturnsAsync(responseQuestionSections);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>(), false)).ReturnsAsync(responseQuestionSection);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>(), false)).ReturnsAsync(responseQuestionSection);

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
        var result = await Sut.SaveResponses(model, "", true, submit: true);

        // Assert

        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(QuestionnaireController.SubmitApplication));

        Mocker
           .GetMock<IRespondentService>()
           .Verify(s => s.SaveRespondentAnswers(It.Is<RespondentAnswersRequest>(r =>
               r.ProjectRecordId == "App1" &&
               r.Id == "RespondentId1" &&
               r.RespondentAnswers.Count == 2)), Times.Once);
    }

    [Theory, AutoData]
    public async Task SaveResponses_Should_RedirectToSubmitApplication_When_SaveAndContinueIsTrue_And_NextStageIsEmpty
    (
        QuestionnaireViewModel model,

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
            Id = "App1"
        };

        var session = new Mock<ISession>();

        var sessionData = new Dictionary<string, byte[]?>
        {
            { $"{SessionKeys.ProjectRecord}", JsonSerializer.SerializeToUtf8Bytes(application) },
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

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetQuestionSections(false)).ReturnsAsync(responseQuestionSections);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>(), false)).ReturnsAsync(responseQuestionSection);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>(), false)).ReturnsAsync(responseQuestionSectionNull);

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
        var result = await Sut.SaveResponses(model, "", true, "", submit, saveAndContinue);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(QuestionnaireController.SubmitApplication));

        Mocker
           .GetMock<IRespondentService>()
           .Verify(s => s.SaveRespondentAnswers(It.Is<RespondentAnswersRequest>(r =>
               r.ProjectRecordId == "App1" &&
               r.Id == "RespondentId1" &&
               r.RespondentAnswers.Count == 2)), Times.Once);
    }

    [Theory(Skip = "Need to fix broken code after refactoring"), AutoData]
    public async Task Should_RedirectToResume_When_SaveAndContinueIsTrue_And_NextStageIsNotEmpty
    (
        QuestionnaireViewModel model,
        List<QuestionSectionsResponse> questionSectionsResponse
    )
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
            Id = "App1"
        };

        var session = new Mock<ISession>();

        var sessionData = new Dictionary<string, byte[]?>
        {
            { $"{SessionKeys.ProjectRecord}", JsonSerializer.SerializeToUtf8Bytes(application) },
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

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetQuestionSections(false)).ReturnsAsync(responseQuestionSections);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>(), false)).ReturnsAsync(responseQuestionSection);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>(), false)).ReturnsAsync(responseQuestionSection);

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
        var result = await Sut.SaveResponses(model, "", true, "", submit, saveAndContinue);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(QuestionnaireController.Resume));

        Mocker
           .GetMock<IRespondentService>()
           .Verify(s => s.SaveRespondentAnswers(It.Is<RespondentAnswersRequest>(r =>
               r.ProjectRecordId == "App1" &&
               r.Id == "RespondentId1" &&
               r.RespondentAnswers.Count == 2)), Times.Once);
    }

    [Theory, AutoData]
    public async Task Should_RedirectToProjectOverview_When_SaveForLaterIsTrue
    (
        QuestionnaireViewModel model,
        string shortProjectTitle,
        List<QuestionSectionsResponse> questionSectionsResponse
    )
    {
        // Arrange
        var submit = false;
        var saveAndContinue = bool.FalseString;
        var saveForLater = bool.TrueString;

        model.CurrentStage = QuestionCategories.A;

        var questions = new List<QuestionViewModel>
    {
        new() { Index = 0, QuestionId = "Q1", QuestionText = "Participating nations", SelectedOption = "Option1", Category = "Category1" },
        new() { Index = 1, QuestionId = "Q2", QuestionText = "Short project title", AnswerText = "Answer2", Category = "Category2" }
    };

        var application = new IrasApplicationResponse
        {
            Id = "App1"
        };

        var session = new Mock<ISession>();

        var sessionData = new Dictionary<string, byte[]?>
    {
        { $"{SessionKeys.ProjectRecord}", JsonSerializer.SerializeToUtf8Bytes(application) },
        { $"{SessionKeys.Questionnaire}:{model.CurrentStage}", JsonSerializer.SerializeToUtf8Bytes(questions) },
        { "Short Project Title", Encoding.UTF8.GetBytes(shortProjectTitle) }
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

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetQuestionSections(false)).ReturnsAsync(responseQuestionSections);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>(), false)).ReturnsAsync(responseQuestionSection);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>(), false)).ReturnsAsync(responseQuestionSection);

        Mocker
            .GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.SaveResponses(model, "", true, "", submit, saveAndContinue, saveForLater);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(ProjectOverviewController.ProjectDetails));

        Mocker
           .GetMock<IRespondentService>()
           .Verify(s => s.SaveRespondentAnswers(It.Is<RespondentAnswersRequest>(r =>
               r.ProjectRecordId == "App1" &&
               r.Id == "RespondentId1" &&
               r.RespondentAnswers.Count == 2)), Times.Once);
    }

    [Theory(Skip = "Need to fix broken code after refactoring"), AutoData]
    public async Task Should_RedirectToResume_When_SaveAndContinueIsTrue
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
            Id = "App1"
        };

        var session = new Mock<ISession>();

        var sessionData = new Dictionary<string, byte[]?>
        {
            { $"{SessionKeys.ProjectRecord}", JsonSerializer.SerializeToUtf8Bytes(application) },
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

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetQuestionSections(false)).ReturnsAsync(responseQuestionSections);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>(), false)).ReturnsAsync(responseQuestionSection);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>(), false)).ReturnsAsync(responseQuestionSection);

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
        var result = await Sut.SaveResponses(model, "", true, "", false, bool.TrueString);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(QuestionnaireController.Resume));
        redirectResult.RouteValues?["projectRecordId"].ShouldBe(application.Id);

        Mocker
           .GetMock<IRespondentService>()
           .Verify(s => s.SaveRespondentAnswers(It.Is<RespondentAnswersRequest>(r =>
               r.ProjectRecordId == "App1" &&
               r.Id == "RespondentId1" &&
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
            Id = "App1"
        };

        var session = new Mock<ISession>();

        var sessionData = new Dictionary<string, byte[]?>
        {
            { SessionKeys.ProjectRecord, JsonSerializer.SerializeToUtf8Bytes(application) },
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

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetQuestionSections(false)).ReturnsAsync(responseQuestionSections);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>(), false)).ReturnsAsync(responseQuestionSection);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>(), false)).ReturnsAsync(responseQuestionSectionNull);

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
        var result = await Sut.SaveResponses(model, "", true, "", submit, saveAndContinue);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(QuestionnaireController.DisplayQuestionnaire));

        Mocker
           .GetMock<IRespondentService>()
           .Verify(s => s.SaveRespondentAnswers(It.Is<RespondentAnswersRequest>(r =>
               r.ProjectRecordId == "App1" &&
               r.Id == "RespondentId1" &&
               r.RespondentAnswers.Count == 2)), Times.Once);
    }

    [Theory]
    [InlineData("searched:true", "Org123", "", "Org123")] // Search performed, org selected
    [InlineData("searched:true", "", "", "")] // Search performed, no org selected
    [InlineData("", "Org123", "Org123", "Org123")] // No search, org selected, search text matches
    [InlineData("", "Org123", "Different", "")] // No search, org selected, search text does not match
    [InlineData("", "Org123", "", "")] // No search, org selected, search text empty
    [InlineData("", "", "SomeText", "")] // No search, no org selected
    public async Task SaveResponses_SponsorOrgLookup_UpdatesAnswerText_Correctly
    (
        string searchedPerformed,
        string sponsorOrganisation,
        string sponsorOrgSearchText,
        string expectedAnswerText
    )
    {
        // Arrange
        var sponsorQuestion = new QuestionViewModel
        {
            Index = 0,
            QuestionId = "Q1",
            QuestionType = "rts:org_lookup",
            QuestionText = "Short project title",
            AnswerText = ""
        };

        var model = new QuestionnaireViewModel
        {
            CurrentStage = "A",
            Questions = [sponsorQuestion],
            SponsorOrgSearch = new()
            {
                SelectedOrganisation = sponsorOrganisation,
                SearchText = sponsorOrgSearchText
            }
        };

        var application = new IrasApplicationResponse
        {
            Id = "App1"
        };

        var session = new Mock<ISession>();
        var sessionData = new Dictionary<string, byte[]?>
        {
            { SessionKeys.ProjectRecord, JsonSerializer.SerializeToUtf8Bytes(application) },
            { $"{SessionKeys.Questionnaire}:{model.CurrentStage}", JsonSerializer.SerializeToUtf8Bytes(model.Questions) }
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

        context.Items[ContextItemKeys.RespondentId] = "RespondentId1";
        Sut.ControllerContext = new ControllerContext { HttpContext = context };
        Sut.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());

        Mocker
            .GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetQuestionSections(false))
            .ReturnsAsync(new ServiceResponse<IEnumerable<QuestionSectionsResponse>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = [new QuestionSectionsResponse { QuestionCategoryId = "A", SectionId = "A" }]
            });

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>(), false))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new QuestionSectionsResponse { QuestionCategoryId = "A", SectionId = "A" }
            });

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>(), false))
            .ReturnsAsync(new ServiceResponse<QuestionSectionsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new QuestionSectionsResponse { QuestionCategoryId = "A", SectionId = "A" }
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.SaveRespondentAnswers(It.IsAny<RespondentAnswersRequest>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        // Act
        var result = await Sut.SaveResponses(model, searchedPerformed, false);

        // Assert
        Mocker
           .GetMock<IRespondentService>()
           .Verify(s => s.SaveRespondentAnswers(It.Is<RespondentAnswersRequest>(r =>
               r.ProjectRecordId == "App1" &&
               r.Id == "RespondentId1" &&
               r.RespondentAnswers[0].AnswerText == expectedAnswerText)), Times.Once);
    }
}