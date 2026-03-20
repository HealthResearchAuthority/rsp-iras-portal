using System.Reflection;
using Azure.Storage.Blobs;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Azure;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs.CmsQuestionset;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications.Documents.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications.Documents;

public class DownloadDocumentsAsZipTests : TestServiceBase<DocumentsController>
{
    [Fact]
    public async Task DownloadDocumentsAsZip_Returns_FileResult_WithZip()
    {
        // Arrange
        var folderName = Guid.NewGuid().ToString();
        var modId = Guid.NewGuid();

        var modificationGuid = Guid.NewGuid();
        var irasId = "358577";

        var selectedDocuments = new List<string>
        {
            $"{irasId}/{modificationGuid}/file1.pdf",
            $"{irasId}/{modificationGuid}/file2.pdf"
        };

        // MOCK BlobClient
        var blobClientMock = new Mock<BlobClient>();

        // MOCK BlobContainerClient
        var containerClientMock = new Mock<BlobContainerClient>();
        containerClientMock
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(blobClientMock.Object);

        // MOCK BlobServiceClient
        var blobServiceClientMock = new Mock<BlobServiceClient>();
        blobServiceClientMock
            .Setup(s => s.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(containerClientMock.Object);

        // MOCK FACTORY so GetBlobClient(true) returns our blobServiceClientMock
        var factoryMock = Mocker
            .GetMock<IAzureClientFactory<BlobServiceClient>>();
        factoryMock
            .Setup(f => f.CreateClient("Clean"))
            .Returns(blobServiceClientMock.Object);

        // Mock access check
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.CheckDocumentAccess(modificationGuid))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        // ============================================
        // SPONSOR BLOCK MOCKS
        // ============================================

        // SponsorDetails questionset
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet(
                It.Is<string>(x => x == "pm-sponsor-reference"),
                It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new()
                {
                    Sections =
                    [
                        new()
                    {
                        Id = "SP1",
                        CategoryId = "SPC",
                        Questions =
                        [
                            new QuestionModel
                            {
                                Id = "SPQ1",
                                QuestionId = "SPQ1",
                                AnswerDataType = "Text",
                                CategoryId = "SPC"
                            }
                        ]
                    }
                    ]
                }
            });

        // Sponsor answers
        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationAnswers(It.IsAny<Guid>(), "PR1"))
            .ReturnsAsync(new ServiceResponse<IEnumerable<RespondentAnswerDto>>
            {
                StatusCode = HttpStatusCode.OK,
                Content = [new() { QuestionId = "SPQ1", AnswerText = "SponsorAnswer" }]
            });

        // DocumentDetails question set
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationQuestionSet(
                It.Is<string>(x => x == "pdm-document-metadata"),
                It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<CmsQuestionSetResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new()
                {
                    Sections =
                    [
                        new()
                    {
                        Id = "DOC1",
                        CategoryId = "D1",
                        Questions =
                        [
                            new QuestionModel
                            {
                                Id = "DocType",
                                QuestionId = ModificationQuestionIds.DocumentType,
                                AnswerDataType = "Text",
                                CategoryId = "D1"
                            }
                        ]
                    }
                    ]
                }
            });

        // Documents
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.GetDocumentsForModification(
                It.IsAny<Guid>(),
                It.IsAny<ProjectOverviewDocumentSearchRequest>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()))
            .ReturnsAsync(new ServiceResponse<ProjectOverviewDocumentResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new()
                {
                    TotalCount = 1,
                    Documents = [new() { DocumentType = "TypeA" }]
                }
            });

        // ranking
        Mocker.GetMock<ICmsQuestionsetService>()
            .Setup(s => s.GetModificationRanking(It.IsAny<RankingOfChangeRequest>()))
            .ReturnsAsync(new ServiceResponse<RankingOfChangeResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = new()
                {
                    ModificationType = new() { Substantiality = "Non-Notifiable", Order = 1 },
                    Categorisation = new() { Category = "C", Order = 1 }
                }
            });

        Mocker.GetMock<IValidator<QuestionnaireViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<QuestionnaireViewModel>>(), default))
            .ReturnsAsync(new ValidationResult());

        var expectedBytes = new byte[] { 1, 2, 3, 4 };

        var fileResult = new FileContentResult(expectedBytes, "application/zip")
        {
            FileDownloadName = "documents.zip"
        };

        var serviceResponse = new ServiceResponse<IActionResult>()
            .WithContent(fileResult, HttpStatusCode.OK);

        // Mock blob storage zip creation
        Mocker.GetMock<IBlobStorageService>()
            .Setup(s => s.DownloadFilesAsZipAsync(
                blobServiceClientMock.Object,
                It.IsAny<string>(),
                selectedDocuments,
                It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            // Store the identifier as a string GUID so the controller can parse it
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = modificationGuid,
            [TempDataKeys.ProjectModification.ProjectModificationId] = modificationGuid,
            [TempDataKeys.IrasId] = 358577,
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await Sut.DownloadDocumentsAsZip(It.IsAny<string>(), "ModificationDetails");

        // Assert
        result.ShouldBeOfType<FileContentResult>();
        var file = result as FileContentResult;

        file!.ContentType.ShouldBe("application/zip");
    }

    [Fact]
    public void BuildZipFileName_WhenIdentifierIsNull_ReturnsDefaultFormat()
    {
        // Act
        var result = InvokeBuildZipFileName(null);

        // Assert
        result.ShouldStartWith("Documents-");
        result.ShouldEndWith(DateTime.UtcNow.ToString("ddMMMyy"));
    }

    [Fact]
    public void BuildZipFileName_WhenIdentifierIsEmpty_ReturnsDefaultFormat()
    {
        // Act
        var result = InvokeBuildZipFileName("");

        // Assert
        result.ShouldStartWith("Documents-");
        result.ShouldEndWith(DateTime.UtcNow.ToString("ddMMMyy"));
    }

    [Fact]
    public void BuildZipFileName_WhenIdentifierContainsSlash_ReplacesSlash()
    {
        // Arrange
        string identifier = "MOD/123";

        // Act
        var result = InvokeBuildZipFileName(identifier);

        // Assert
        result.ShouldStartWith("MOD-123-");
        result.ShouldEndWith(DateTime.UtcNow.ToString("ddMMMyy"));
        result.ShouldNotContain("/");
    }

    private static string InvokeBuildZipFileName(string? value)
    {
        var method = typeof(DocumentsController)
            .GetMethod("BuildZipFileName", BindingFlags.NonPublic | BindingFlags.Static);

        return (string)method!.Invoke(null, new object?[] { value })!;
    }

    [Fact]
    public async Task DownloadDocumentsAsZip_WhenAccessForbidden_Returns403()
    {
        // Arrange
        var folderName = Guid.NewGuid().ToString();
        var modificationGuid = Guid.NewGuid().ToString();

        // Mock access check forbidden
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.CheckDocumentAccess(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse().WithStatus(HttpStatusCode.Forbidden));

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = modificationGuid,
            [TempDataKeys.ProjectModification.ProjectModificationId] = modificationGuid,
            [TempDataKeys.IrasId] = 358577,
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await Sut.DownloadDocumentsAsZip(folderName, It.IsAny<string>());

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe((int)HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DownloadDocumentsSelectionAsZip_Returns_FileResult_WithZip()
    {
        // Arrange
        var modificationGuid = Guid.NewGuid();
        var irasId = "358577";

        var selectedDocuments = new List<string>
        {
            $"{irasId}/{modificationGuid}/file1.pdf",
            $"{irasId}/{modificationGuid}/file2.pdf"
        };

        // MOCK BlobClient
        var blobClientMock = new Mock<BlobClient>();

        // MOCK BlobContainerClient
        var containerClientMock = new Mock<BlobContainerClient>();
        containerClientMock
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(blobClientMock.Object);

        // MOCK BlobServiceClient
        var blobServiceClientMock = new Mock<BlobServiceClient>();
        blobServiceClientMock
            .Setup(s => s.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(containerClientMock.Object);

        // MOCK FACTORY so GetBlobClient(true) returns our blobServiceClientMock
        var factoryMock = Mocker
            .GetMock<IAzureClientFactory<BlobServiceClient>>();
        factoryMock
            .Setup(f => f.CreateClient("Clean"))
            .Returns(blobServiceClientMock.Object);

        var expectedBytes = new byte[] { 1, 2, 3, 4 };

        var fileResult = new FileContentResult(expectedBytes, "application/zip")
        {
            FileDownloadName = "documents.zip"
        };

        var serviceResponse = new ServiceResponse<IActionResult>()
            .WithContent(fileResult, HttpStatusCode.OK);

        // Mock blob storage zip creation
        Mocker.GetMock<IBlobStorageService>()
            .Setup(s => s.DownloadFilesAsZipAsync(
                blobServiceClientMock.Object,
                It.IsAny<string>(),
                selectedDocuments,
                It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        // Mock access check
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.CheckDocumentAccess(modificationGuid))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = modificationGuid.ToString(),
            [TempDataKeys.ProjectModification.ProjectModificationId] = modificationGuid,
            [TempDataKeys.IrasId] = irasId
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await Sut.DownloadDocumentsSelectionAsZip(selectedDocuments, It.IsAny<string>());

        // Assert
        result.ShouldBeOfType<FileContentResult>();

        var file = result as FileContentResult;

        file!.ContentType.ShouldBe("application/zip");
        file.FileDownloadName.ShouldBe("documents.zip");
    }

    [Fact]
    public async Task DownloadDocumentsSelectionAsZip_ReturnsReviewAllChangesModelError_FileResult_WithZip()
    {
        // Arrange
        var modificationGuid = Guid.NewGuid();
        var irasId = "358577";

        var selectedDocuments = new List<string>();

        // MOCK BlobClient
        var blobClientMock = new Mock<BlobClient>();

        // MOCK BlobContainerClient
        var containerClientMock = new Mock<BlobContainerClient>();
        containerClientMock
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(blobClientMock.Object);

        // MOCK BlobServiceClient
        var blobServiceClientMock = new Mock<BlobServiceClient>();
        blobServiceClientMock
            .Setup(s => s.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(containerClientMock.Object);

        // MOCK FACTORY so GetBlobClient(true) returns our blobServiceClientMock
        var factoryMock = Mocker
            .GetMock<IAzureClientFactory<BlobServiceClient>>();
        factoryMock
            .Setup(f => f.CreateClient("Clean"))
            .Returns(blobServiceClientMock.Object);

        var expectedBytes = new byte[] { 1, 2, 3, 4 };

        var fileResult = new FileContentResult(expectedBytes, "application/zip")
        {
            FileDownloadName = "documents.zip"
        };

        var serviceResponse = new ServiceResponse<IActionResult>()
            .WithContent(fileResult, HttpStatusCode.OK);

        // Mock blob storage zip creation
        Mocker.GetMock<IBlobStorageService>()
            .Setup(s => s.DownloadFilesAsZipAsync(
                blobServiceClientMock.Object,
                It.IsAny<string>(),
                selectedDocuments,
                It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        // Mock access check
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.CheckDocumentAccess(modificationGuid))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = modificationGuid.ToString(),
            [TempDataKeys.ProjectModification.ProjectModificationId] = modificationGuid,
            [TempDataKeys.IrasId] = irasId
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await Sut.DownloadDocumentsSelectionAsZip(selectedDocuments, "ReviewAllChanges");

        // Assert
        result.ShouldBeOfType<RedirectToRouteResult>();
    }

    [Fact]
    public async Task DownloadDocumentsSelectionAsZip_ReturnsModificationDetailsModelError_FileResult_WithZip()
    {
        // Arrange
        var modificationGuid = Guid.NewGuid();
        var irasId = "358577";

        var selectedDocuments = new List<string>();

        // MOCK BlobClient
        var blobClientMock = new Mock<BlobClient>();

        // MOCK BlobContainerClient
        var containerClientMock = new Mock<BlobContainerClient>();
        containerClientMock
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(blobClientMock.Object);

        // MOCK BlobServiceClient
        var blobServiceClientMock = new Mock<BlobServiceClient>();
        blobServiceClientMock
            .Setup(s => s.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(containerClientMock.Object);

        // MOCK FACTORY so GetBlobClient(true) returns our blobServiceClientMock
        var factoryMock = Mocker
            .GetMock<IAzureClientFactory<BlobServiceClient>>();
        factoryMock
            .Setup(f => f.CreateClient("Clean"))
            .Returns(blobServiceClientMock.Object);

        var expectedBytes = new byte[] { 1, 2, 3, 4 };

        var fileResult = new FileContentResult(expectedBytes, "application/zip")
        {
            FileDownloadName = "documents.zip"
        };

        var serviceResponse = new ServiceResponse<IActionResult>()
            .WithContent(fileResult, HttpStatusCode.OK);

        // Mock blob storage zip creation
        Mocker.GetMock<IBlobStorageService>()
            .Setup(s => s.DownloadFilesAsZipAsync(
                blobServiceClientMock.Object,
                It.IsAny<string>(),
                selectedDocuments,
                It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        // Mock access check
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.CheckDocumentAccess(modificationGuid))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = modificationGuid.ToString(),
            [TempDataKeys.ProjectModification.ProjectModificationId] = modificationGuid,
            [TempDataKeys.IrasId] = irasId
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await Sut.DownloadDocumentsSelectionAsZip(selectedDocuments, "ModificationDetails");

        // Assert
        result.ShouldBeOfType<RedirectToRouteResult>();
    }
}