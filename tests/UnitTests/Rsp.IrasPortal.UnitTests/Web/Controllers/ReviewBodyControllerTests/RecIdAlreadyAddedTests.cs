using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class RecIdAlreadyAddedTests : TestServiceBase<ReviewBodyController>
{
    [Theory, AutoData]
    public void RecIdAlreadyAdded_WithNewReviewBody_ShouldReturnViewWithCreateMode(
        AddUpdateReviewBodyModel model)
    {
        // Arrange
        model.Id = Guid.Empty;

        // Act
        var result = Sut.RecIdAlreadyAdded(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("RecIdAlreadyAdded");
        viewResult.Model.ShouldBeEquivalentTo(model);
    }

    [Theory, AutoData]
    public void RecIdAlreadyAdded_WithExistingReviewBody_ShouldReturnViewWithUpdateMode(
        AddUpdateReviewBodyModel model)
    {
        // Arrange
        model.Id = Guid.NewGuid();

        // Act
        var result = Sut.RecIdAlreadyAdded(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("RecIdAlreadyAdded");
        viewResult.Model.ShouldBeEquivalentTo(model);
    }
}