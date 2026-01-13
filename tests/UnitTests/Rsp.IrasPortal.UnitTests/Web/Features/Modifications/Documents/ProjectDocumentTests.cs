using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Web.Features.Modifications.Documents.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications.Documents;

public class ProjectDocumentTests : TestServiceBase<DocumentsController>
{
    [Theory, AutoData]
    public async Task ProjectDocument_ReturnsView_WithCorrectViewModel(
        string shortTitle,
        string irasId,
        string modificationId,
        string specificAreaOfChange
    )
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ShortProjectTitle] = shortTitle,
            [TempDataKeys.IrasId] = irasId,
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = modificationId,
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = specificAreaOfChange
        };

        Sut.TempData = tempData;

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        // Act
        var result = await Sut.ProjectDocument();

        // Assert
        result.ShouldBeOfType<ViewResult>();
        var viewResult = result as ViewResult;

        viewResult!.ViewName.ShouldBe("UploadDocuments");
        viewResult.Model.ShouldBeOfType<ModificationUploadDocumentsViewModel>();

        var model = viewResult.Model as ModificationUploadDocumentsViewModel;
        model!.ShortTitle.ShouldBe(shortTitle);
        model.IrasId.ShouldBe(irasId);
        model.ModificationIdentifier.ShouldBe(modificationId);
    }

    [Fact]
    public async Task ProjectDocument_ReturnsView_WithEmptyFallbackValues()
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.IrasId] = null,
            [TempDataKeys.ShortProjectTitle] = null,
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = null
        };

        Sut.TempData = tempData;

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        // Act
        var result = await Sut.ProjectDocument();

        // Assert
        result.ShouldBeOfType<ViewResult>();
        var viewResult = result as ViewResult;

        viewResult!.ViewName.ShouldBe("UploadDocuments");
        viewResult.Model.ShouldBeOfType<ModificationUploadDocumentsViewModel>();

        var model = viewResult.Model as ModificationUploadDocumentsViewModel;
        model!.ShortTitle.ShouldBeEmpty();
        model.IrasId.ShouldBeEmpty();
        model.ModificationIdentifier.ShouldBeEmpty();
        model.SpecificAreaOfChange.ShouldBeEmpty();
    }
}