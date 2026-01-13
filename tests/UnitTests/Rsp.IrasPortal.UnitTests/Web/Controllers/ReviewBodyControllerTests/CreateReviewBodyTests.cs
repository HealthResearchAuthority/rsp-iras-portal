using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class CreateReviewBodyTests : TestServiceBase<ReviewBodyController>
{
    [Fact]
    public Task CreateReviewBody_ShouldReturnView()
    {
        // Arrange + Act
        var result = Sut.CreateReviewBody();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeAssignableTo<AddUpdateReviewBodyModel>();
        return Task.CompletedTask;
    }

    [Theory, AutoData]
    public void CreateReviewBody_ShouldReturnViewWithOrderedReviewBodies(
        ReviewBodyDto reviewBodies,
        AddUpdateReviewBodyModel addUpdateReviewBodyModel)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<ReviewBodyDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBodies
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.CreateReviewBody(It.IsAny<ReviewBodyDto>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = Sut.CreateReviewBody(addUpdateReviewBodyModel);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<AddUpdateReviewBodyModel>();
        model.ShouldBeEquivalentTo(addUpdateReviewBodyModel);
    }
}