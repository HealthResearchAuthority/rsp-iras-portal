using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.AutoMock;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;
using Shouldly;
using Xunit;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.QuestionnaireControllerTests;

public class SubmitApplicationTests : TestServiceBase<QuestionnaireController>
{
    [Theory, AutoData]
    public async Task Should_ReturnServiceError_When_RespondentServiceGetRespondentAnswersFails
    (
        string applicationId
    )
    {
        // Arrange
        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(s => s.GetQuestions())
            .ReturnsAsync(new ServiceResponse<IEnumerable<Application.DTOs.QuestionsResponse>>
            {
                StatusCode = HttpStatusCode.OK
            });

        var context = new DefaultHttpContext();
        context.Request.Path = "/questionnaire/submitapplication";

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };

        // Act
        var result = await Sut.SubmitApplication(applicationId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Error");
    }

    [Theory, AutoData]
    public async Task Should_ReturnServiceError_When_QuestionSetServiceGetQuestions_Fails
    (
        string applicationId
    )
    {
        // Arrange
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
            .ReturnsAsync(new ServiceResponse<IEnumerable<Application.DTOs.QuestionsResponse>>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        var context = new DefaultHttpContext();
        context.Request.Path = "/questionnaire/submitapplication";

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };

        // Act
        var result = await Sut.SubmitApplication(applicationId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Error");
    }

    [Theory, AutoData]
    public async Task Should_MarkCategoryAsNotEntered_When_NoAnswersAreProvided
    (
        string applicationId
    )
    {
        // Arrange
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
            .Setup(s => s.GetRespondentAnswers(applicationId))
            .ReturnsAsync(respondentServiceResponse);

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(s => s.GetQuestions())
            .ReturnsAsync(questionSetServiceResponse);

        // Act
        var result = await Sut.SubmitApplication(applicationId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeOfType<Dictionary<string, string>>();
        var model = viewResult.Model as Dictionary<string, string>;
        model.ShouldNotBeNull();
        model.ShouldContainKeyAndValue("Category1", "Not Entered");
        model.ShouldContainKeyAndValue("Category2", "Not Entered");
    }

    [Theory, AutoData]
    public async Task Should_MarkCategoryAsCompleted_When_AllAnswersAreValid
    (
        string applicationId
    )
    {
        // Arrange
        var respondentServiceResponse = new ServiceResponse<IEnumerable<RespondentAnswerDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new List<RespondentAnswerDto>
        {
            new() { CategoryId = "Category1", QuestionId = "Q1", AnswerText = "Answer1" },
            new() { CategoryId = "Category1", QuestionId = "Q2", AnswerText = "Answer2" }
        }
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
            .Setup(s => s.GetRespondentAnswers(applicationId))
            .ReturnsAsync(respondentServiceResponse);

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(s => s.GetQuestions())
            .ReturnsAsync(questionSetServiceResponse);

        Mocker
            .GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.SubmitApplication(applicationId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeOfType<Dictionary<string, string>>();
        var model = viewResult.Model as Dictionary<string, string>;
        model.ShouldNotBeNull();
        model.ShouldContainKeyAndValue("Category1", "Completed");
    }

    [Theory, AutoData]
    public async Task Should_MarkCategoryAsInComplete_When_AllAnswersAreInValid
    (
        string applicationId
    )
    {
        // Arrange
        var respondentServiceResponse = new ServiceResponse<IEnumerable<RespondentAnswerDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new List<RespondentAnswerDto>
        {
            new() { CategoryId = "Category1", QuestionId = "Q1", AnswerText = "Answer1" },
            new() { CategoryId = "Category1", QuestionId = "Q2", AnswerText = "Answer2" }
        }
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
            .Setup(s => s.GetRespondentAnswers(applicationId))
            .ReturnsAsync(respondentServiceResponse);

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(s => s.GetQuestions())
            .ReturnsAsync(questionSetServiceResponse);

        Mocker
            .GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult
            {
                Errors = { new ValidationFailure("Category1", "Error") }
            });

        // Act
        var result = await Sut.SubmitApplication(applicationId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeOfType<Dictionary<string, string>>();
        var model = viewResult.Model as Dictionary<string, string>;
        model.ShouldNotBeNull();
        model.ShouldContainKeyAndValue("Category1", "Incomplete");
    }
}