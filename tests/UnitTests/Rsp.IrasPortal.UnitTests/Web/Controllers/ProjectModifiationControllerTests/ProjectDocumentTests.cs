using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ProjectModifiationControllerTests;

public class ProjectDocumentTests : TestServiceBase<ProjectModificationController>
{
    [Theory, AutoData]
    public void ProjectDocument_ReturnsView_WithCorrectViewModel(
        string shortTitle,
        string irasId,
        string modificationId,
        string specificAreaOfChange
    )
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ShortProjectTitle] = shortTitle,
            [TempDataKeys.IrasId] = irasId,
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = modificationId,
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = specificAreaOfChange
        };

        Sut.TempData = tempData;

        // Act
        var result = Sut.ProjectDocument();

        // Assert
        result.ShouldBeOfType<ViewResult>();
        var viewResult = result as ViewResult;

        viewResult!.ViewName.ShouldBe("UploadDocuments");
        viewResult.Model.ShouldBeOfType<ModificationUploadDocumentsViewModel>();

        var model = viewResult.Model as ModificationUploadDocumentsViewModel;
        model!.ShortTitle.ShouldBe(shortTitle);
        model.IrasId.ShouldBe(irasId);
        model.ModificationIdentifier.ShouldBe(modificationId);
        model.PageTitle.ShouldBe($"Add documents for {specificAreaOfChange}");
    }

    [Fact]
    public void ProjectDocument_ReturnsView_WithEmptyFallbackValues()
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.IrasId] = null,
            [TempDataKeys.ShortProjectTitle] = null,
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = null
        };

        Sut.TempData = tempData;

        // Act
        var result = Sut.ProjectDocument();

        // Assert
        result.ShouldBeOfType<ViewResult>();
        var viewResult = result as ViewResult;

        viewResult!.ViewName.ShouldBe("UploadDocuments");
        viewResult.Model.ShouldBeOfType<ModificationUploadDocumentsViewModel>();

        var model = viewResult.Model as ModificationUploadDocumentsViewModel;
        model!.ShortTitle.ShouldBeEmpty();
        model.IrasId.ShouldBeEmpty();
        model.ModificationIdentifier.ShouldBeEmpty();
        model.PageTitle.ShouldBeEmpty();
    }
}