using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.BlobStorageServiceTests;

public class DeleteFileTests
{
    private readonly Mock<BlobServiceClient> _blobServiceClientMock;
    private readonly Mock<BlobContainerClient> _containerClientMock;
    private readonly Mock<BlobClient> _blobClientMock;

    private readonly BlobStorageService _sut;

    public DeleteFileTests()
    {
        _blobServiceClientMock = new Mock<BlobServiceClient>();
        _containerClientMock = new Mock<BlobContainerClient>();
        _blobClientMock = new Mock<BlobClient>();

        // Setup BlobServiceClient → BlobContainerClient
        _blobServiceClientMock
            .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(_containerClientMock.Object);

        // Setup BlobContainerClient → BlobClient
        _containerClientMock
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(_blobClientMock.Object);

        _sut = new BlobStorageService(); // No blob client injected anymore
    }

    [Fact]
    public async Task DeleteFileAsync_ValidBlob_DeletesAndReturnsOk()
    {
        // Arrange
        var containerName = "test-container";
        var blobPath = "folder/file.pdf";

        _containerClientMock
            .Setup(x => x.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, default))
            .ReturnsAsync(Mock.Of<Response<BlobContainerInfo>>());

        _blobClientMock
            .Setup(x => x.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, null, default))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        // Act
        var result = await _sut.DeleteFileAsync(
            _blobServiceClientMock.Object,
            containerName,
            blobPath
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        _blobServiceClientMock.Verify(
            x => x.GetBlobContainerClient(containerName),
            Times.Once);

        _containerClientMock.Verify(
            x => x.GetBlobClient(blobPath),
            Times.Once);
    }

    [Fact]
    public async Task DeleteFileAsync_EmptyBlobPath_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.DeleteFileAsync(
            _blobServiceClientMock.Object,
            "test-container",
            ""
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("Blob path cannot be null or empty.", result.Error);

        // Ensure nothing was called
        _blobServiceClientMock.Verify(
            x => x.GetBlobContainerClient(It.IsAny<string>()),
            Times.Never);
    }
}