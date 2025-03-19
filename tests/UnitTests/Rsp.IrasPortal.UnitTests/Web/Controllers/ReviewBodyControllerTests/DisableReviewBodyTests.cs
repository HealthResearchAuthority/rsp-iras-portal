using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class DisableReviewBodyTests : TestServiceBase<ReviewBodyController>

{
    [Theory, AutoData]
    public void DisableReviewBody_WithValidModel_ShouldReturnDisableReviewBodyView(AddUpdateReviewBodyModel model)
    {
        // Act
        var result = Sut.DisableReviewBody(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("SuccessMessage");
        viewResult.Model.ShouldBeEquivalentTo(model);
    }
}