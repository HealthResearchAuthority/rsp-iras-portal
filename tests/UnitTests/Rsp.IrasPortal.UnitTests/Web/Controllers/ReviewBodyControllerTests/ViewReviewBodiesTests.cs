using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Areas.Admin.Models;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class ViewReviewBodiesTests : TestServiceBase<ReviewBodyController>
{
    private readonly DefaultHttpContext _http;

    public ViewReviewBodiesTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

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
            .Setup(s => s.GetAllReviewBodies(It.IsAny<ReviewBodySearchRequest>(), 1, 20,
                                             nameof(ReviewBodyDto.RegulatoryBodyName), SortDirections.Ascending))
            .ReturnsAsync(serviceResponse);

        var reviewBodySearchModel = new ReviewBodySearchModel
        {
            SearchQuery = null,
            Country = [],
            Status = null
        };

        // Persist the search model in Session (controller now reads from session)
        _http.Session.SetString(SessionKeys.ReviewBodiesSearch, JsonSerializer.Serialize(reviewBodySearchModel));

        // Act
        var result = await Sut.ViewReviewBodies(1, 20, nameof(ReviewBodyDto.RegulatoryBodyName),
                                                SortDirections.Ascending,
                                                new ReviewBodySearchViewModel
                                                {
                                                    Search = new ReviewBodySearchModel
                                                    {
                                                        SearchQuery = null,
                                                        Country = [],
                                                        Status = null
                                                    }
                                                });

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<ReviewBodySearchViewModel>();
        model.ReviewBodies.ShouldBeEquivalentTo(reviewBodies.ReviewBodies);

        // Verify
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetAllReviewBodies(It.IsAny<ReviewBodySearchRequest>(), 1, 20,
                                              nameof(ReviewBodyDto.RegulatoryBodyName), SortDirections.Ascending),
                    Times.Once);
    }

    [Fact]
    public async Task ViewReviewBodies_ShouldReturnEmptyView_WhenServiceReturnsNullContent()
    {
        // Arrange
        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetAllReviewBodies(It.IsAny<ReviewBodySearchRequest>(), 1, 20,
                                             nameof(ReviewBodyDto.RegulatoryBodyName), SortDirections.Ascending))
            .ReturnsAsync(new ServiceResponse<AllReviewBodiesResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = null
            });

        // Optional: clear any persisted session search
        _http.Session.Remove(SessionKeys.ReviewBodiesSearch);

        // Act
        var result = await Sut.ViewReviewBodies(1, 20, nameof(ReviewBodyDto.RegulatoryBodyName),
                                                SortDirections.Ascending,
                                                new ReviewBodySearchViewModel
                                                {
                                                    Search = new ReviewBodySearchModel
                                                    {
                                                        SearchQuery = null,
                                                        Country = [],
                                                        Status = null
                                                    }
                                                });

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<ReviewBodySearchViewModel>();
        model.ShouldNotBeNull();
        model.ReviewBodies.ShouldBeNull();
        model.Pagination.ShouldNotBeNull();
        model.Pagination.TotalCount.ShouldBe(0);

        // Verify
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetAllReviewBodies(It.IsAny<ReviewBodySearchRequest>(), 1, 20,
                                              nameof(ReviewBodyDto.RegulatoryBodyName), SortDirections.Ascending),
                    Times.Once);
    }

    [Fact]
    public async Task ViewReviewBodies_ShouldReturnErrorView_WhenServiceFails()
    {
        // Arrange
        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetAllReviewBodies(It.IsAny<ReviewBodySearchRequest>(), 1, 20,
                                             nameof(ReviewBodyDto.RegulatoryBodyName), SortDirections.Ascending))
            .ReturnsAsync(new ServiceResponse<AllReviewBodiesResponse>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var result = await Sut.ViewReviewBodies(1, 20, nameof(ReviewBodyDto.RegulatoryBodyName),
                                                SortDirections.Ascending,
                                                new ReviewBodySearchViewModel
                                                {
                                                    Search = new ReviewBodySearchModel
                                                    {
                                                        SearchQuery = null,
                                                        Country = [],
                                                        Status = null
                                                    }
                                                });

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeAssignableTo<ReviewBodySearchViewModel>();

        // Verify
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetAllReviewBodies(It.IsAny<ReviewBodySearchRequest>(), 1, 20,
                                              nameof(ReviewBodyDto.RegulatoryBodyName), SortDirections.Ascending),
                    Times.Once);
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

        // Verify
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetReviewBodyById(id), Times.Once);
    }
}