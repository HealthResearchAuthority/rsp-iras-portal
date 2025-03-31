﻿using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class ReviewBodyStatusChangesTests : TestServiceBase<ReviewBodyController>

{
    [Theory,AutoData]
    public void ReviewBodyStatusChanges_DisableReviewBody(AddUpdateReviewBodyModel addUpdateReviewBodyModel)
    {
        // Arrange
        Sut.ViewBag.Mode = "disable";

        // Act
        var result = Sut.ReviewBodyStatusChanges(addUpdateReviewBodyModel);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeAssignableTo<AddUpdateReviewBodyModel>();
    }

    [Theory,AutoData]
    public void ReviewBodyStatusChanges_EnableReviewBody(AddUpdateReviewBodyModel addUpdateReviewBodyModel)
    {
        // Arrange
        Sut.ViewBag.Mode = "enable";

        // Act
        var result = Sut.ReviewBodyStatusChanges(addUpdateReviewBodyModel);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeAssignableTo<AddUpdateReviewBodyModel>();
    }
}