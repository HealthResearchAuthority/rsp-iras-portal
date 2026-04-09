using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.Enums;
using Rsp.Portal.Web.Features.Modifications;
using Rsp.Portal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications;

public class ModificationsControllerBaseTests
{
    private readonly Mock<IRespondentService> _respondentService = new();
    private readonly Mock<IProjectModificationsService> _projectModificationsService = new();
    private readonly Mock<ICmsQuestionsetService> _cmsQuestionsetService = new();
    private readonly Mock<IValidator<QuestionnaireViewModel>> _validator = new();
    private readonly Mock<IFeatureManager> _featureManager = new();

    private TestModificationsController _controller;

    public ModificationsControllerBaseTests()
    {
        _validator
            .Setup(v => v.ValidateAsync(
                It.IsAny<ValidationContext<QuestionnaireViewModel>>(),
                default
            ))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _controller = new TestModificationsController(
            _respondentService.Object,
            _projectModificationsService.Object,
            _cmsQuestionsetService.Object,
            _validator.Object,
            _featureManager.Object
        );

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    public class TestModificationsController : ModificationsControllerBase
    {
        public TestModificationsController(
            IRespondentService respondentService,
            IProjectModificationsService projectModificationsService,
            ICmsQuestionsetService cmsQuestionsetService,
            IValidator<QuestionnaireViewModel> validator,
            IFeatureManager featureManager
        ) : base(respondentService, projectModificationsService, cmsQuestionsetService, validator, featureManager)
        {
        }

        public Task Invoke_MapDocumentTypesAndStatusesAsync(
            QuestionnaireViewModel questionnaire,
            IEnumerable<ProjectOverviewDocumentDto> documents,
            bool addModelErrors = true,
            bool showIncompleteForReviseAndAuthoriseStatus = false)
        {
            return base.MapDocumentTypesAndStatusesAsync(
                questionnaire,
                documents,
                addModelErrors,
                showIncompleteForReviseAndAuthoriseStatus
            );
        }

        public IEnumerable<ProjectOverviewDocumentDto> Invoke_GetSortedAndPaginatedDocuments(
          IEnumerable<ProjectOverviewDocumentDto> documents,
          int pageSize,
          int pageNumber,
          string sortField,
          string sortDirection)
        {
            return GetSortedAndPaginatedDocuments(documents, sortField, sortDirection, pageSize, pageNumber);
        }
    }

    [Fact]
    public async Task MapDocumentTypesAndStatuses_WhenShowIncompleteIsFalse_AlwaysKeepsReviseAndAuthorise()
    {
        var doc = new ProjectOverviewDocumentDto
        {
            Id = Guid.NewGuid(),
            Status = DocumentStatus.ReviseAndAuthorise,
            DocumentType = "TypeA"
        };

        var questionnaire = new QuestionnaireViewModel
        {
            Questions =
            [
                new QuestionViewModel
                {
                    QuestionId = QuestionIds.SelectedDocumentType,
                    Answers =
                    [
                        new AnswerViewModel
                        {
                            AnswerId = "TypeA",
                            AnswerText = "FriendlyType"
                        }
                    ]
                }
            ]
        };

        await _controller.Invoke_MapDocumentTypesAndStatusesAsync(
            questionnaire,
            new[] { doc },
            addModelErrors: false,
            showIncompleteForReviseAndAuthoriseStatus: false
        );

        doc.Status.ShouldBe(DocumentStatus.ReviseAndAuthorise);
    }

    [Fact]
    public async Task MapDocumentTypesAndStatuses_WhenShowIncompleteTrue_CompleteAndReviseAndAuthorise_ReturnsReviseAndAuthorise()
    {
        var docId = Guid.NewGuid();

        var doc = new ProjectOverviewDocumentDto
        {
            Id = docId,
            Status = DocumentStatus.ReviseAndAuthorise,
            DocumentType = "TypeA"
        };

        var questionnaire = new QuestionnaireViewModel
        {
            Questions =
            [
                new QuestionViewModel
                {
                    QuestionId = QuestionIds.SelectedDocumentType,
                    Answers =
                    [
                        new AnswerViewModel
                        {
                            AnswerId = "TypeA",
                            AnswerText = "FriendlyType"
                        }
                    ]
                }
            ]
        };

        _respondentService.Setup(s => s.GetModificationDocumentAnswers(docId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = [new() { QuestionId = "X", AnswerText = "Valid" }]
            });

        await _controller.Invoke_MapDocumentTypesAndStatusesAsync(
            questionnaire,
            new[] { doc },
            addModelErrors: false,
            showIncompleteForReviseAndAuthoriseStatus: true
        );

        doc.Status.ShouldBe(DocumentStatus.ReviseAndAuthorise);
    }

    [Fact]
    public async Task MapDocumentTypesAndStatuses_WhenShowIncompleteTrue_IncompleteAndReviseAndAuthorise_ReturnsIncomplete()
    {
        var docId = Guid.NewGuid();

        var doc = new ProjectOverviewDocumentDto
        {
            Id = docId,
            Status = DocumentStatus.ReviseAndAuthorise,
            DocumentType = "TypeA"
        };

        var questionnaire = new QuestionnaireViewModel
        {
            Questions =
            [
                new QuestionViewModel
                {
                    QuestionId = QuestionIds.SelectedDocumentType,
                    Answers =
                    [
                        new AnswerViewModel
                        {
                            AnswerId = "TypeA",
                            AnswerText = "FriendlyType"
                        }
                    ]
                }
            ]
        };

        _respondentService.Setup(s => s.GetModificationDocumentAnswers(docId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        await _controller.Invoke_MapDocumentTypesAndStatusesAsync(
            questionnaire,
            new[] { doc },
            addModelErrors: false,
            showIncompleteForReviseAndAuthoriseStatus: true
        );

        doc.Status.ShouldBe(DocumentDetailStatus.Incomplete.ToString());
    }

    [Fact]
    public async Task MapDocumentTypesAndStatuses_WhenUploadedAndComplete_ReturnsComplete()
    {
        var docId = Guid.NewGuid();

        var doc = new ProjectOverviewDocumentDto
        {
            Id = docId,
            Status = DocumentStatus.Uploaded,
            DocumentType = "TypeA"
        };

        var questionnaire = new QuestionnaireViewModel
        {
            Questions =
            [
                new QuestionViewModel
                {
                    QuestionId = QuestionIds.SelectedDocumentType,
                    Answers =
                    [
                        new AnswerViewModel
                        {
                            AnswerId = "TypeA",
                            AnswerText = "FriendlyType"
                        }
                    ]
                }
            ]
        };

        _respondentService.Setup(s => s.GetModificationDocumentAnswers(docId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content =
                [
                    new() { QuestionId = "X", AnswerText = "Valid" }
                ]
            });

        await _controller.Invoke_MapDocumentTypesAndStatusesAsync(
            questionnaire,
            new[] { doc },
            addModelErrors: false,
            showIncompleteForReviseAndAuthoriseStatus: true
        );

        doc.Status.ShouldBe(DocumentDetailStatus.Complete.ToString());
    }

    [Fact]
    public async Task MapDocumentTypesAndStatuses_WhenUploadedAndIncomplete_ReturnsIncomplete()
    {
        var docId = Guid.NewGuid();

        var doc = new ProjectOverviewDocumentDto
        {
            Id = docId,
            Status = DocumentStatus.Uploaded,
            DocumentType = "TypeA"
        };

        var questionnaire = new QuestionnaireViewModel
        {
            Questions =
            [
                new QuestionViewModel
                {
                    QuestionId = QuestionIds.SelectedDocumentType,
                    Answers =
                    [
                        new AnswerViewModel
                        {
                            AnswerId = "TypeA",
                            AnswerText = "FriendlyType"
                        }
                    ]
                }
            ]
        };

        _respondentService.Setup(s => s.GetModificationDocumentAnswers(docId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = []
            });

        await _controller.Invoke_MapDocumentTypesAndStatusesAsync(
            questionnaire,
            new[] { doc },
            addModelErrors: false,
            showIncompleteForReviseAndAuthoriseStatus: true
        );

        doc.Status.ShouldBe(DocumentDetailStatus.Incomplete.ToString());
    }

    [Fact]
    public async Task MapDocumentTypesAndStatuses_MapsDocumentTypeIdToFriendlyText()
    {
        var doc = new ProjectOverviewDocumentDto
        {
            Id = Guid.NewGuid(),
            Status = DocumentStatus.Uploaded,
            DocumentType = "TypeA"
        };

        var questionnaire = new QuestionnaireViewModel
        {
            Questions =
            [
                new QuestionViewModel
                {
                    QuestionId = QuestionIds.SelectedDocumentType,
                    Answers =
                    [
                        new AnswerViewModel
                        {
                            AnswerId = "TypeA",
                            AnswerText = "FriendlyNameA"
                        }
                    ]
                }
            ]
        };

        _respondentService.Setup(s => s.GetModificationDocumentAnswers(doc.Id))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content =
                [
                    new() { QuestionId = "X", AnswerText = "SomeAnswer" }
                ]
            });

        await _controller.Invoke_MapDocumentTypesAndStatusesAsync(
            questionnaire,
            new[] { doc }
        );

        doc.DocumentType.ShouldBe("FriendlyNameA");
    }

    [Theory]
    [AutoData]
    public void ReturnCorrectSort_When_Status_Present_Ascending(IEnumerable<ProjectOverviewDocumentDto> allDocuments)
    {
        // arrange
        var sortField = "Status";
        var sortDirection = "asc";
        var pageSize = 5;
        var pageNumber = 1;

        var expectedResult = allDocuments.OrderBy(d => d.Status).Take(pageSize);

        // execute
        var result = _controller.Invoke_GetSortedAndPaginatedDocuments(
            allDocuments,
            pageSize,
            pageNumber,
            sortField,
            sortDirection
        );

        result.ShouldNotBeNull();
        result.Count().ShouldBeLessThanOrEqualTo(pageSize);
        result.ShouldBe(expectedResult);
    }

    [Theory]
    [AutoData]
    public void ReturnCorrectSort_When_Status_Present_Descending(IEnumerable<ProjectOverviewDocumentDto> allDocuments)
    {
        // arrange
        var sortField = "Status";
        var sortDirection = "desc";
        var pageSize = 5;
        var pageNumber = 1;

        var expectedResult = allDocuments.OrderByDescending(d => d.Status).Take(pageSize);

        // execute
        var result = _controller.Invoke_GetSortedAndPaginatedDocuments(
            allDocuments,
            pageSize,
            pageNumber,
            sortField,
            sortDirection
        );

        result.ShouldNotBeNull();
        result.Count().ShouldBeLessThanOrEqualTo(pageSize);
        result.ShouldBe(expectedResult);
    }
}