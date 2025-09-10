using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Features.Modifications;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.ProjectModifiationControllerTests;

public class AddAnotherDocumentTests : TestServiceBase<ModificationsController>
{
    [Fact]
    public void AddAnotherDocument_RedirectsToUploadDocuments()
    {
        // Act
        var result = Sut.AddAnotherDocument();

        // Assert
        result.ShouldBeOfType<RedirectToActionResult>();
        var redirectResult = result as RedirectToActionResult;
        redirectResult!.ActionName.ShouldBe(nameof(ModificationsController.UploadDocuments));
    }
}