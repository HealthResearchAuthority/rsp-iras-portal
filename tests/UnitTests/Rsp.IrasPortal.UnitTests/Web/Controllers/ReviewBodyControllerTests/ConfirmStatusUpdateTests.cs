using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class ConfirmStatusUpdateTests : TestServiceBase<ReviewBodyController>

{
    [Theory, AutoData]
    public async Task ConfirmStatusUpdate_DisableReviewBody(List<ReviewBodyDto> reviewBodies,
        AddUpdateReviewBodyModel addUpdateReviewBodyModel)
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
            .Setup(s => s.DisableReviewBody(It.IsAny<Guid>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.ConfirmStatusUpdate(addUpdateReviewBodyModel);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeAssignableTo<AddUpdateReviewBodyModel>();
    }

    [Theory, AutoData]
    public async Task ConfirmStatusUpdate_EnableReviewBody(List<ReviewBodyDto> reviewBodies,
        AddUpdateReviewBodyModel addUpdateReviewBodyModel)
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
            .Setup(s => s.DisableReviewBody(It.IsAny<Guid>()))
            .ReturnsAsync(serviceResponse);

        addUpdateReviewBodyModel.IsActive = true;

        // Act
        var result = await Sut.ConfirmStatusUpdate(addUpdateReviewBodyModel);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeAssignableTo<AddUpdateReviewBodyModel>();
    }
}