using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class SubmitReviewBodyTests : TestServiceBase<ReviewBodyController>
{
    [Theory]
    [AutoData]
    public async Task SubmitReviewBody_WithValidData_ShouldReturnSuccessView(
        AddUpdateReviewBodyModel model)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<IEnumerable<ReviewBodyDto>>
        {
            StatusCode = HttpStatusCode.OK
        };

        model.Id = Guid.Empty;

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.CreateReviewBody(It.IsAny<ReviewBodyDto>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.SubmitReviewBody(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("SuccessMessage");
        viewResult.Model.ShouldBeEquivalentTo(model);

        // Verify the service method was called once
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.CreateReviewBody(It.IsAny<ReviewBodyDto>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task SubmitReviewBody_WithInvalidData_ShouldReturnSuccessView(
        AddUpdateReviewBodyModel model)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<IEnumerable<ReviewBodyDto>>
        {
            StatusCode = HttpStatusCode.InternalServerError
        };

        model.Id = Guid.Empty;

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.CreateReviewBody(It.IsAny<ReviewBodyDto>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.SubmitReviewBody(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("SuccessMessage");
        viewResult.Model.ShouldBeEquivalentTo(model);

        // Verify the service method was called once
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.CreateReviewBody(It.IsAny<ReviewBodyDto>()), Times.Once);
    }
}