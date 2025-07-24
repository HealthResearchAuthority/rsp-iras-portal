using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationControllerTests;

public class ProjectOverview : TestServiceBase<ApplicationController>
{
    [Fact]
    public async Task ProjectOverview_UsesTempData_AndReturnsViewResult()
    {
        // Arrange
        var tempDataProvider = new Mock<ITempDataProvider>();

        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), tempDataProvider.Object)
        {
            [TempDataKeys.ShortProjectTitle] = "Test Project",
            [TempDataKeys.CategoryId] = QuestionCategories.ProjectRecrod,
            [TempDataKeys.ProjectRecordId] = "456"
        };

        // Act
        var result = await Sut.ProjectOverview(null, null);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeOfType<ProjectOverviewModel>();

        model.ProjectTitle.ShouldBe("Test Project");
        model.CategoryId.ShouldBe(QuestionCategories.ProjectRecrod);
        model.ProjectRecordId.ShouldBe("456");
    }

    [Fact]
    public async Task ProjectOverview_SetsNotificationBanner_WhenProjectModificationIdExists()
    {
        // Arrange
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = new TempDataDictionary(new DefaultHttpContext(), tempDataProvider.Object)
        {
            [TempDataKeys.ProjectModificationId] = "mod-1"
        };
        Sut.TempData = tempData;

        // Act
        await Sut.ProjectOverview(null, null);

        // Assert
        tempData[TempDataKeys.ShowNotificationBanner].ShouldBe(true);
    }

    [Fact]
    public async Task ProjectOverview_RemovesModificationRelatedTempDataKeys()
    {
        // Arrange
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = new TempDataDictionary(new DefaultHttpContext(), tempDataProvider.Object)
        {
            [TempDataKeys.ProjectModificationId] = "mod-1",
            [TempDataKeys.ProjectModificationIdentifier] = "ident-1",
            [TempDataKeys.ProjectModificationChangeId] = "chg-1",
            [TempDataKeys.ProjectModificationSpecificArea] = "area-1"
        };
        Sut.TempData = tempData;

        // Act
        await Sut.ProjectOverview(null, null);

        // Assert
        tempData.ContainsKey(TempDataKeys.ProjectModificationId).ShouldBeFalse();
        tempData.ContainsKey(TempDataKeys.ProjectModificationIdentifier).ShouldBeFalse();
        tempData.ContainsKey(TempDataKeys.ProjectModificationChangeId).ShouldBeFalse();
        tempData.ContainsKey(TempDataKeys.ProjectModificationSpecificArea).ShouldBeFalse();
    }

    [Fact]
    public async Task ProjectOverview_SetsProjectOverviewTempDataKey()
    {
        // Arrange
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = new TempDataDictionary(new DefaultHttpContext(), tempDataProvider.Object);
        Sut.TempData = tempData;

        // Act
        await Sut.ProjectOverview(null, null);

        // Assert
        tempData[TempDataKeys.ProjectOverview].ShouldBe(true);
    }
}