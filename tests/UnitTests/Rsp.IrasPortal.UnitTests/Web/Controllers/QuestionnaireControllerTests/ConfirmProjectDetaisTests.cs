using System.Text.Json;
using Bogus;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.QuestionnaireControllerTests;

public class ConfirmProjectDetaisTests : TestServiceBase<QuestionnaireController>
{
    [Fact]
    public async Task ConfirmProjectDetails_Should_ReturnServiceError_When_RespondentServiceFails()
    {
        // Arrange
        SetupApplicationInSession();

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var result = await Sut.ConfirmProjectDetails();

        // Assert
        result.ShouldBeOfType<ViewResult>().ViewName.ShouldBe("Error");
    }

    [Fact]
    public async Task ConfirmProjectDetails_Should_ReturnServiceError_When_QuestionSetServiceFails()
    {
        // Arrange
        SetupApplicationInSession();

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK
            });

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(s => s.GetQuestions())
            .ReturnsAsync(new ServiceResponse<IEnumerable<QuestionsResponse>>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var result = await Sut.ConfirmProjectDetails();

        // Assert
        result.ShouldBeOfType<ViewResult>().ViewName.ShouldBe("Error");
    }

    [Fact]
    public async Task ConfirmProjectDetails_Should_ReturnReviewAnswers_When_ValidationFails()
    {
        // Arrange
        SetupApplicationInSession();

        var respondentServiceResponse = new ServiceResponse<IEnumerable<RespondentAnswerDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = []
        };

        var questionSetServiceResponse = new ServiceResponse<IEnumerable<QuestionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content =
            [
                new() { Category = "Category1", QuestionId = "Q1" },
                new() { Category = "Category2", QuestionId = "Q2" }
            ]
        };

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>()))
            .ReturnsAsync(respondentServiceResponse);

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(s => s.GetQuestions())
            .ReturnsAsync(questionSetServiceResponse);

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult
            {
                Errors = { new ValidationFailure("Category1", "ReviewAnswers") }
            });

        // Act
        var result = await Sut.ConfirmProjectDetails();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("ReviewAnswers");
        viewResult.Model.ShouldBeOfType<QuestionnaireViewModel>();
    }

    [Fact]
    public async Task ConfirmProjectDetails_Should_RedirectToProjectOverview_When_ValidationPasses()
    {
        // Arrange
        SetupApplicationInSession();

        var respondentServiceResponse = new ServiceResponse<IEnumerable<RespondentAnswerDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = []
        };

        var questionSetServiceResponse = new ServiceResponse<IEnumerable<QuestionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content =
            [
                new() { Category = "Category1", QuestionId = "Q1" },
                new() { Category = "Category2", QuestionId = "Q2" }
            ]
        };

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>()))
            .ReturnsAsync(respondentServiceResponse);

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(s => s.GetQuestions())
            .ReturnsAsync(questionSetServiceResponse);

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.ConfirmProjectDetails();

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe("ProjectOverview");
        redirectResult.ControllerName.ShouldBe("Application");
    }

    private void SetupApplicationInSession()
    {
        var faker = new Faker<QuestionViewModel>();
        List<QuestionViewModel> expectedQuestions = faker.Generate(3);

        var session = new Mock<ISession>();
        session
            .Setup(s => s.Keys)
            .Returns([SessionKeys.Questionnaire]);

        session
            .Setup(s => s.TryGetValue(SessionKeys.Questionnaire, out It.Ref<byte[]?>.IsAny))
            .Returns((string key, out byte[]? value) =>
            {
                if (key != SessionKeys.Questionnaire)
                {
                    value = null;
                    return false;
                }

                value = JsonSerializer.SerializeToUtf8Bytes(expectedQuestions);
                return true;
            });

        var httpContext = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.IrasId] = "1234"
        };
    }
}