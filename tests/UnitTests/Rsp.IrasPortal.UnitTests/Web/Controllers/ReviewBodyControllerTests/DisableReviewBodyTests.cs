using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class DisableReviewBodyTests : TestServiceBase<ReviewBodyController>

{
    [Theory, AutoData]
    public async Task DisableReviewBody_WithValidModel_ShouldReturnDisableReviewBodyView(List<ReviewBodyDto> reviewBodies)
    {
        //Act
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
            .Setup(s => s.DisableReviewBody(It.IsAny<Guid>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.DisableReviewBody(reviewBodies[0].Id);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeAssignableTo<AddUpdateReviewBodyModel>();
    }

    [Theory, AutoData]
    public async Task DisableReviewBody_WithValidModel_ShouldReturnManageBodiesView(List<ReviewBodyDto> reviewBodies)
    {
        //Act
        // Arrange
        var serviceResponse = new ServiceResponse<IEnumerable<ReviewBodyDto>>
        {
            StatusCode = HttpStatusCode.OK,
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(It.IsAny<Guid>()))
            .ReturnsAsync(serviceResponse);

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.DisableReviewBody(It.IsAny<Guid>()))
            .ReturnsAsync(serviceResponse);


        // Act
        var result = await Sut.DisableReviewBody(reviewBodies[0].Id);

        // Assert
         result.ShouldBeOfType<RedirectToActionResult>();
    }
}