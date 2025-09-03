using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ProjectModifiationControllerTests;

public class UploadDocumentTests : TestServiceBase<ProjectModificationController>
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
            [TempDataKeys.ProjectRecordId] = projectRecordId
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
}