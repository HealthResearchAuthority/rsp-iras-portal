using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Rsp.IrasPortal.Services;

namespace Rsp.IrasPortal.UnitTests.Services.BlobStorageServiceTests;

public class UploadFilesTests
{
    [Theory, AutoData]
    public async Task UploadFilesAsync_UploadsFilesToBlobStorage_AndReturnsSummary(
        string containerName,
        string folderPrefix,
        string fileName)
    {
        // Arrange
        var fileLength = 2048L;
        var stream = new MemoryStream(new byte[fileLength]);

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(fileLength);
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);

        var files = new List<IFormFile> { fileMock.Object };

        // mocks
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
            .Setup(b => b.GetBlobContainerClient(containerName))
            .Returns(containerClientMock.Object);

        var sut = new BlobStorageService(blobServiceClientMock.Object);

        // Act
        var result = await sut.UploadFilesAsync(files, containerName, folderPrefix);

        // Assert
        result.Count.ShouldBe(1);

        var uploaded = result.First();
        uploaded.FileName.ShouldBe(fileName);
        uploaded.FileSize.ShouldBe(fileLength);
        uploaded.BlobUri.ShouldContain(folderPrefix);
        uploaded.BlobUri.ShouldContain(fileName);
    }
}