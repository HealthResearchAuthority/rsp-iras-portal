using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

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

    [Theory]
    [AutoData]
    public async Task CreateReviewBody_ShouldReturnViewWithOrderedReviewBodies(
        List<ReviewBodyDto> reviewBodies,
        AddUpdateReviewBodyModel addUpdateReviewBodyModel)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<IEnumerable<ReviewBodyDto>>
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