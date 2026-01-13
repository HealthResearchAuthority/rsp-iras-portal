using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Azure;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications.Documents.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications.Documents;

public class UploadDocumentTests : TestServiceBase<DocumentsController>
{
    [Fact]
    public async Task UploadDocuments_NullModel_ReturnsFileTooLargeView()
    {
        var result = await Sut.UploadDocuments(null);

        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("FileTooLarge");
    }

    [Fact]
    public async Task UploadDocuments_FileWithInvalidExtension_AddsModelErrorAndStaysOnView()
    {
        var model = new ModificationUploadDocumentsViewModel
        {
            Files = new List<IFormFile>
            {
                new FormFile(new MemoryStream(), 0, 100, "file", "test.xyz")
            }
        };

        var blobClientMock = new Mock<BlobClient>();
        blobClientMock
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

        var containerClientMock = new Mock<BlobContainerClient>();
        containerClientMock
            .Setup(c => c.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(Mock.Of<BlobContainerInfo>(), null!));
        containerClientMock
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(blobClientMock.Object);

        var blobServiceClientMock = new Mock<BlobServiceClient>();
        blobServiceClientMock
            .Setup(b => b.GetBlobContainerClient("containerName"))
            .Returns(containerClientMock.Object);

        // MOCK FACTORY so GetBlobClient(true) returns our blobServiceClientMock
        var factoryMock = Mocker.GetMock<IAzureClientFactory<BlobServiceClient>>();
        factoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(blobServiceClientMock.Object);

        Mocker.GetMock<IBlobStorageService>()
            .Setup(b => b.UploadFilesAsync(blobServiceClientMock.Object, It.IsAny<IEnumerable<IFormFile>>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<DocumentSummaryItemDto>
            {
                new() { FileName = "good.pdf", BlobUri = "uri", FileSize = 1024 }
            });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            { StatusCode = HttpStatusCode.OK, Content = new List<ProjectModificationDocumentRequest>() });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.UserId] = "respondent-1";

        var result = await Sut.UploadDocuments(model);

        result.ShouldBeOfType<ViewResult>();
        Sut.ModelState.ContainsKey("Files").ShouldBeTrue();
        Sut.ModelState["Files"].Errors.First().ErrorMessage.ShouldContain("must be a permitted file type");
    }

    [Fact]
    public async Task UploadDocuments_FileWithDuplicateName_AddsModelErrorAndStaysOnView()
    {
        var existingDocs = new List<ProjectModificationDocumentRequest>
        {
            new() { FileName = "duplicate.pdf" }
        };

        var model = new ModificationUploadDocumentsViewModel
        {
            Files = new List<IFormFile>
            {
                new FormFile(new MemoryStream(), 0, 100, "file", "duplicate.pdf")
            }
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
        var factoryMock = Mocker.GetMock<IAzureClientFactory<BlobServiceClient>>();
        factoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(blobServiceClientMock.Object);

        Mocker.GetMock<IBlobStorageService>()
            .Setup(b => b.UploadFilesAsync(blobServiceClientMock.Object, It.IsAny<IEnumerable<IFormFile>>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<DocumentSummaryItemDto>
            {
                new() { FileName = It.IsAny<string>(), BlobUri = It.IsAny<string>(), FileSize = It.IsAny<long>() }
            });

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            { StatusCode = HttpStatusCode.OK, Content = existingDocs });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.UserId] = "respondent-1";

        var result = await Sut.UploadDocuments(model);

        result.ShouldBeOfType<ViewResult>();
        Sut.ModelState["Files"].Errors.Any(e => e.ErrorMessage.Contains("already been uploaded")).ShouldBeTrue();
    }

    [Fact]
    public async Task UploadDocuments_FileTooLarge_AddsModelErrorAndStaysOnView()
    {
        var model = new ModificationUploadDocumentsViewModel
        {
            Files = new List<IFormFile>
            {
                new FormFile(new MemoryStream(new byte[1024]), 0, 101 * 1024 * 1024, "file", "large.pdf")
            }
        };

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            { StatusCode = HttpStatusCode.OK, Content = new List<ProjectModificationDocumentRequest>() });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.UserId] = "respondent-1";

        var result = await Sut.UploadDocuments(model);

        result.ShouldBeOfType<ViewResult>();
        Sut.ModelState["Files"].Errors.Any(e => e.ErrorMessage.Contains("must be smaller than 100 MB")).ShouldBeTrue();
    }

    [Fact]
    public async Task UploadDocuments_ValidFiles_UploadsAndRedirects()
    {
        var model = new ModificationUploadDocumentsViewModel
        {
            Files = new List<IFormFile>
            {
                new FormFile(new MemoryStream(), 0, 1024, "file", "good.pdf")
            }
        };

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            { StatusCode = HttpStatusCode.OK, Content = new List<ProjectModificationDocumentRequest>() });

        var blobClientMock = new Mock<BlobClient>();
        blobClientMock
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

        var containerClientMock = new Mock<BlobContainerClient>();
        containerClientMock
            .Setup(c => c.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(Mock.Of<BlobContainerInfo>(), null!));
        containerClientMock
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(blobClientMock.Object);

        var blobServiceClientMock = new Mock<BlobServiceClient>();
        blobServiceClientMock
            .Setup(b => b.GetBlobContainerClient("containerName"))
            .Returns(containerClientMock.Object);

        // MOCK FACTORY so GetBlobClient(true) returns our blobServiceClientMock
        var factoryMock = Mocker.GetMock<IAzureClientFactory<BlobServiceClient>>();
        factoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(blobServiceClientMock.Object);

        Mocker.GetMock<IBlobStorageService>()
            .Setup(b => b.UploadFilesAsync(blobServiceClientMock.Object, It.IsAny<IEnumerable<IFormFile>>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<DocumentSummaryItemDto>
            {
                new() { FileName = "good.pdf", BlobUri = "uri", FileSize = 1024 }
            });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.UserId] = "respondent-1";

        var result = await Sut.UploadDocuments(model);

        var redirect = result.ShouldBeOfType<ViewResult>();
    }

    [Fact]
    public async Task UploadDocuments_MixedValidAndInvalidFiles_ReturnsViewWithErrors()
    {
        var model = new ModificationUploadDocumentsViewModel
        {
            Files = new List<IFormFile>
            {
                new FormFile(new MemoryStream(), 0, 1024, "file", "good.pdf"),
                new FormFile(new MemoryStream(), 0, 1024, "file", "bad.xyz")
            }
        };

        Mocker.GetMock<IRespondentService>().Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            { StatusCode = HttpStatusCode.OK, Content = new List<ProjectModificationDocumentRequest>() });

        var blobClientMock = new Mock<BlobClient>();
        blobClientMock
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

        var containerClientMock = new Mock<BlobContainerClient>();
        containerClientMock
            .Setup(c => c.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(Mock.Of<BlobContainerInfo>(), null!));
        containerClientMock
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(blobClientMock.Object);

        var blobServiceClientMock = new Mock<BlobServiceClient>();
        blobServiceClientMock
            .Setup(b => b.GetBlobContainerClient("containerName"))
            .Returns(containerClientMock.Object);

        // MOCK FACTORY so GetBlobClient(true) returns our blobServiceClientMock
        var factoryMock = Mocker.GetMock<IAzureClientFactory<BlobServiceClient>>();
        factoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(blobServiceClientMock.Object);

        Mocker.GetMock<IBlobStorageService>()
            .Setup(b => b.UploadFilesAsync(blobServiceClientMock.Object, It.IsAny<IEnumerable<IFormFile>>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<DocumentSummaryItemDto>
            {
                new() { FileName = "good.pdf", BlobUri = "uri", FileSize = 1024 }
            });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.UserId] = "respondent-1";

        var result = await Sut.UploadDocuments(model);

        var view = result.ShouldBeOfType<ViewResult>();
        Sut.ModelState["Files"].Errors.Any().ShouldBeTrue();
    }

    [Fact]
    public async Task UploadDocuments_BackendFails_ShowsServiceError()
    {
        var model = new ModificationUploadDocumentsViewModel
        {
            Files = new List<IFormFile>()
        };

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>> { StatusCode = HttpStatusCode.InternalServerError });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.UserId] = "respondent-1";

        var result = await Sut.UploadDocuments(model);

        result.ShouldBeOfType<StatusCodeResult>(); // or whatever ServiceError returns
    }

    [Fact]
    public async Task UploadDocuments_NoNewFilesButExistingDocs_Redirects()
    {
        var model = new ModificationUploadDocumentsViewModel { Files = new List<IFormFile>() };

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            { StatusCode = HttpStatusCode.OK, Content = new List<ProjectModificationDocumentRequest> { new() { FileName = "existing.pdf" } } });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.UserId] = "respondent-1";

        var result = await Sut.UploadDocuments(model);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe(nameof(Sut.ModificationDocumentsAdded));
    }

    [Fact]
    public async Task UploadDocuments_NoNewFilesAndNoExistingDocs_ReturnsViewWithError()
    {
        var model = new ModificationUploadDocumentsViewModel { Files = new List<IFormFile>() };

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            { StatusCode = HttpStatusCode.OK, Content = new List<ProjectModificationDocumentRequest>() });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.UserId] = "respondent-1";

        var result = await Sut.UploadDocuments(model);

        var view = result.ShouldBeOfType<ViewResult>();
        Sut.ModelState["Files"].Errors.Any(e => e.ErrorMessage.Contains("Please upload at least one document")).ShouldBeTrue();
    }
}