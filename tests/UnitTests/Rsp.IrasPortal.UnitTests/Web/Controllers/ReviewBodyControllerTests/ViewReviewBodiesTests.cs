using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class ViewReviewBodiesTests : TestServiceBase<ReviewBodyController>
{
    [Theory, AutoData]
    public async Task ViewReviewBodies_ShouldReturnViewWithOrderedReviewBodies(AllReviewBodiesResponse reviewBodies)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<AllReviewBodiesResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBodies
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetAllReviewBodies(1, 20, null))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.ViewReviewBodies();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<(IEnumerable<ReviewBodyDto>, PaginationViewModel)>();
        model.Item1.ShouldBeEquivalentTo(reviewBodies.ReviewBodies);

        // Verify
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetAllReviewBodies(1, 20, null), Times.Once);
    }

    [Fact]
    public async Task ViewReviewBodies_ShouldReturnEmptyView_WhenServiceReturnsNull()
    {
        // Arrange
        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetAllReviewBodies(1, 20, null))
            .ReturnsAsync(new ServiceResponse<AllReviewBodiesResponse>
            { StatusCode = HttpStatusCode.OK, Content = null });

        // Act
        var result = await Sut.ViewReviewBodies();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeAssignableTo<(IEnumerable<ReviewBodyDto>, PaginationViewModel)>();

        // Verify
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetAllReviewBodies(1, 20, null), Times.Once);
    }

    [Fact]
    public async Task ViewReviewBodies_ShouldReturnErrorView_WhenServiceFails()
    {
        // Arrange
        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetAllReviewBodies(1, 20, null))
            .ReturnsAsync(new ServiceResponse<AllReviewBodiesResponse>
            { StatusCode = HttpStatusCode.InternalServerError });

        // Act
        var result = await Sut.ViewReviewBodies();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeAssignableTo<(IEnumerable<ReviewBodyDto>, PaginationViewModel)>();

        // Verify
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetAllReviewBodies(1, 20, null), Times.Once);
    }

    [Theory, AutoData]
    public async Task ViewReviewBody_ShouldReturnViewWithReviewBody(Guid id, ReviewBodyDto reviewBodyDto)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<ReviewBodyDto>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBodyDto
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(id))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.ViewReviewBody(id);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<AddUpdateReviewBodyModel>();

        // Verify that the service method was called once
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetReviewBodyById(id), Times.Once);
    }
}