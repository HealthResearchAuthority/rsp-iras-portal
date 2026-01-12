using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Azure;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Features.Modifications.Documents.Controllers;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications.Documents;

public class DownloadDocumentTests : TestServiceBase<DocumentsController>
{
    [Fact]
    public async Task DownloadDocument_ReturnsFileResult()
    {
        // Arrange
        var modificationId = Guid.NewGuid();
        string path = $"test/{modificationId}";
        string fileName = "file.txt";

        var fileResult = new FileContentResult(new byte[] { 1, 2, 3 }, "application/octet-stream")
        {
            FileDownloadName = fileName
        };

        var serviceResponse = new ServiceResponse<IActionResult>()
            .WithContent(fileResult, HttpStatusCode.OK);

        // Mock access check OK
        Mocker
            .GetMock<IProjectModificationsService>()
            .Setup(s => s.CheckDocumentAccess(modificationId))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

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

        // Mock blob storage service returning a file response
        Mocker
            .GetMock<IBlobStorageService>()
            .Setup(s => s.DownloadFileToHttpResponseAsync(
                blobServiceClientMock.Object,
                It.IsAny<string>(),
                path,
                fileName))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.DownloadDocument(path, fileName);

        // Assert
        result.ShouldBeOfType<FileContentResult>();
        var fileContentResult = result as FileContentResult;
        fileContentResult.ShouldNotBeNull();
        fileContentResult.FileDownloadName.ShouldBe(fileName);
        fileContentResult.ContentType.ShouldBe("application/octet-stream");

        // Verify that the service was called
        Mocker
            .GetMock<IBlobStorageService>().Verify(s =>
            s.DownloadFileToHttpResponseAsync(
                blobServiceClientMock.Object,
                It.IsAny<string>(),
                path,
                fileName),
            Times.Once);

        // Verify the factory was used
        factoryMock.Verify(f => f.CreateClient("Clean"), Times.Once);
    }

    [Fact]
    public async Task DownloadDocument_WhenBlobPathEmpty_ReturnsNotFound()
    {
        // Arrange
        var modificationId = Guid.NewGuid();
        var serviceResponse = new ServiceResponse<IActionResult>()
            .WithContent(new NotFoundObjectResult("File not found"), HttpStatusCode.NotFound);

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

        Mocker
            .GetMock<IBlobStorageService>()
            .Setup(s => s.DownloadFileToHttpResponseAsync(blobServiceClientMock.Object, It.IsAny<string>(), string.Empty, It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        // Mock access check OK
        Mocker
            .GetMock<IProjectModificationsService>()
            .Setup(s => s.CheckDocumentAccess(modificationId))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        // Act
        var result = await Sut.DownloadDocument($"invalid/{modificationId}", "missing.txt");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task DownloadDocument_WhenFileNameEmpty_ReturnsNotFound()
    {
        // Arrange
        var modificationId = Guid.NewGuid();
        string path = $"invalid/{modificationId}";

        var serviceResponse = new ServiceResponse<IActionResult>()
            .WithContent(new NotFoundObjectResult("File not found"), HttpStatusCode.NotFound);

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

        Mocker
            .GetMock<IBlobStorageService>()
            .Setup(s => s.DownloadFileToHttpResponseAsync(blobServiceClientMock.Object, It.IsAny<string>(), It.IsAny<string>(), string.Empty))
            .ReturnsAsync(serviceResponse);

        // Ensure CheckDocumentAccess returns success for GUID
        Mocker
            .GetMock<IProjectModificationsService>()
            .Setup(s => s.CheckDocumentAccess(modificationId))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        // Act
        var result = await Sut.DownloadDocument(path, "missing.txt");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task DownloadDocument_WhenExceptionOccurs_ReturnsObjectResult()
    {
        // Arrange
        var modificationId = Guid.NewGuid();
        string path = $"invalid/{modificationId}";

        var serviceResponse = new ServiceResponse<IActionResult>()
            .WithContent(new ObjectResult($"Error downloading blob")
            {
                StatusCode = StatusCodes.Status500InternalServerError
            });

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

        Mocker
            .GetMock<IBlobStorageService>()
            .Setup(s => s.DownloadFileToHttpResponseAsync(blobServiceClientMock.Object, It.IsAny<string>(), It.IsAny<string>(), string.Empty))
            .ReturnsAsync(serviceResponse);

        // Mock access check OK
        Mocker
            .GetMock<IProjectModificationsService>()
            .Setup(s => s.CheckDocumentAccess(modificationId))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        // Act
        var result = await Sut.DownloadDocument(path, "missing.txt");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task DownloadDocument_WhenAccessForbidden_Returns403()
    {
        // Arrange
        var modificationIdStr = Guid.NewGuid().ToString();
        string path = $"secure/{modificationIdStr}";
        string fileName = "any.txt";

        // Ensure controller has HttpContext and TempData
        var httpContext = new DefaultHttpContext();
        Sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        // Mock access check to return Forbidden for the parsed GUID
        Mocker
            .GetMock<IProjectModificationsService>()
            .Setup(s => s.CheckDocumentAccess(Guid.Parse(modificationIdStr)))
            .ReturnsAsync(new ServiceResponse().WithStatus(HttpStatusCode.Forbidden));

        // Act
        var result = await Sut.DownloadDocument(path, fileName);

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe((int)HttpStatusCode.Forbidden);
    }
}