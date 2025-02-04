using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.QuestionnaireControllerTests;

public class ResumeTests : TestServiceBase<QuestionnaireController>
{
    [Theory, AutoData]
    public async Task Resume_ShouldReturnNotFound_WhenLoadApplicationReturnsNull(string applicationId, string categoryId)
    {
        // Arrange
        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplication(applicationId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.NotFound });

        // Act
        var result = await Sut.Resume(applicationId, categoryId);

        // Assert
        result.ShouldBeOfType<NotFoundResult>();

        // Verify
        Mocker
            .GetMock<IApplicationsService>()
            .Verify(s => s.GetApplication(applicationId), Times.Once);
    }

    [Theory, AutoData]
    public async Task Resume_Should_Return_ServiceError_When_RespondentService_GetRespondentAnswers_Is_Unsuccessful(string applicationId, string categoryId)
    {
        // Arrange
        var applicationResponse = new ServiceResponse<IrasApplicationResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new IrasApplicationResponse()
        };

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplication(applicationId))
            .ReturnsAsync(applicationResponse);

        var unsuccessfulResponse = new ServiceResponse<IEnumerable<RespondentAnswerDto>>
        {
            StatusCode = HttpStatusCode.InternalServerError
        };

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(applicationId, categoryId))
            .ReturnsAsync(unsuccessfulResponse);

        var session = new Mock<ISession>();
        var httpContext = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await Sut.Resume(applicationId, categoryId);

        // Assert
        result.ShouldBeOfType<ViewResult>();
        var viewResult = result as ViewResult;
        viewResult!.ViewName.ShouldBe("Error");
    }

    [Theory, AutoData]
    public async Task Resume_Should_Return_ServiceError_When_QuestionSetService_GetQuestions_Is_Unsuccessful(string applicationId, string categoryId)
    {
        // Arrange
        var respondentServiceResponse = new ServiceResponse<IEnumerable<RespondentAnswerDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = []
        };

        var questionsSetServiceResponse = new ServiceResponse<IEnumerable<QuestionsResponse>>
        {
            StatusCode = HttpStatusCode.InternalServerError
        };

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(x => x.GetApplication(applicationId))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse> { StatusCode = HttpStatusCode.OK, Content = new IrasApplicationResponse() });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(x => x.GetRespondentAnswers(applicationId, categoryId))
            .ReturnsAsync(respondentServiceResponse);

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(x => x.GetQuestions(categoryId))
            .ReturnsAsync(questionsSetServiceResponse);

        var session = new Mock<ISession>();
        var httpContext = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await Sut.Resume(applicationId, categoryId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Error");
        viewResult.Model.ShouldBeOfType<Microsoft.AspNetCore.Mvc.ProblemDetails>();
    }

    [Theory, AutoData]
    public async Task Resume_Should_UpdateQuestionnaireWithExistingAnswers_WhenRespondentAnswersNotEmpty(string applicationId, string categoryId)
    {
        // Arrange
        var applicationResponse = new ServiceResponse<IrasApplicationResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new IrasApplicationResponse()
        };

        var respondentAnswers = new ServiceResponse<IEnumerable<RespondentAnswerDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = []
        };

        var questions = new ServiceResponse<IEnumerable<QuestionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = []
        };

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(x => x.GetApplication(applicationId))
            .ReturnsAsync(applicationResponse);

        Mocker
            .GetMock<IRespondentService>()
            .Setup(x => x.GetRespondentAnswers(applicationId, categoryId))
            .ReturnsAsync(respondentAnswers);

        Mocker
            .GetMock<IQuestionSetService>()
            .Setup(x => x.GetQuestions(categoryId))
            .ReturnsAsync(questions);

        var session = new Mock<ISession>();
        var httpContext = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await Sut.Resume(applicationId, categoryId);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(QuestionnaireController.DisplayQuestionnaire));
        redirectResult.RouteValues!["categoryId"].ShouldBe(categoryId);

        Mocker
            .GetMock<IRespondentService>()
            .Verify(x => x.GetRespondentAnswers(applicationId, categoryId), Times.Once);

        Mocker
            .GetMock<IQuestionSetService>()
            .Verify(x => x.GetQuestions(categoryId), Times.Once);
    }

    [Theory, AutoData]
    public async Task Resume_Should_Return_View_With_Questionnaire_When_Validate_Is_True(string applicationId, string categoryId)
    {
        // Arrange
        var applicationResponse = new ServiceResponse<IrasApplicationResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new IrasApplicationResponse()
        };

        var respondentAnswers = new ServiceResponse<IEnumerable<RespondentAnswerDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = []
        };

        var questions = new ServiceResponse<IEnumerable<QuestionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = []
        };

        Mocker.GetMock<IApplicationsService>()
            .Setup(x => x.GetApplication(applicationId))
            .ReturnsAsync(applicationResponse);

        Mocker.GetMock<IRespondentService>()
            .Setup(x => x.GetRespondentAnswers(applicationId, categoryId))
            .ReturnsAsync(respondentAnswers);

        Mocker.GetMock<IQuestionSetService>()
            .Setup(x => x.GetQuestions(categoryId))
            .ReturnsAsync(questions);

        Mocker
            .GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var session = new Mock<ISession>();
        var httpContext = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await Sut.Resume(applicationId, categoryId, "True");

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Index");
        viewResult.Model.ShouldBeOfType<QuestionnaireViewModel>();

        Mocker
            .GetMock<IValidator<QuestionnaireViewModel>>()
            .Verify(x => x.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task Resume_Should_RedirectToDisplayQuestionnaire_When_ValidateIsFalse(string applicationId, string categoryId)
    {
        // Arrange
        var applicationResponse = new ServiceResponse<IrasApplicationResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new IrasApplicationResponse()
        };

        var respondentAnswers = new ServiceResponse<IEnumerable<RespondentAnswerDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = []
        };

        var questions = new ServiceResponse<IEnumerable<QuestionsResponse>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = []
        };

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetApplication(applicationId))
            .ReturnsAsync(applicationResponse);

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(applicationId, categoryId))
            .ReturnsAsync(respondentAnswers);

        Mocker.GetMock<IQuestionSetService>()
            .Setup(s => s.GetQuestions(categoryId))
            .ReturnsAsync(questions);

        var session = new Mock<ISession>();
        var httpContext = new DefaultHttpContext
        {
            Session = session.Object
        };

        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await Sut.Resume(applicationId, categoryId);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(QuestionnaireController.DisplayQuestionnaire));
        redirectResult.RouteValues!["categoryId"].ShouldBe(categoryId);
    }
}