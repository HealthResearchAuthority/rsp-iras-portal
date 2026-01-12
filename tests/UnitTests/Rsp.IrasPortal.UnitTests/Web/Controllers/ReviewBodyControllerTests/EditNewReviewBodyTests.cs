using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class EditNewReviewBodyTests : TestServiceBase<ReviewBodyController>
{
    [Fact]
    public void EditNewReviewBody_ShouldReturnView()
    {
        // Arrange + Act
        var result = Sut.EditNewReviewBody(It.IsAny<AddUpdateReviewBodyModel>());

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeAssignableTo<AddUpdateReviewBodyModel>();
    }
}