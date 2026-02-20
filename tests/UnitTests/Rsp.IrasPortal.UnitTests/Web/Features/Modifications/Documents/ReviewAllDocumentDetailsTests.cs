using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.FeatureManagement;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.CmsQuestionset;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications.Documents.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications.Documents;

public class ReviewAllDocumentDetailsTests : TestServiceBase<DocumentsController>
{
    [Fact]
    public async Task ReviewAllDocumentDetails_AllValidationsPass_ShouldRedirectToPostApproval()
    {
        // Arrange
        var docId = Guid.NewGuid();

        Mocker.GetMock<IFeatureManager>()
            .Setup(f => f.IsEnabledAsync(FeatureFlags.SupersedingDocuments))
            .ReturnsAsync(true);

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentRequest>
                {
                    new ProjectModificationDocumentRequest
                    {
                        Id = docId,
                        FileName = "doc1.pdf",
                        DocumentStoragePath = "path",
                        ProjectModificationId = Guid.NewGuid(),
                        ReplacedByDocumentId = Guid.NewGuid(),
                        LinkedDocumentId = Guid.NewGuid()
                    }
                }
            });

        Mocker
            .GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentAnswers(docId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentAnswerDto>
                {
                    new ProjectModificationDocumentAnswerDto { QuestionId = QuestionIds.PreviousVersionOfDocument, AnswerText = QuestionIds.PreviousVersionOfDocumentYesOption, OptionType = "dropdown", SelectedOption = QuestionIds.PreviousVersionOfDocumentYesOption }
                }
            });

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse
                {
                    Sections = new List<SectionModel>
                    {
                        new SectionModel
                        {
                            Id = "Q1",
                            Questions = new List<QuestionModel>()
                            {
                                new QuestionModel
                                {
                                    Id = "1",
                                    QuestionId = "Test",
                                    AnswerDataType = "Dropdown",
                                    Answers = new List<AnswerModel>()
                                    {
                                        new AnswerModel
                                        {
                                            Id = "2",
                                            OptionName = "Test",
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = It.IsAny<Guid>(),
            [TempDataKeys.ProjectRecordId] = It.IsAny<string>()
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        Sut.HttpContext.Items[ContextItemKeys.UserId] = "respondent-1";

        // Act
        var result = await Sut.ReviewAllDocumentDetails();

        // Assert
        var redirect = Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task ReviewAllDocumentDetails_WhenStatusIsReviseAndAuthorise_ShouldRedirectToModificationDetails()
    {
        // Arrange
        var docId = Guid.NewGuid();

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentRequest>
                {
                new ProjectModificationDocumentRequest
                {
                    Id = docId, FileName = "doc1", DocumentStoragePath = "path"
                }
                }
            });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationDocumentAnswers(docId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new List<ProjectModificationDocumentAnswerDto>()
            });

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse
                {
                    Sections = new List<SectionModel>
                    {
                    new SectionModel
                    {
                        Id = "Q1",
                        Questions = new List<QuestionModel>
                        {
                            new QuestionModel
                            {
                                Id = "1",
                                QuestionId = "Test",
                                AnswerDataType = "Text"
                            }
                        }
                    }
                    }
                }
            });

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        var modId = Guid.NewGuid();
        var projId = "record-123";
        var sponsorUserId = Guid.NewGuid();
        var rtsId = "rts-xyz";

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = modId,
            [TempDataKeys.ProjectRecordId] = projId,
            [TempDataKeys.RevisionSponsorOrganisationUserId] = sponsorUserId,
            [TempDataKeys.RevisionRtsId] = rtsId,
            [TempDataKeys.ProjectModification.ProjectModificationStatus] = ModificationStatus.ReviseAndAuthorise,
            [TempDataKeys.IrasId] = "IRAS-123",
            [TempDataKeys.ShortProjectTitle] = "Short title"
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        Sut.HttpContext.Items[ContextItemKeys.UserId] = "respondent-1";

        // Act
        var result = await Sut.ReviewAllDocumentDetails();

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();

        redirect.RouteName.ShouldBe("pmc:ModificationDetails");

        redirect.RouteValues["projectRecordId"].ShouldBe(projId);
        redirect.RouteValues["irasId"].ShouldBe("IRAS-123");
        redirect.RouteValues["shortTitle"].ShouldBe("Short title");
        redirect.RouteValues["projectModificationId"].ShouldBe(modId.ToString());
        redirect.RouteValues["sponsorOrganisationUserId"].ShouldBe(sponsorUserId.ToString());
        redirect.RouteValues["rtsId"].ShouldBe(rtsId);
    }
}