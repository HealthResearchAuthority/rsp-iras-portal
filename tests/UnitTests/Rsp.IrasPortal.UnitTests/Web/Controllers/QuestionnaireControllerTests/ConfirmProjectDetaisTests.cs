using System.Text.Json;
using Bogus;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.QuestionnaireControllerTests;

public class ConfirmProjectDetaisTests : TestServiceBase<QuestionnaireController>
{
    public ConfirmProjectDetaisTests()
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
    public async Task ConfirmProjectDetails_Should_ReturnServiceError_When_RespondentServiceFails()
    {
        // Arrange
        SetupApplicationInSession();

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var result = await Sut.ConfirmProjectDetails();

        // Assert
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task ConfirmProjectDetails_Should_ReturnServiceError_When_QuestionSetServiceFails()
    {
        // Arrange
        SetupApplicationInSession();

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK
            });

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetQuestionSet(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var result = await Sut.ConfirmProjectDetails();

        // Assert
        var statusCodeResult = result.ShouldBeOfType<StatusCodeResult>();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
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

        var questionSetServiceResponse = new ServiceResponse<CmsQuestionSetResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new CmsQuestionSetResponse
            {
                Id = "123",
                Version = "1.0",
                Sections = new List<SectionModel>
                {
                    new SectionModel
                    {
                        SectionId = "section-1",
                        Questions = new List<QuestionModel>
                        {
                            new QuestionModel
                            {
                                QuestionId = "Q1"
                            }
                        }
                    }
                }
            }
        };

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(respondentServiceResponse);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetQuestionSet(It.IsAny<string>(), It.IsAny<string>()))
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

        var questionSetServiceResponse = new ServiceResponse<CmsQuestionSetResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new CmsQuestionSetResponse
            {
                Id = "123",
                Version = "1.0",
                Sections = new List<SectionModel>
                {
                    new SectionModel
                    {
                        SectionId = "section-1",
                        Questions = new List<QuestionModel>
                        {
                            new QuestionModel
                            {
                                QuestionId = "Q1"
                            }
                        }
                    }
                }
            }
        };

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetRespondentAnswers(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(respondentServiceResponse);

        Mocker
            .GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetQuestionSet(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(questionSetServiceResponse);

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.ConfirmProjectDetails();

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe("ProjectDetails");
        redirectResult.ControllerName.ShouldBe("ProjectOverview");
    }

    [Theory]
    [InlineData("Date", "2026-04-23", "23 April 2026")]
    [InlineData("Text", "Sample Text", "Sample Text")]
    [InlineData("Email", "test@example.com", "test@example.com")]
    public void GetDisplayText_ReturnsFormatted_AnswerText(string dataType, string input, string expected)
    {
        var question = CreateQuestion(dataType, answerText: input);

        var result = question.GetDisplayText();

        result.ShouldBe(expected);
    }

    [Fact]
    public void GetDisplayText_ReturnsSelectedOptionText_ForRadioButton()
    {
        var question = CreateQuestion("Radio button", selectedOption: "yes");

        var result = question.GetDisplayText();

        result.ShouldBe("Yes");
    }

    [Fact]
    public void GetDisplayText_ReturnsSelectedAnswers_ForCheckbox()
    {
        var question = CreateQuestion("Checkbox", hasSelectedAnswer: true);

        var result = question.GetDisplayText();

        result.ShouldBe("Yes");
    }

    [Fact]
    public void GetDisplayText_ReturnsPrompt_IfNoAnswer()
    {
        var question = CreateQuestion("Text");

        var result = question.GetDisplayText();

        result.ShouldBe("Enter start date");
    }

    [Fact]
    public void GetActionText_ReturnsChange_IfAnswered()
    {
        var question = CreateQuestion("Text", answerText: "Something");

        var result = question.GetActionText();

        result.ShouldBe("Change");
    }

    [Fact]
    public void GetActionText_ReturnsEnterPrompt_IfNotAnswered()
    {
        var question = CreateQuestion("Text");

        var result = question.GetActionText();

        result.ShouldBe("Enter start date");
    }

    [Fact]
    public void IsMissingAnswer_ReturnsTrue_IfAllAreEmpty()
    {
        var question = CreateQuestion("Text");

        var result = question.IsMissingAnswer();

        result.ShouldBeTrue();
    }

    [Fact]
    public void IsMissingAnswer_ReturnsFalse_IfAnswered()
    {
        var question = CreateQuestion("Text", answerText: "123");

        var result = question.IsMissingAnswer();

        result.ShouldBeFalse();
    }

    private QuestionViewModel CreateQuestion(
        string dataType,
        string? answerText = null,
        string? selectedOption = null,
        bool hasSelectedAnswer = false)
    {
        return new QuestionViewModel
        {
            Index = 0,
            QuestionId = "Q1",
            DataType = dataType,
            QuestionText = "What is the start date of the project?",
            ShortQuestionText = "Start date",
            AnswerText = answerText,
            SelectedOption = selectedOption,
            Answers = new List<AnswerViewModel>
            {
                new() { AnswerId = "yes", AnswerText = "Yes", IsSelected = hasSelectedAnswer },
                new() { AnswerId = "no", AnswerText = "No", IsSelected = false }
            }
        };
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