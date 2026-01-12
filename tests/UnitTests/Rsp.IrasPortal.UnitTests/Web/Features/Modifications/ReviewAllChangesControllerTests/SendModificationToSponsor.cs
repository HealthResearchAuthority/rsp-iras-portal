using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.CmsQuestionset;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Domain.Enums;
using Rsp.Portal.Web.Features.Modifications;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications.ReviewAllChangesControllerTests;

public class SendModificationToSponsor : TestServiceBase<ReviewAllChangesController>
{
    private const string SectionId = "pm-sponsor-reference";
    private const string CategoryId = "Sponsor reference";

    [Theory, AutoData]
    public async Task SendModificationToSponsor_Should_Return_View_When_Success
    (
        string projectRecordId,
        Guid projectModificationId
    )
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var response = new ServiceResponse
        {
            StatusCode = HttpStatusCode.OK
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.UpdateModificationStatus(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(response);

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationAnswers(It.IsAny<Guid>(), It.IsAny<string>(), CategoryId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = System.Net.HttpStatusCode.OK, Content = [] });

        var qset = new CmsQuestionSetResponse
        {
            Sections = [new SectionModel { Id = SectionId, CategoryId = CategoryId, Questions = [new QuestionModel { Id = "Q1", QuestionId = "Q1", Name = "Q1", CategoryId = CategoryId, AnswerDataType = "Text" }] }]
        };

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet(It.Is<string?>(x => x == SectionId), It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse> { StatusCode = System.Net.HttpStatusCode.OK, Content = qset });

        // Validator OK both passes
        Mocker.GetMock<FluentValidation.IValidator<QuestionnaireViewModel>>()
            .SetupSequence(v => v.ValidateAsync(It.IsAny<FluentValidation.ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult())
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        var result = await Sut.SendModificationToSponsor(projectRecordId, projectModificationId);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("ModificationSentToSponsor");

        // Verify
        Mocker.GetMock<IProjectModificationsService>()
            .Verify(s => s.UpdateModificationStatus(projectRecordId, projectModificationId, ModificationStatus.WithSponsor), Times.Once);
    }

    [Theory, AutoData]
    public async Task SendModificationToSponsor_Should_Return_StatusCode_When_Failure
    (
        string projectRecordId,
        Guid projectModificationId
    )
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var response = new ServiceResponse
        {
            StatusCode = HttpStatusCode.InternalServerError
        };

        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.UpdateModificationStatus(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(response);

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationAnswers(It.IsAny<Guid>(), It.IsAny<string>(), CategoryId))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>> { StatusCode = System.Net.HttpStatusCode.OK, Content = [] });

        var qset = new CmsQuestionSetResponse
        {
            Sections = [new SectionModel { Id = SectionId, CategoryId = CategoryId, Questions = [new QuestionModel { Id = "Q1", QuestionId = "Q1", Name = "Q1", CategoryId = CategoryId, AnswerDataType = "Text" }] }]
        };

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet(It.Is<string?>(x => x == SectionId), It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse> { StatusCode = System.Net.HttpStatusCode.OK, Content = qset });

        // Validator OK both passes
        Mocker.GetMock<FluentValidation.IValidator<QuestionnaireViewModel>>()
            .SetupSequence(v => v.ValidateAsync(It.IsAny<FluentValidation.ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult())
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        var result = await Sut.SendModificationToSponsor(projectRecordId, projectModificationId);

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Theory, AutoData]
    public async Task SendModificationToSponsor_Should_Redirect_When_MalwareScanNotCompleted
    (
        string projectRecordId,
        Guid projectModificationId
    )
    {
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var response = new ServiceResponse
        {
            StatusCode = HttpStatusCode.OK
        };

        // Arrange - one document with IsMalwareScanSuccessful = null → still scanning
        var documents = new List<ProjectOverviewDocumentDto>
        {
            new() { FileName = "mod1", DocumentType = "TypeA", IsMalwareScanSuccessful = null  },
            new() { FileName = "mod2", DocumentType = "TypeB", IsMalwareScanSuccessful = null  }
        };

        var documentsResponse = new ProjectOverviewDocumentResponse
        {
            Documents = documents,
            TotalCount = documents.Count
        };

        var serviceResponse = new ServiceResponse<ProjectOverviewDocumentResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = documentsResponse
        };

        var projectModificationsService = Mocker.GetMock<IProjectModificationsService>();
        projectModificationsService
            .Setup(s => s.GetDocumentsForModification(It.IsAny<Guid>(), It.IsAny<ProjectOverviewDocumentSearchRequest>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { }
            });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = "Safety",
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.ShortProjectTitle] = "Short Title",
            [TempDataKeys.IrasId] = 999,
        };

        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await Sut.SendModificationToSponsor(projectRecordId, projectModificationId);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pmc:DocumentsScanInProgress");
    }

    [Theory, AutoData]
    public async Task SendModificationToSponsor_Should_Redirect_When_Document_Details_Incomplete(
        string projectRecordId,
        Guid projectModificationId)
    {
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        var response = new ServiceResponse
        {
            StatusCode = HttpStatusCode.OK
        };

        // Arrange - one document with IsMalwareScanSuccessful = null → still scanning
        var documents = new List<ProjectOverviewDocumentDto>
        {
            new() { FileName = "mod1", Status = DocumentDetailStatus.Incomplete.ToString(), DocumentType = "TypeA", IsMalwareScanSuccessful = null  },
            new() { FileName = "mod2", Status = DocumentDetailStatus.Incomplete.ToString(), DocumentType = "TypeB", IsMalwareScanSuccessful = null  }
        };

        var documentsResponse = new ProjectOverviewDocumentResponse
        {
            Documents = documents,
            TotalCount = documents.Count
        };

        var serviceResponse = new ServiceResponse<ProjectOverviewDocumentResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = documentsResponse
        };

        var projectModificationsService = Mocker.GetMock<IProjectModificationsService>();
        projectModificationsService
            .Setup(s => s.GetDocumentsForModification(It.IsAny<Guid>(), It.IsAny<ProjectOverviewDocumentSearchRequest>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        var questions = new List<QuestionModel>
        {
            new() { Id = "Q1", NhsInvolvment = "NHS", Answers = [ new() { Id = "A1", OptionName = "NHS" } ] },
            new() { Id = "Q2", NonNhsInvolvment = "Non-NHS", Answers = [ new() { Id = "B1", OptionName = "Non-NHS" } ] },
            new() { Id = "Q3", AffectedOrganisations = true, Answers = [ new() { Id = "C1", OptionName = "All" } ] },
            new() { Id = "Q4", RequireAdditionalResources = true, Answers = [ new() { Id = "D1", OptionName = "Yes" } ] }
        };

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { Sections = [new SectionModel { Questions = questions }] }
            });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            { StatusCode = HttpStatusCode.OK, Content = new List<ProjectModificationDocumentRequest>() { new ProjectModificationDocumentRequest { Status = DocumentStatus.Incomplete.ToString() } } });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = "Safety",
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.ShortProjectTitle] = "Short Title",
            [TempDataKeys.IrasId] = 999,
        };

        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await Sut.SendModificationToSponsor(projectRecordId, projectModificationId);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pmc:DocumentsScanInProgress");
    }

    [Theory, AutoData]
    public async Task SendModificationToSponsor_Should_Redirect_When_Malware_Scan_Incomplete(
    string projectRecordId,
    Guid projectModificationId)
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());

        // Arrange - one document with IsMalwareScanSuccessful = null → still scanning
        var documents = new List<ProjectOverviewDocumentDto>
        {
            new() { FileName = "mod1", Status = DocumentDetailStatus.Complete.ToString(), DocumentType = "TypeA", IsMalwareScanSuccessful = null  },
            new() { FileName = "mod2", Status = DocumentDetailStatus.Complete.ToString(), DocumentType = "TypeB", IsMalwareScanSuccessful = null  }
        };

        var documentsResponse = new ProjectOverviewDocumentResponse
        {
            Documents = documents,
            TotalCount = documents.Count
        };

        var serviceResponse = new ServiceResponse<ProjectOverviewDocumentResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = documentsResponse
        };

        var projectModificationsService = Mocker.GetMock<IProjectModificationsService>();
        projectModificationsService
            .Setup(s => s.GetDocumentsForModification(It.IsAny<Guid>(), It.IsAny<ProjectOverviewDocumentSearchRequest>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = "Safety",
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.ShortProjectTitle] = "Short Title",
            [TempDataKeys.IrasId] = 999,
        };

        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet("pdm-document-metadata", null))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new CmsQuestionSetResponse { }
            });

        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Act
        var result = await Sut.SendModificationToSponsor(projectRecordId, projectModificationId);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pmc:DocumentsScanInProgress");
    }
}