using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.QuestionnaireControllerTests;

public class ValidateTests : TestServiceBase<QuestionnaireController>
{
    [Theory, AutoData]
    public async Task Should_ValidateQuestionnaire_And_SaveResultInViewData
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
            { $"{SessionKeys.ProjectRecord}:{model.CurrentStage}", JsonSerializer.SerializeToUtf8Bytes(application) },
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

        var responseQuestionSection = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse[0]
        };

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetNextQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        var context = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        Sut.TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>());

        Sut.ControllerContext = new ControllerContext { HttpContext = context };

        Mocker
            .GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.Validate(model);

        // Assert
        result.ShouldBeOfType<ViewResult>();
        Sut.ViewData[ViewDataKeys.IsQuestionnaireValid].ShouldBe(true);
    }

    [Theory, AutoData]
    public async Task Should_InvalidateQuestionnaire_And_AddErrorsToModelState
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
            { $"{SessionKeys.ProjectRecord}:{model.CurrentStage}", JsonSerializer.SerializeToUtf8Bytes(application) },
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

        var responseQuestionSection = new ServiceResponse<QuestionSectionsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = questionSectionsResponse[0]
        };

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(q => q.GetPreviousQuestionSection(It.IsAny<string>())).ReturnsAsync(responseQuestionSection);

        Mocker.GetMock<ICmsQuestionsetService>()
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
           .ReturnsAsync(new ValidationResult
           {
               Errors =
               {
                new ValidationFailure("Question1", "Error1"),
                new ValidationFailure("Question2", "Error2")
               }
           });

        // Act
        var result = await Sut.Validate(model);

        // Assert
        result.ShouldBeOfType<ViewResult>();
        Sut.ViewData[ViewDataKeys.IsQuestionnaireValid].ShouldBe(false);
        Sut.ModelState.IsValid.ShouldBe(false);
    }
}