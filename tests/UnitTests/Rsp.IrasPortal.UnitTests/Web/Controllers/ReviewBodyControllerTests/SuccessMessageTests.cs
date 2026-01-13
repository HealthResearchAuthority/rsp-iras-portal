using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class SuccessMessageTests : TestServiceBase<ReviewBodyController>

{
    [Theory]
    [AutoData]
    public void SuccessMessage_WithValidModel_ShouldReturnSuccessMessageView(AddUpdateReviewBodyModel model)
    {
        // Act
        var result = Sut.SuccessMessage(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("SuccessMessage");
        viewResult.Model.ShouldBeEquivalentTo(model);
    }
}