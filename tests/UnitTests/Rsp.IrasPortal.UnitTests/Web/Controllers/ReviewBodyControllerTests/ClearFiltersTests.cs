using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class ClearFiltersTests : TestServiceBase<ReviewBodyController>
{
    [Fact]
    public void ClearFilters_ShouldRedirectToViewReviewBodies()
    {
        // Act
        var result = Sut.ClearFilters();

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe("ViewReviewBodies");
    }
}