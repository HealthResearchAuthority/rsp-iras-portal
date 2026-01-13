using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Web.Features.Modifications;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications.ModificationDetailsControllerTests;

public class RedirectToActionTests : TestServiceBase<ModificationDetailsController>
{
    // --------------------------------------------------------------
    // UnfinishedChanges
    // --------------------------------------------------------------
    [Fact]
    public void UnfinishedChanges_Should_Return_Correct_View_And_Model()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "PR1",
            [TempDataKeys.IrasId] = "12345",
            [TempDataKeys.ShortProjectTitle] = "Short"
        };

        // Act
        var result = Sut.UnfinishedChanges();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("UnfinishedChanges");

        viewResult.Model.ShouldBeOfType<BaseProjectModificationViewModel>();
    }

    // --------------------------------------------------------------
    // NoChangesToSubmit
    // --------------------------------------------------------------
    [Fact]
    public void NoChangesToSubmit_Should_Return_Correct_View_And_Model()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "PR1",
            [TempDataKeys.IrasId] = "12345",
            [TempDataKeys.ShortProjectTitle] = "Short"
        };

        // Act
        var result = Sut.NoChangesToSubmit();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("NoChangesToSubmit");

        viewResult.Model.ShouldBeOfType<BaseProjectModificationViewModel>();
    }

    // --------------------------------------------------------------
    // DocumentsScanInProgress
    // --------------------------------------------------------------
    [Fact]
    public void DocumentsScanInProgress_Should_Return_Correct_View_And_Model()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "PR1",
            [TempDataKeys.IrasId] = "12345",
            [TempDataKeys.ShortProjectTitle] = "Short"
        };

        // Act
        var result = Sut.DocumentsScanInProgress();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("DocumentsScanInProgress");

        viewResult.Model.ShouldBeOfType<BaseProjectModificationViewModel>();
    }

    // --------------------------------------------------------------
    // DocumentDetailsIncomplete
    // --------------------------------------------------------------
    [Fact]
    public void DocumentDetailsIncomplete_Should_Return_Correct_View_And_Model()
    {
        // Arrange
        var http = new DefaultHttpContext();
        Sut.ControllerContext = new() { HttpContext = http };
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectModification.ProjectModificationId] = Guid.NewGuid(),
            [TempDataKeys.ProjectRecordId] = "PR1",
            [TempDataKeys.IrasId] = "12345",
            [TempDataKeys.ShortProjectTitle] = "Short"
        };

        // Act
        var result = Sut.DocumentDetailsIncomplete();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("DocumentDetailsIncomplete");

        viewResult.Model.ShouldBeOfType<BaseProjectModificationViewModel>();
    }
}