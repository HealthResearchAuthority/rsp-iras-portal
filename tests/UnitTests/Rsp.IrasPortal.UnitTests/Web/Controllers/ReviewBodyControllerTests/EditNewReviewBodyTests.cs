using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class EditNewReviewBodyTests : TestServiceBase<ReviewBodyController>
{
    [Fact]
    public async Task EditNewReviewBody_ShouldReturnView()
    {
        // Arrange + Act
        var result = await Sut.EditNewReviewBody(It.IsAny<AddUpdateReviewBodyModel>());

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeAssignableTo<AddUpdateReviewBodyModel>();
    }
}