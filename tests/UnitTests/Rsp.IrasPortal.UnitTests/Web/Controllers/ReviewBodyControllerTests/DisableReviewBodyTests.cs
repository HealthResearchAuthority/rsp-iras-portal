using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class DisableReviewBodyTests : TestServiceBase<ReviewBodyController>

{
    [Theory, AutoData]
    public async Task DisableReviewBody_WithValidModel_ShouldReturnDisableReviewBodyView(ReviewBodyDto reviewBody)
    {
        //Act
        // Arrange
        var serviceResponse = new ServiceResponse<ReviewBodyDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBody
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(It.IsAny<Guid>()))
            .ReturnsAsync(serviceResponse);

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.DisableReviewBody(It.IsAny<Guid>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.DisableReviewBody(reviewBody.Id);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeAssignableTo<AddUpdateReviewBodyModel>();
    }

    [Theory, AutoData]
    public async Task DisableReviewBody_WithValidModel_ShouldReturnManageBodiesView(ReviewBodyDto reviewBody)
    {
        //Act
        // Arrange
        var serviceResponse = new ServiceResponse<ReviewBodyDto>
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
        var result = await Sut.DisableReviewBody(reviewBody.Id);

        // Assert
        result.ShouldBeOfType<RedirectToActionResult>();
    }
}