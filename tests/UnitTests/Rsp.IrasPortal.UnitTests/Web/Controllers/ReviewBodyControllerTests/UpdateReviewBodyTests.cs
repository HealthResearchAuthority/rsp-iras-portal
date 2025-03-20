using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class UpdateReviewBodyTests : TestServiceBase<ReviewBodyController>
{
    [Theory,AutoData]
    public async Task UpdateReviewBody_ShouldReturnViewWithOrderedReviewBodies(
        List<ReviewBodyDto> reviewBodies)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<IEnumerable<ReviewBodyDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBodies
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(It.IsAny<Guid>()))
            .ReturnsAsync(serviceResponse);

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.UpdateReviewBody(It.IsAny<ReviewBodyDto>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.UpdateReviewBody(reviewBodies[0].Id);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeAssignableTo<AddUpdateReviewBodyModel>();
    }
}