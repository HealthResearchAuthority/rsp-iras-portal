using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Web.Extensions;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Extensions.TempDataExtensionsTests;

public class PopulateBaseProjectModificationPropertiesTests : TestServiceBase
{
    [Fact]
    public void Should_Populate_BaseProjectModificationViewModel_From_TempData()
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ShortProjectTitle] = "ShortTitleTest",
            [TempDataKeys.IrasId] = "IRAS123",
            [TempDataKeys.ProjectModification.ProjectModificationIdentifier] = "MOD-456",
            [TempDataKeys.ProjectModification.SpecificAreaOfChangeText] = "PageTitleTest"
        };
        var model = new BaseProjectModificationViewModel();

        // Act
        tempData.PopulateBaseProjectModificationProperties(model);

        // Assert
        model.ShortTitle.ShouldBe("ShortTitleTest");
        model.IrasId.ShouldBe("IRAS123");
        model.ModificationIdentifier.ShouldBe("MOD-456");
        model.SpecificAreaOfChange.ShouldBe("PageTitleTest");
    }

    [Fact]
    public void Should_Populate_Empty_Strings_When_Keys_Missing()
    {
        // Arrange
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        var model = new BaseProjectModificationViewModel();

        // Act
        tempData.PopulateBaseProjectModificationProperties(model);

        // Assert
        model.ShortTitle.ShouldBe("");
        model.IrasId.ShouldBe("");
        model.ModificationIdentifier.ShouldBe("");
        model.SpecificAreaOfChange.ShouldBe("");
    }
}