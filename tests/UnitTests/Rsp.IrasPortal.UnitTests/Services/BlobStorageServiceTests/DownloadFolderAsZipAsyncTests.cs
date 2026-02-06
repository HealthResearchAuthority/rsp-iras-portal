using System.IO.Compression;
using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Rsp.Portal.Services;

namespace Rsp.Portal.UnitTests.Services.BlobStorageServiceTests;

public class DownloadFolderAsZipAsyncTests
{
    [Theory, AutoData]
    public async Task DownloadFolderAsZipAsync_Returns_ZipArchive(
    string containerName,
    string folderName,
    string saveAsFileName,
    string file1Content,
    string file2Content)
    {
        // Arrange
        folderName = folderName.TrimEnd('/');   // ensure realistic formatting
        string blob1Name = $"{folderName}/file1.txt";
        string blob2Name = $"{folderName}/nested/file2.csv";

        var blob1Bytes = Encoding.UTF8.GetBytes(file1Content);
        var blob2Bytes = Encoding.UTF8.GetBytes(file2Content);

        // Create BlobItem mocks
        var blobItems = new[]
        {
        BlobsModelFactory.BlobItem(name: blob1Name),
        BlobsModelFactory.BlobItem(name: blob2Name)
    };

        // Mock Pageable returned from GetBlobsAsync
        var containerClientMock = new Mock<BlobContainerClient>();
        containerClientMock
            .Setup(c => c.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), default))
            .Returns(AsyncPageableMock.Create(blobItems));

        // Mock BlobClient 1
        var blobClientMock1 = new Mock<BlobClient>();
        blobClientMock1
        .Setup(b => b.DownloadToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
        .Callback<Stream, CancellationToken>((stream, _) =>
        {
            stream.Write(blob1Bytes);
            stream.Position = 0;
        })
        .ReturnsAsync(Response.FromValue(Mock.Of<Response>(), Mock.Of<Response>()));

        containerClientMock
            .Setup(c => c.GetBlobClient(blob1Name))
            .Returns(blobClientMock1.Object);

        // Mock BlobClient 2
        var blobClientMock2 = new Mock<BlobClient>();
        blobClientMock2
            .Setup(b => b.DownloadToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, CancellationToken>((stream, _) =>
            {
                stream.Write(blob2Bytes);
                stream.Position = 0;
            })
        .ReturnsAsync(Response.FromValue(Mock.Of<Response>(), Mock.Of<Response>()));

        containerClientMock
            .Setup(c => c.GetBlobClient(blob2Name))
            .Returns(blobClientMock2.Object);

        // Mock BlobServiceClient
        var blobServiceClientMock = new Mock<BlobServiceClient>();
        blobServiceClientMock
            .Setup(b => b.GetBlobContainerClient(containerName))
            .Returns(containerClientMock.Object);

        var sut = new BlobStorageService();

        // Act
        var result = await sut.DownloadFolderAsZipAsync(
            blobServiceClientMock.Object,
            containerName,
            folderName,
            saveAsFileName);

        // Assert
        result.FileBytes.ShouldNotBeNull();
        result.FileBytes.Length.ShouldBeGreaterThan(0);
        result.FileName.ShouldBe($"{saveAsFileName}.zip");

        // Validate ZIP
        using var zipStream = new MemoryStream(result.FileBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        archive.Entries.Count.ShouldBe(2);

        // Check file1.txt
        var entry1 = archive.GetEntry("file1.txt");
        entry1.ShouldNotBeNull();
        using (var reader = new StreamReader(entry1.Open()))
        {
            (await reader.ReadToEndAsync()).ShouldNotBe(file1Content);
        }

        // Check nested/file2.csv
        var entry2 = archive.GetEntry("nested/file2.csv");
        entry2.ShouldNotBeNull();
        using (var reader = new StreamReader(entry2.Open()))
        {
            (await reader.ReadToEndAsync()).ShouldNotBe(file2Content);
        }
    }
}

public static class AsyncPageableMock
{
    public static AsyncPageable<T> Create<T>(IEnumerable<T> items) where T : notnull
    {
        return new MockAsyncPageable<T>(items);
    }

    private class MockAsyncPageable<T> : AsyncPageable<T> where T : notnull
    {
        private readonly IEnumerable<T> _items;

        public MockAsyncPageable(IEnumerable<T> items)
        {
            _items = items;
        }

        public override IAsyncEnumerable<Page<T>> AsPages(string? continuationToken = null, int? pageSizeHint = null)
        {
            throw new NotImplementedException("AsPages is not implemented for this mock.");
        }

        public override async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            foreach (var item in _items)
            {
                yield return item;
                await Task.Yield();
            }
        }
    }
}