using System.IO.Compression;
using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.BlobStorageServiceTests;

public class DownloadFilesAsZipAsyncTests
{
    [Fact]
    public async Task DownloadFilesAsZipAsync_Returns_BadRequest_When_NoFilesProvided()
    {
        // Arrange
        var blobServiceClientMock = new Mock<BlobServiceClient>();
        var sut = new BlobStorageService();

        // Act
        var result = await sut.DownloadFilesAsZipAsync(
            blobServiceClientMock.Object,
            "container",
            new List<string>(),
            "documents.zip");

        // Assert
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        result.Content.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DownloadFilesAsZipAsync_Returns_BadRequest_When_FileNameEmpty()
    {
        // Arrange
        var blobServiceClientMock = new Mock<BlobServiceClient>();
        var sut = new BlobStorageService();

        var blobs = new List<string> { "folder/file1.txt" };

        // Act
        var result = await sut.DownloadFilesAsZipAsync(
            blobServiceClientMock.Object,
            "container",
            blobs,
            "");

        // Assert
        result.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        result.Content.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Theory, AutoData]
    public async Task DownloadFilesAsZipAsync_Returns_ZipArchive(
    string containerName,
    string saveAsFileName,
    string file1Content,
    string file2Content)
    {
        // Arrange
        var blob1Path = "folder/file1.txt";
        var blob2Path = "folder/file2.csv";

        var blob1Bytes = Encoding.UTF8.GetBytes(file1Content);
        var blob2Bytes = Encoding.UTF8.GetBytes(file2Content);

        var blobClient1 = new Mock<BlobClient>();
        blobClient1.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        blobClient1
        .Setup(b => b.DownloadToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
        .Callback<Stream, CancellationToken>((stream, _) =>
        {
            stream.Write(blob1Bytes);
            stream.Position = 0;
        })
        .ReturnsAsync(Response.FromValue(Mock.Of<Response>(), Mock.Of<Response>()));

        var blobClient2 = new Mock<BlobClient>();
        blobClient2.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        blobClient2
        .Setup(b => b.DownloadToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
        .Callback<Stream, CancellationToken>((stream, _) =>
        {
            stream.Write(blob1Bytes);
            stream.Position = 0;
        })
        .ReturnsAsync(Response.FromValue(Mock.Of<Response>(), Mock.Of<Response>()));

        var containerClientMock = new Mock<BlobContainerClient>();
        containerClientMock.Setup(c => c.GetBlobClient(blob1Path))
            .Returns(blobClient1.Object);

        containerClientMock.Setup(c => c.GetBlobClient(blob2Path))
            .Returns(blobClient2.Object);

        var blobServiceClientMock = new Mock<BlobServiceClient>();
        blobServiceClientMock.Setup(b => b.GetBlobContainerClient(containerName))
            .Returns(containerClientMock.Object);

        var sut = new BlobStorageService();

        // Act
        var result = await sut.DownloadFilesAsZipAsync(
            blobServiceClientMock.Object,
            containerName,
            new List<string> { blob1Path, blob2Path },
            saveAsFileName);

        // Assert
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        var fileResult = result.Content.ShouldBeOfType<FileStreamResult>();

        using var archive = new ZipArchive(fileResult.FileStream, ZipArchiveMode.Read);

        archive.Entries.Count.ShouldBe(2);

        var entry1 = archive.GetEntry("file1.txt");
        entry1.ShouldNotBeNull();

        var entry2 = archive.GetEntry("file2.csv");
        entry2.ShouldNotBeNull();
    }

    [Fact]
    public async Task DownloadFilesAsZipAsync_Skips_MissingBlobs()
    {
        // Arrange
        var blobPath = "folder/file1.txt";

        var blobClient = new Mock<BlobClient>();
        blobClient.Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        var containerClientMock = new Mock<BlobContainerClient>();
        containerClientMock.Setup(c => c.GetBlobClient(blobPath))
            .Returns(blobClient.Object);

        var blobServiceClientMock = new Mock<BlobServiceClient>();
        blobServiceClientMock.Setup(b => b.GetBlobContainerClient("container"))
            .Returns(containerClientMock.Object);

        var sut = new BlobStorageService();

        // Act
        var result = await sut.DownloadFilesAsZipAsync(
            blobServiceClientMock.Object,
            "container",
            new List<string> { blobPath },
            "documents");

        // Assert
        result.StatusCode.ShouldBe(HttpStatusCode.OK);

        var fileResult = result.Content.ShouldBeOfType<FileStreamResult>();

        using var archive = new ZipArchive(fileResult.FileStream, ZipArchiveMode.Read);

        archive.Entries.Count.ShouldBe(0);
    }
}