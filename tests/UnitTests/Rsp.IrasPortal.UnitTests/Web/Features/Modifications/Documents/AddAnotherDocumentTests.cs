using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Features.Modifications.Documents.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.Documents;

public class AddAnotherDocumentTests : TestServiceBase<DocumentsController>
{
    [Fact]
    public void AddAnotherDocument_RedirectsToUploadDocuments()
    {
        // Act
        var result = Sut.AddAnotherDocument();

        // Assert
        result.ShouldBeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult!.ActionName.ShouldBe(nameof(DocumentsController.UploadDocuments));
    }
}