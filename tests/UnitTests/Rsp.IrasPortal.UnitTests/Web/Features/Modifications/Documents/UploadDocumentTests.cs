using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications.Documents.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.Documents;

public class UploadDocumentTests : TestServiceBase<DocumentsController>
{
    [Fact]
    public async Task UploadDocuments_NullModel_ReturnsFileTooLargeView()
    {
        // Act
        var result = await Sut.UploadDocuments(null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("FileTooLarge", viewResult.ViewName);
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

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            { StatusCode = HttpStatusCode.OK, Content = new List<ProjectModificationDocumentRequest>() });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.RespondentId] = "respondent-1";

        var result = await Sut.UploadDocuments(model);

        Assert.IsType<ViewResult>(result);
        Assert.True(Sut.ModelState.ContainsKey("Files"));
        var error = Sut.ModelState["Files"].Errors.First();
        Assert.Contains("must be a permitted file type", error.ErrorMessage);
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

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            { StatusCode = HttpStatusCode.OK, Content = existingDocs });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.RespondentId] = "respondent-1";

        var result = await Sut.UploadDocuments(model);

        Assert.IsType<ViewResult>(result);
        Assert.True(Sut.ModelState["Files"].Errors.Any(e => e.ErrorMessage.Contains("already been uploaded")));
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
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            { StatusCode = HttpStatusCode.OK, Content = new List<ProjectModificationDocumentRequest>() });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.RespondentId] = "respondent-1";

        var result = await Sut.UploadDocuments(model);

        Assert.IsType<ViewResult>(result);
        Assert.True(Sut.ModelState["Files"].Errors.Any(e => e.ErrorMessage.Contains("must be smaller than 100 MB")));
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
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            { StatusCode = HttpStatusCode.OK, Content = new List<ProjectModificationDocumentRequest>() });

        Mocker.GetMock<IBlobStorageService>()
            .Setup(b => b.UploadFilesAsync(It.IsAny<IEnumerable<IFormFile>>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<DocumentSummaryItemDto>
            {
                new() { FileName = "good.pdf", BlobUri = "uri", FileSize = 1024 }
            });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.RespondentId] = "respondent-1";

        var result = await Sut.UploadDocuments(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(Sut.ModificationDocumentsAdded), redirect.ActionName);
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

        Mocker.GetMock<IRespondentService>().Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            { StatusCode = HttpStatusCode.OK, Content = new List<ProjectModificationDocumentRequest>() });

        Mocker.GetMock<IBlobStorageService>()
            .Setup(b => b.UploadFilesAsync(It.IsAny<IEnumerable<IFormFile>>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<DocumentSummaryItemDto>
            {
                new() { FileName = "good.pdf", BlobUri = "uri", FileSize = 1024 }
            });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.RespondentId] = "respondent-1";

        var result = await Sut.UploadDocuments(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.True(Sut.ModelState["Files"].Errors.Any());
    }

    [Fact]
    public async Task UploadDocuments_BackendFails_ShowsServiceError()
    {
        var model = new ModificationUploadDocumentsViewModel
        {
            Files = new List<IFormFile>()
        };

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>> { StatusCode = HttpStatusCode.InternalServerError });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.RespondentId] = "respondent-1";

        var result = await Sut.UploadDocuments(model);

        var view = Assert.IsType<StatusCodeResult>(result); // or whatever ServiceError returns
    }

    [Fact]
    public async Task UploadDocuments_NoNewFilesButExistingDocs_Redirects()
    {
        var model = new ModificationUploadDocumentsViewModel { Files = new List<IFormFile>() };

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            { StatusCode = HttpStatusCode.OK, Content = new List<ProjectModificationDocumentRequest> { new() { FileName = "existing.pdf" } } });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.RespondentId] = "respondent-1";

        var result = await Sut.UploadDocuments(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(Sut.ModificationDocumentsAdded), redirect.ActionName);
    }

    [Fact]
    public async Task UploadDocuments_NoNewFilesAndNoExistingDocs_ReturnsViewWithError()
    {
        var model = new ModificationUploadDocumentsViewModel { Files = new List<IFormFile>() };

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
            { StatusCode = HttpStatusCode.OK, Content = new List<ProjectModificationDocumentRequest>() });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        Sut.HttpContext.Items[ContextItemKeys.RespondentId] = "respondent-1";

        var result = await Sut.UploadDocuments(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.True(Sut.ModelState["Files"].Errors.Any(e => e.ErrorMessage.Contains("Please upload at least one document")));
    }
}