using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ProjectModifiationControllerTests;

public class AddAnotherDocumentTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public void AddAnotherDocument_RedirectsToUploadDocuments()
    {
        // Act
        var result = Sut.AddAnotherDocument();

        // Assert
        result.ShouldBeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult!.ActionName.ShouldBe(nameof(ProjectModificationController.UploadDocuments));
    }
}