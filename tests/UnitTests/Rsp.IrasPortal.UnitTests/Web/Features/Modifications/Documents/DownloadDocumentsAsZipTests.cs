using System.Reflection;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Azure;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.Responses;
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

        var blobClientMock = new Mock<BlobServiceClient>();

        // Register the blob client factory to return our blob client mock
        var factoryMock = Mocker.GetMock<IAzureClientFactory<BlobServiceClient>>();
        factoryMock
            .Setup(f => f.CreateClient("Clean"))
            .Returns(blobClientMock.Object);

        // Use the Mocker to setup the IBlobStorageService used by the controller
        var expectedBytes = new byte[] { 1, 2, 3, 4 };

        Mocker.GetMock<IBlobStorageService>()
            .Setup(s => s.DownloadFolderAsZipAsync(
                It.IsAny<BlobServiceClient>(),
                "clean-container",
                cleanedFolderName,
                It.IsAny<string>()))
            .ReturnsAsync((expectedBytes, "documents.zip"));

        var modificationGuid = Guid.NewGuid().ToString();

        // Mock access check for the GUID
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.CheckDocumentAccess(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse { StatusCode = HttpStatusCode.OK });

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            // Store the identifier as a string GUID so the controller can parse it
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = modificationGuid,
            [TempDataKeys.ProjectModification.ProjectModificationId] = modificationGuid,
            [TempDataKeys.IrasId] = 358577,
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

    [Fact]
    public async Task DownloadDocumentsAsZip_WhenAccessForbidden_Returns403()
    {
        // Arrange
        var folderName = "ChildFolder";
        var modificationGuid = Guid.NewGuid().ToString();

        // Mock access check forbidden
        Mocker.GetMock<IProjectModificationsService>()
            .Setup(s => s.CheckDocumentAccess(It.IsAny<Guid>()))
            .ReturnsAsync(new ServiceResponse().WithStatus(HttpStatusCode.Forbidden));

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = modificationGuid,
            [TempDataKeys.ProjectModification.ProjectModificationId] = modificationGuid,
            [TempDataKeys.IrasId] = 358577,
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await Sut.DownloadDocumentsAsZip(folderName);

        // Assert
        var status = result.ShouldBeOfType<StatusCodeResult>();
        status.StatusCode.ShouldBe((int)HttpStatusCode.Forbidden);
    }
}