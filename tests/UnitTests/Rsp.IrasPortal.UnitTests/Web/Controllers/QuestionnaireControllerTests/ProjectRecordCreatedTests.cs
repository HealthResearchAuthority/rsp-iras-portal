using System.Text.Json;
using Bogus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Features.ProjectRecord.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.QuestionnaireControllerTests;

public class ProjectRecordCreatedTests : TestServiceBase<QuestionnaireController>
{
    public ProjectRecordCreatedTests()
    {
        var mockSession = new Mock<ISession>();

        var httpContext = new DefaultHttpContext
        {
            Session = mockSession.Object
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var tempDataProvider = new Mock<ITempDataProvider>();

        Sut.TempData = new TempDataDictionary(httpContext, tempDataProvider.Object);
    }

    [Fact]
    public async Task ProjectRecordCreated_Should_ReturnServiceError_When_ApplicationServiceFails()
    {
        // Arrange
        SetupApplicationInSession();

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetProjectRecord(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var result = await Sut.ProjectRecordCreated();

        // Assert
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task ProjectRecordCreated_Should_ReturnServiceError_When_RespondentServiceFails()
    {
        // Arrange
        SetupApplicationInSession();

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetProjectRecord(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.OK
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var result = await Sut.ProjectRecordCreated();

        // Assert
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Theory, AutoData]
    public async Task ProjectRecordCreated_Should_ReturnView_When_Successful
    (
        IrasApplicationResponse applicationResponse,
        List<RespondentAnswerDto> respondentAnswers
    )
    {
        // Arrange
        SetupApplicationInSession();

        Mocker
            .GetMock<IApplicationsService>()
            .Setup(s => s.GetProjectRecord(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IrasApplicationResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = applicationResponse
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = respondentAnswers
            });

        // Act
        var result = await Sut.ProjectRecordCreated();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
    }

    private void SetupApplicationInSession()
    {
        var faker = new Faker<QuestionViewModel>()
        .RuleFor(q => q.Index, fake => fake.IndexFaker)
        .RuleFor(q => q.QuestionId, fake => fake.Random.Guid().ToString())
        .RuleFor(q => q.Category, fake => "project-details")
        .RuleFor(q => q.SectionId, fake => "section-1")
        .RuleFor(q => q.Section, fake => "Project Details")
        .RuleFor(q => q.Sequence, fake => fake.Random.Int(1, 10))
        .RuleFor(q => q.Heading, fake => fake.Lorem.Sentence())
        .RuleFor(q => q.QuestionText, fake => "What is the start date of the project?")
        .RuleFor(q => q.ShortQuestionText, fake => "Project start date")
        .RuleFor(q => q.DataType, fake => fake.PickRandom("Date", "Text", "Email", "Checkbox", "Boolean", "Radio button"))
        .RuleFor(q => q.AnswerText, (f, q) =>
        {
            return q.DataType == "Date" ? "2026-04-23" :
                   q.DataType == "Text" ? "sample text" :
                   null;
        })
        .RuleFor(q => q.SelectedOption, (f, q) => q.DataType is "Boolean" or "Radio button" ? "yes" : null)
        .RuleFor(q => q.Answers, (f, q) =>
        {
            var answers = new List<AnswerViewModel>
            {
                new() { AnswerId = "yes", AnswerText = "Yes", IsSelected = q.SelectedOption == "yes" },
                new() { AnswerId = "no", AnswerText = "No", IsSelected = false }
            };

            if (q.DataType == "Checkbox")
            {
                answers.ForEach(a => a.IsSelected = f.Random.Bool());
            }

            return answers;
        });
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