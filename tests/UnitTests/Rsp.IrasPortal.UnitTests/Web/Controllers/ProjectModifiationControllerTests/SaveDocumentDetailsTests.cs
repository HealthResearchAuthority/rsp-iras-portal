using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs.CmsQuestionset;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ProjectModifiationControllerTests;

public class SaveDocumentDetailsTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public async Task SaveDocumentDetails_WhenValidationFails_ReturnsAddDocumentDetailsView()
    {
        // Arrange
        var viewModel = new ModificationAddDocumentDetailsViewModel
        {
            DocumentId = Guid.NewGuid(),
            Questionnaire = new QuestionnaireViewModel
            {
                Questions = new List<QuestionViewModel>
                {
                    new QuestionViewModel { Index = 0, QuestionId = "Q1" }
                }
            }
        };

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult(new[]
            {
                new ValidationFailure("Questions[0].AnswerText", "Answer is required")
            }));

        // Act
        var result = await Sut.SaveDocumentDetails(viewModel, validateMandatory: false);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("AddDocumentDetails", viewResult.ViewName);
        Assert.False(Sut.ModelState.IsValid);
        Assert.Contains(Sut.ModelState["Questions[0].AnswerText"].Errors,
            e => e.ErrorMessage == "Answer is required");
    }

    [Fact]
    public async Task SaveDocumentDetails_WhenValidationSucceedsAndValidateMandatoryTrue_RedirectsToAddDocumentDetailsList()
    {
        // Arrange
        var viewModel = new ModificationAddDocumentDetailsViewModel
        {
            DocumentId = Guid.NewGuid(),
            Questionnaire = new QuestionnaireViewModel { Questions = [] }
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
        var result = await Sut.SaveDocumentDetails(viewModel, validateMandatory: true);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ProjectModificationController.AddDocumentDetailsList), redirect.ActionName);
    }

    [Fact]
    public async Task SaveDocumentDetails_WhenValidationSucceedsAndValidateMandatoryFalse_RedirectsToReviewDocumentDetails()
    {
        // Arrange
        var viewModel = new ModificationAddDocumentDetailsViewModel
        {
            DocumentId = Guid.NewGuid(),
            Questionnaire = new QuestionnaireViewModel { Questions = [] }
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
        var result = await Sut.SaveDocumentDetails(viewModel, validateMandatory: false);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ProjectModificationController.ReviewDocumentDetails), redirect.ActionName);
    }
}