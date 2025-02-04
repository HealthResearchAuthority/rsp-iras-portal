using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.QuestionnaireControllerTests;

public class ValidateTests : TestServiceBase<QuestionnaireController>
{
    [Theory, AutoData]
    public async Task Should_ValidateQuestionnaire_And_SaveResultInViewData
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