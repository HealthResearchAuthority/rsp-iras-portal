using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class ConfirmChangesTests : TestServiceBase<ReviewBodyController>

{
    [Theory]
    [AutoData]
    public void ConfirmChanges_WithValidModel_ShouldReturnConfirmView(AddUpdateReviewBodyModel model)
    {
        // Act
        var result = Sut.ConfirmChanges(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("ConfirmChanges");
        viewResult.Model.ShouldBeEquivalentTo(model);
    }

    [Theory]
    [AutoData]
    public void ConfirmChanges_WithInvalidModel_ShouldReturnCreateEditView(AddUpdateReviewBodyModel model)
    {
        // Arrange
        Sut.ModelState.AddModelError("ErrorKey", "Some error message");

        // Act
        var result = Sut.ConfirmChanges(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("CreateReviewBody");
        viewResult.Model.ShouldBeEquivalentTo(model);
    }
}