using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Domain.Entities;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationControllerTests;

public class DocumentUploadTests : TestServiceBase<ApplicationController>
{
    [Fact]
    public void DocumentUpload_WithDocumentsInTempData_ReturnsViewWithDocuments()
    {
        // Arrange
        var documents = new List<Document>
        {
            new() { Name = "doc1.pdf", Size = 1024, Type = ".pdf" },
            new() { Name = "doc2.docx", Size = 2048, Type = ".docx" }
        };

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.UploadedDocuments] = JsonSerializer.Serialize(documents)
        };

        // Act
        var result = Sut.DocumentUpload();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<List<Document>>();
        model.ShouldBeEquivalentTo(documents);
    }

    [Fact]
    public void DocumentUpload_WithoutDocumentsInTempData_ReturnsViewWithNullModel()
    {
        // Arrange
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.UploadedDocuments] = null
        };

        // Act
        var result = Sut.DocumentUpload();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeNull();
    }
}