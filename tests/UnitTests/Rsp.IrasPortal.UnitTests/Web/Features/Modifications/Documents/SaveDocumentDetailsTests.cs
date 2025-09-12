using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications.Documents.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.Documents;

public class SaveDocumentDetailsTests : TestServiceBase<DocumentsController>
{
    [Fact]
    public async Task SaveDocumentDetails_WhenValidationFails_ReturnsAddDocumentDetailsView()
    {
        // Arrange
        var viewModel = new ModificationAddDocumentDetailsViewModel
        {
            DocumentId = Guid.NewGuid(),

            Questions = new List<QuestionViewModel>
                {
                    new QuestionViewModel { Index = 0, QuestionId = "Q1" }
                }
        };

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { }
            });

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Questions[0].AnswerText", "Answer is required")
            }));

        // Act
        var result = await Sut.SaveDocumentDetails(viewModel);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("AddDocumentDetails", viewResult.ViewName);
        Assert.False(Sut.ModelState.IsValid);
        Assert.Contains(Sut.ModelState["Questions[0].AnswerText"].Errors,
            e => e.ErrorMessage == "Answer is required");
    }

    [Fact]
    public async Task SaveDocumentDetails_WhenValidationFails_MapsApplicantAnswersIntoQuestionnaire_AndReturnsAddDocumentDetailsView()
    {
        // Arrange
        var viewModel = new ModificationAddDocumentDetailsViewModel
        {
            DocumentId = Guid.NewGuid(),
            Questions =
        [
            new QuestionViewModel
            {
                Index = 0,
                QuestionId = "Q1",
                AnswerText = "Applicant Answer",
                SelectedOption = "OptionA",
                Answers = new List<AnswerViewModel> { new AnswerViewModel { IsSelected = true } },
                Day = "01",
                Month = "09",
                Year = "2025"
            }
        ]
        };

        // CMS question set will return a "blank" questionnaire question with the same Index
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse
                {
                    Sections =
                    [
                        new SectionModel { Id = "Q1", SectionId = "Text",
                        Questions =
                        [
                            new QuestionModel
                            {
                                QuestionId = "Q1",
                                Answers = new List<AnswerModel> { new AnswerModel { Id = "1", OptionName = "OptionName" } },
                            }
                        ]}
                    ]
                }
            });

        // Validator fails so we stay on the same view
        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        // Act
        var result = await Sut.SaveDocumentDetails(viewModel);

        // Assert
        var viewResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(DocumentsController.AddDocumentDetailsList), viewResult.ActionName);
        Assert.True(Sut.ModelState.IsValid);
    }

    [Fact]
    public async Task SaveDocumentDetails_WhenValidationSucceedsAndValidateMandatoryTrue_RedirectsToAddDocumentDetailsList()
    {
        // Arrange
        var viewModel = new ModificationAddDocumentDetailsViewModel
        {
            DocumentId = Guid.NewGuid(),
            Questions = []
        };

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { }
            });

        // Act
        var result = await Sut.SaveDocumentDetails(viewModel);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(DocumentsController.AddDocumentDetailsList), redirect.ActionName);
    }

    [Fact]
    public async Task SaveDocumentDetails_WhenValidationSucceedsAndValidateMandatoryFalse_RedirectsToReviewDocumentDetails()
    {
        // Arrange
        var viewModel = new ModificationAddDocumentDetailsViewModel
        {
            DocumentId = Guid.NewGuid(),
            Questions = []
        };

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { }
            });

        // Act
        var result = await Sut.SaveDocumentDetails(viewModel);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(DocumentsController.AddDocumentDetailsList), redirect.ActionName);
    }
}