using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Web.Features.Modifications;
using Rsp.Portal.Web.Features.Modifications.Models;
using Rsp.Portal.Web.Helpers;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications.ReviewAllChangesControllerTests;

public class DownloadModificationPdfTests : TestServiceBase<ReviewAllChangesController>
{
    [Theory, AutoData]
    public async Task DownloadModificationPdfFromtHtml_Should_Return_Pdf_When_Successful
    (
        ReviewOutcomeViewModel model,
        byte[] pdf
    )
    {
        // Arrange
        var ctx = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = ctx };
        var tdProvider = Mocker.GetMock<ITempDataProvider>();
        var razorViewEngine = Mocker.GetMock<IRazorViewEngine>();

        Sut.TempData = new TempDataDictionary(ctx, tdProvider.Object)
        {
            [TempDataKeys.ProjectModification.ProjectModificationsDetails] = JsonSerializer.Serialize(model)
        };

        const string html = "<html><body><p>Test<p></body></html>";
        Mocker.GetMock<IViewHelper>()
            .Setup(vh => vh.RenderViewAsString(
                It.IsAny<string>(),
                It.IsAny<ModificationDetailsViewModel>(),
                It.IsAny<ControllerContext>()))
            .ReturnsAsync(html);

        Mocker.GetMock<IViewHelper>()
            .Setup(vh => vh.GeneratePdf(
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(pdf);

        // Act
        var result = await Sut.DownloadModificationPdfFromHtml();

        // Assert
        var fileResult = result.ShouldBeOfType<FileContentResult>();
        fileResult.ShouldNotBeNull();
        fileResult.ContentType.ShouldBe("application/pdf");
    }

    [Theory, AutoData]
    public async Task DownloadModificationPdfFromtHtml_Should_Return_ServiceError_When_Unsuccessful
    (
        ReviewOutcomeViewModel model
    )
    {
        // Arrange
        var ctx = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = ctx };
        Sut.TempData = new TempDataDictionary(ctx, Mock.Of<ITempDataProvider>());

        // Act
        var result = await Sut.DownloadModificationPdfFromHtml();

        // Assert
        var errorResult = result.ShouldBeOfType<StatusCodeResult>();
        errorResult.StatusCode.ShouldBe(404);
    }
}