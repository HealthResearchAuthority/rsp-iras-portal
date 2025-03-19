using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class ViewReviewBodiesTests : TestServiceBase<ReviewBodyController>
{
    [Theory, AutoData]
    public async Task ViewReviewBodies_ShouldReturnViewWithOrderedReviewBodies(List<ReviewBodyDto> reviewBodies)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<IEnumerable<ReviewBodyDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = reviewBodies
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetAllReviewBodies())
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.ViewReviewBodies();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<IEnumerable<ReviewBodyDto>>();
        model.ShouldBeEquivalentTo(reviewBodies.OrderBy(rb => rb.OrganisationName));

        // Verify
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetAllReviewBodies(), Times.Once);
    }

    [Fact]
    public async Task ViewReviewBodies_ShouldReturnEmptyView_WhenServiceReturnsNull()
    {
        // Arrange
        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetAllReviewBodies())
            .ReturnsAsync(new ServiceResponse<IEnumerable<ReviewBodyDto>>
                { StatusCode = HttpStatusCode.OK, Content = null });

        // Act
        var result = await Sut.ViewReviewBodies();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<IEnumerable<ReviewBodyDto>>();
        model.ShouldBeNull();

        // Verify
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetAllReviewBodies(), Times.Once);
    }

    [Fact]
    public async Task ViewReviewBodies_ShouldReturnErrorView_WhenServiceFails()
    {
        // Arrange
        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetAllReviewBodies())
            .ReturnsAsync(new ServiceResponse<IEnumerable<ReviewBodyDto>>
                { StatusCode = HttpStatusCode.InternalServerError });

        // Act
        var result = await Sut.ViewReviewBodies();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<IEnumerable<ReviewBodyDto>>();
        model.ShouldBeNull();

        // Verify
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetAllReviewBodies(), Times.Once);
    }

    [Theory, AutoData]
    public async Task ViewReviewBody_ShouldReturnViewWithReviewBody(Guid id, ReviewBodyDto reviewBodyDto)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<IEnumerable<ReviewBodyDto>>
        {
            StatusCode = HttpStatusCode.OK,
            Content = new List<ReviewBodyDto> { reviewBodyDto }
        };

        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetReviewBodyById(id))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.ViewReviewBody(id);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<ReviewBodyDto>();
        model.ShouldBeEquivalentTo(reviewBodyDto);

        // Verify that the service method was called once
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetReviewBodyById(id), Times.Once);
    }
}