using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.BlobStorageServiceTests;

public class DownloadFileToHttpResponseAsyncTests
{
    [Theory, AutoData]
    public async Task DownloadFileToHttpResponseAsync_Returns_FileStreamResult(
    string containerName,
    string blobPath,
    string fileName,
    string content)
    {
        // Arrange
        var blobContent = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        var blobProperties = BlobsModelFactory.BlobProperties(contentType: "text/plain");

        var blobClientMock = new Mock<BlobClient>();
        blobClientMock
            .Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        blobClientMock
        .Setup(b => b.DownloadToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
        .Callback<Stream, CancellationToken>((stream, _) =>
        {
            blobContent.Position = 0;
            blobContent.CopyTo(stream);
            stream.Position = 0;
        })
        .ReturnsAsync(Response.FromValue(Mock.Of<Response>(), Mock.Of<Response>()));

        blobClientMock
        .Setup(b => b.GetPropertiesAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Response.FromValue(blobProperties, Mock.Of<Response>()));

        var blobDownloadInfo = BlobsModelFactory.BlobDownloadStreamingResult(
        content: new MemoryStream(Encoding.UTF8.GetBytes("test file content")),
        details: BlobsModelFactory.BlobDownloadDetails(
            contentType: "text/plain"
        ));
        blobClientMock
            .Setup(b => b.DownloadStreamingAsync(It.IsAny<BlobDownloadOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(blobDownloadInfo, Mock.Of<Response>()));

        var containerClientMock = new Mock<BlobContainerClient>();
        containerClientMock
            .Setup(c => c.GetBlobClient(blobPath))
            .Returns(blobClientMock.Object);

        var blobServiceClientMock = new Mock<BlobServiceClient>();
        blobServiceClientMock
            .Setup(b => b.GetBlobContainerClient(containerName))
            .Returns(containerClientMock.Object);

        var sut = new BlobStorageService(blobServiceClientMock.Object);

        // Act
        var serviceResponse = await sut.DownloadFileToHttpResponseAsync(containerName, blobPath, fileName);

        // Assert
        serviceResponse.ShouldNotBeNull();
        serviceResponse.ShouldBeOfType<ServiceResponse<IActionResult>>();

        var result = serviceResponse.Content as FileStreamResult;
        result.ShouldNotBeNull();
        result.ShouldBeOfType<FileStreamResult>();

        result!.FileDownloadName.ShouldBe(fileName);
        result.ContentType.ShouldBe("text/plain");

        using var reader = new StreamReader(result.FileStream);
        var downloadedContent = await reader.ReadToEndAsync();
        downloadedContent.ShouldBe("test file content");
    }
}