using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Domain.Entities;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Extensions;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationControllerTests;

public class UploadTests : TestServiceBase<ApplicationController>
{
    [Fact]
    public void Upload_WithValidFiles_AddsDocumentsToTempDataAndRedirects()
    {
        // Arrange
        var formFiles = new FormFileCollection();
        var file1 = new Mock<IFormFile>();
        file1
            .Setup(f => f.FileName)
            .Returns("test1.pdf");

        file1
            .Setup(f => f.Length)
            .Returns(1024);

        var file2 = new Mock<IFormFile>();
        file2
            .Setup(f => f.FileName)
            .Returns("test2.docx");

        file2
            .Setup(f => f.Length)
            .Returns(2048);

        formFiles.AddRange
        (
            [file1.Object, file2.Object]
        );

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        // Act
        var result = Sut.Upload(formFiles);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(ApplicationController.DocumentUpload));

        Sut.TempData
            .TryGetValue<List<Document>>(TempDataKeys.UploadedDocuments, out var capturedDocuments, true)
            .ShouldBeTrue();

        capturedDocuments.ShouldNotBeNull();
        capturedDocuments.Count.ShouldBe(2);
        capturedDocuments.ShouldContain
        (
            document =>
                document.Name == "test1.pdf" &&
                document.Size == 1024 &&
                document.Type == ".pdf"
        );

        capturedDocuments.ShouldContain
        (
            document =>
                document.Name == "test2.docx" &&
                document.Size == 2048 &&
                document.Type == ".docx"
        );
    }

    [Fact]
    public void Upload_WithNoFiles_RedirectsWithoutAddingToTempData()
    {
        // Arrange
        var formFiles = new FormFileCollection();

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        // Act
        var result = Sut.Upload(formFiles);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(ApplicationController.DocumentUpload));

        Sut.TempData
            .TryGetValue<List<Document>>(TempDataKeys.UploadedDocuments, out var capturedDocuments, true)
            .ShouldBeTrue();

        capturedDocuments.ShouldBeEmpty();
    }
}