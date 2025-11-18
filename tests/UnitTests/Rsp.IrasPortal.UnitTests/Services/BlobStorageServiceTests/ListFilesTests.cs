using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.BlobStorageServiceTests;

public class ListFilesTests
{
    [Theory, AutoData]
    public async Task ListFilesAsync_ReturnsFileMetadataList(
        string containerName,
        string folderPrefix,
        string blobName,
        long fileSize)
    {
        // Arrange
        var blobUri = new Uri($"https://mock.blob.core.windows.net/{containerName}/{blobName}");

        var blobItem = BlobsModelFactory.BlobItem(
            name: $"{folderPrefix}/{blobName}",
            properties: BlobsModelFactory.BlobItemProperties(
                accessTierInferred: false,
                contentLength: fileSize
            )
        );

        var blobItems = new[] { blobItem };

        var asyncPageableMock = new Mock<AsyncPageable<BlobItem>>();
        asyncPageableMock
            .Setup(p => p.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<BlobItem>(blobItems));

        var blobClientMock = new Mock<BlobClient>();
        blobClientMock
            .Setup(b => b.Uri)
            .Returns(blobUri);

        var containerClientMock = new Mock<BlobContainerClient>();
        containerClientMock
            .Setup(c => c.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), folderPrefix, It.IsAny<CancellationToken>()))
            .Returns(asyncPageableMock.Object);
        containerClientMock
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(blobClientMock.Object);

        var blobServiceClientMock = new Mock<BlobServiceClient>();
        blobServiceClientMock
            .Setup(b => b.GetBlobContainerClient(containerName))
            .Returns(containerClientMock.Object);

        var sut = new BlobStorageService();

        // Act
        var result = await sut.ListFilesAsync(blobServiceClientMock.Object, containerName, folderPrefix);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);

        var file = result.First();
        file.FileName.ShouldBe(blobName);
        file.FileSize.ShouldBe(fileSize);
        file.BlobUri.ShouldBe(blobUri.ToString());
    }

    private class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _enumerator;

        public TestAsyncEnumerator(IEnumerable<T> items)
        {
            _enumerator = items.GetEnumerator();
        }

        public T Current => _enumerator.Current;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public ValueTask<bool> MoveNextAsync() =>
            new ValueTask<bool>(_enumerator.MoveNext());
    }
}