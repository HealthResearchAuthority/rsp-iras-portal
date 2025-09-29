using Azure.Storage.Blobs;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.BlobStorageServiceTests;

public class DeleteFileTests
{
    private readonly Mock<BlobServiceClient> _blobServiceClientMock;
    private readonly Mock<BlobContainerClient> _containerClientMock;
    private readonly Mock<BlobClient> _blobClientMock;
    private readonly BlobStorageService _sut; // The class that contains DeleteFileAsync

    public DeleteFileTests()
    {
        _blobServiceClientMock = new Mock<BlobServiceClient>();
        _containerClientMock = new Mock<BlobContainerClient>();
        _blobClientMock = new Mock<BlobClient>();

        // Setup mocks
        _blobServiceClientMock
            .Setup(x => x.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(_containerClientMock.Object);

        _containerClientMock
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(_blobClientMock.Object);

        _sut = new BlobStorageService(_blobServiceClientMock.Object);
    }

    [Fact]
    public async Task Throws_When_BlobPath_IsNull()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => _sut.DeleteFileAsync("test-container", null));
    }

    [Fact]
    public async Task Throws_When_BlobPath_IsEmpty()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => _sut.DeleteFileAsync("test-container", ""));
    }

    [Fact]
    public async Task CreatesContainer_AndDeletesBlob()
    {
        // Arrange
        var containerName = "test-container";
        var blobPath = "folder/file.txt";

        // Act
        await _sut.DeleteFileAsync(containerName, blobPath);

        // Assert
        _blobServiceClientMock.Verify(x => x.GetBlobContainerClient(containerName), Times.Once);
        _containerClientMock.Verify(x => x.GetBlobClient(blobPath), Times.Once);
    }
}