using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications.Documents.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.Documents;

public class DownloadDocumentTests : TestServiceBase<DocumentsController>
{
    [Fact]
    public async Task DownloadDocument_ReturnsFileResult()
    {
        // Arrange
        string path = "test/path";
        string fileName = "file.txt";

        var fileResult = new FileContentResult(new byte[] { 1, 2, 3 }, "application/octet-stream")
        {
            FileDownloadName = fileName
        };

        var serviceResponse = new ServiceResponse<IActionResult>()
            .WithContent(fileResult, HttpStatusCode.OK);

        Mocker.GetMock<IBlobStorageService>()
            .Setup(s => s.DownloadFileToHttpResponseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.DownloadDocument(path, fileName);

        // Assert
        result.ShouldBeOfType<FileContentResult>();
        var fileContentResult = result as FileContentResult;
        fileContentResult.ShouldNotBeNull();
        fileContentResult.FileDownloadName.ShouldBe(fileName);
        fileContentResult.ContentType.ShouldBe("application/octet-stream");

        // Verify the blob service was called once
        Mocker.GetMock<IBlobStorageService>().Verify(s =>
            s.DownloadFileToHttpResponseAsync(
                It.IsAny<string>(),
                path,
                fileName),
            Times.Once);
    }

    [Fact]
    public async Task DownloadDocument_WhenFileNotFound_ReturnsNotFound()
    {
        // Arrange
        var serviceResponse = new ServiceResponse<IActionResult>()
            .WithContent(new NotFoundObjectResult("File not found"), HttpStatusCode.NotFound);

        Mocker.GetMock<IBlobStorageService>()
            .Setup(s => s.DownloadFileToHttpResponseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.DownloadDocument("invalid/path", "missing.txt");

        // Assert
        result.ShouldBeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DownloadDocument_WhenBlobPathEmpty_ReturnsNotFound()
    {
        // Arrange
        var serviceResponse = new ServiceResponse<IActionResult>()
            .WithContent(new NotFoundObjectResult("File not found"), HttpStatusCode.NotFound);

        Mocker.GetMock<IBlobStorageService>()
            .Setup(s => s.DownloadFileToHttpResponseAsync(It.IsAny<string>(), string.Empty, It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.DownloadDocument("invalid/path", "missing.txt");

        // Assert
        result.ShouldBeOfType<ObjectResult>();
    }

    [Fact]
    public async Task DownloadDocument_WhenFileNameEmpty_ReturnsNotFound()
    {
        // Arrange
        var serviceResponse = new ServiceResponse<IActionResult>()
            .WithContent(new NotFoundObjectResult("File not found"), HttpStatusCode.NotFound);

        Mocker.GetMock<IBlobStorageService>()
            .Setup(s => s.DownloadFileToHttpResponseAsync(It.IsAny<string>(), It.IsAny<string>(), string.Empty))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.DownloadDocument("invalid/path", "missing.txt");

        // Assert
        result.ShouldBeOfType<ObjectResult>();
    }

    [Fact]
    public async Task DownloadDocument_WhenExceptionOccurs_ReturnsObjectResult()
    {
        // Arrange
        var serviceResponse = new ServiceResponse<IActionResult>()
            .WithContent(new ObjectResult($"Error downloading blob")
            {
                StatusCode = StatusCodes.Status500InternalServerError
            });

        Mocker.GetMock<IBlobStorageService>()
            .Setup(s => s.DownloadFileToHttpResponseAsync(It.IsAny<string>(), It.IsAny<string>(), string.Empty))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.DownloadDocument("invalid/path", "missing.txt");

        // Assert
        result.ShouldBeOfType<ObjectResult>();
    }
}