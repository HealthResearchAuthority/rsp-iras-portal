using System.Reflection;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Features.Modifications.Documents.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.Documents;

public class DownloadDocumentsAsZipTests : TestServiceBase<DocumentsController>
{
    [Fact]
    public async Task DownloadDocumentsAsZip_Returns_FileResult_WithZip()
    {
        // Arrange
        var folderName = "ChildFolder";
        var cleanedFolderName = "358577/ChildFolder";
        var expectedFileName = "MOD-1-" + DateTime.UtcNow.ToString("ddMMMyy");

        var blobClientMock = new Mock<BlobServiceClient>();
        var blobStorageServiceMock = new Mock<IBlobStorageService>();

        var expectedBytes = new byte[] { 1, 2, 3, 4 };

        blobStorageServiceMock
            .Setup(s => s.DownloadFolderAsZipAsync(
                It.IsAny<BlobServiceClient>(),
                "clean-container",
                cleanedFolderName,
                expectedFileName))
            .ReturnsAsync((expectedBytes, expectedFileName + ".zip"));

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = Guid.NewGuid(),
            [TempDataKeys.IrasId] = 999,
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await Sut.DownloadDocumentsAsZip(folderName);

        // Assert
        result.ShouldBeOfType<FileContentResult>();
        var file = result as FileContentResult;

        file!.ContentType.ShouldBe("application/zip");
    }

    [Fact]
    public void BuildZipFileName_WhenIdentifierIsNull_ReturnsDefaultFormat()
    {
        // Act
        var result = InvokeBuildZipFileName(null);

        // Assert
        result.ShouldStartWith("Documents-");
        result.ShouldEndWith(DateTime.UtcNow.ToString("ddMMMyy"));
    }

    [Fact]
    public void BuildZipFileName_WhenIdentifierIsEmpty_ReturnsDefaultFormat()
    {
        // Act
        var result = InvokeBuildZipFileName("");

        // Assert
        result.ShouldStartWith("Documents-");
        result.ShouldEndWith(DateTime.UtcNow.ToString("ddMMMyy"));
    }

    [Fact]
    public void BuildZipFileName_WhenIdentifierContainsSlash_ReplacesSlash()
    {
        // Arrange
        string identifier = "MOD/123";

        // Act
        var result = InvokeBuildZipFileName(identifier);

        // Assert
        result.ShouldStartWith("MOD-123-");
        result.ShouldEndWith(DateTime.UtcNow.ToString("ddMMMyy"));
        result.ShouldNotContain("/");
    }

    private static string InvokeBuildZipFileName(string? value)
    {
        var method = typeof(DocumentsController)
            .GetMethod("BuildZipFileName", BindingFlags.NonPublic | BindingFlags.Static);

        return (string)method!.Invoke(null, new object?[] { value })!;
    }
}