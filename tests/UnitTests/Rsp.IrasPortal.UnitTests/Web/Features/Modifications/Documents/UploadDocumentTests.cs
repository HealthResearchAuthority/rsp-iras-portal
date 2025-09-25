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
    [Theory, AutoData]
    public async Task UploadDocuments_ValidInput_UploadsFilesAndRedirects
    (
        string irasId,
        string respondentId,
        Guid changeId,
        string projectRecordId,
        string fileName,
        long fileSize
    )
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var memoryStream = new MemoryStream(new byte[fileSize]);
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(fileSize);
        fileMock.Setup(f => f.OpenReadStream()).Returns(memoryStream);

        var model = new ModificationUploadDocumentsViewModel
        {
            IrasId = irasId,
            Files = new List<IFormFile> { fileMock.Object }
        };

        var uploadedDto = new DocumentSummaryItemDto
        {
            BlobUri = $"blob/path/{fileName}",
            FileName = fileName,
            FileSize = fileSize
        };

        Mocker
            .GetMock<IBlobStorageService>()
            .Setup(b => b.UploadFilesAsync(model.Files, It.IsAny<string>(), model.IrasId.ToString()))
            .ReturnsAsync(new List<DocumentSummaryItemDto> { uploadedDto });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = changeId,
            [TempDataKeys.ProjectRecordId] = projectRecordId,
            [TempDataKeys.IrasId] = model.IrasId
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Items = { [ContextItemKeys.RespondentId] = respondentId }
            }
        };

        // Act
        var result = await Sut.UploadDocuments(model);

        // Assert
        result.ShouldBeOfType<RedirectToActionResult>();
        var redirect = result as RedirectToActionResult;
        redirect!.ActionName.ShouldBe("ModificationDocumentsAdded");
    }

    [Fact]
    public async Task UploadDocuments_WhenBackendFails_ReturnsServiceError()
    {
        // Arrange
        var model = new ModificationUploadDocumentsViewModel { Files = new List<IFormFile>() };
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var response = new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>> { StatusCode = HttpStatusCode.InternalServerError };
        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var result = await Sut.UploadDocuments(model);

        // Assert
        var serviceErrorResult = Assert.IsType<StatusCodeResult>(result); // Assuming ServiceError returns ViewResult
        Assert.Equal(serviceErrorResult.StatusCode, StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task UploadDocuments_WhenDocumentsExist_RedirectsToReview()
    {
        // Arrange
        var model = new ModificationUploadDocumentsViewModel { Files = new List<IFormFile>() };
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var response = new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new List<ProjectModificationDocumentRequest> { new() { FileName = "doc1.pdf" } }
        };

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var result = await Sut.UploadDocuments(model);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(Sut.ModificationDocumentsAdded), redirect.ActionName);
    }

    [Fact]
    public async Task UploadDocuments_WhenNoFilesAndNoExistingDocuments_AddsModelError()
    {
        // Arrange
        var model = new ModificationUploadDocumentsViewModel { Files = new List<IFormFile>() };
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationChangeId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "record-123",
            [TempDataKeys.IrasId] = 999
        };
        Sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var response = new ServiceResponse<IEnumerable<ProjectModificationDocumentRequest>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new List<ProjectModificationDocumentRequest>() // no documents
        };

        Mocker.GetMock<IRespondentService>()
            .Setup(s => s.GetModificationChangesDocuments(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var result = await Sut.UploadDocuments(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var returnedModel = Assert.IsType<ModificationUploadDocumentsViewModel>(viewResult.Model);

        Assert.True(Sut.ModelState.ContainsKey("Files"));
        Assert.Equal("Please upload at least one document", Sut.ModelState["Files"].Errors[0].ErrorMessage);
    }
}