using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Application.DTOs;
using Rsp.Portal.Application.DTOs.Requests;
using Rsp.Portal.Application.DTOs.Responses;
using Rsp.Portal.Application.Responses;
using Rsp.Portal.Application.Services;
using Rsp.Portal.Web.Areas.Admin.Models;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.ReviewBodyControllerTests;

public class ApplyFiltersTests : TestServiceBase<ReviewBodyController>
{
    private readonly DefaultHttpContext _http;

    public ApplyFiltersTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

    [Theory, AutoData]
    public async Task ApplyFilters_ShouldReturnViewWithOrderedReviewBodies(AllReviewBodiesResponse reviewBodies)
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

        // Persist the search model in Session (GET path reads from session)
        var persisted = JsonSerializer.Serialize(reviewBodySearchModel);
        _http.Session.SetString(SessionKeys.ReviewBodiesSearch, persisted);

        // Simulate HTTP GET
        _http.Request.Method = HttpMethods.Get;

        // Act
        var result = await Sut.ApplyFilters(
            new ReviewBodySearchViewModel
            {
                // Controller should ignore this on GET and use session
                Search = new ReviewBodySearchModel { SearchQuery = "ignored", Country = ["England"], Status = false }
            },
            nameof(ReviewBodyDto.RegulatoryBodyName),
            SortDirections.Ascending,
            fromPagination: false);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<ReviewBodySearchViewModel>();
        model.ReviewBodies.ShouldBeEquivalentTo(reviewBodies.ReviewBodies);

        // Session should NOT be overwritten on GET
        _http.Session.GetString(SessionKeys.ReviewBodiesSearch).ShouldBe(persisted);

        // Verify
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetAllReviewBodies(It.IsAny<ReviewBodySearchRequest>(), 1, 20,
                    nameof(ReviewBodyDto.RegulatoryBodyName), SortDirections.Ascending),
                Times.Once);
    }

    [Fact]
    public async Task ApplyFilters_ShouldReturnEmptyView_WhenServiceReturnsNullContent_OnHttpGet()
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

        // No persisted search — simulate first-load GET
        _http.Session.Remove(SessionKeys.ReviewBodiesSearch);

        // Simulate HTTP GET
        _http.Request.Method = HttpMethods.Get;

        // Act
        var result = await Sut.ApplyFilters(
            new ReviewBodySearchViewModel
            {
                // Controller should ignore payload on GET if it uses session/empty defaults
                Search = new ReviewBodySearchModel { SearchQuery = null, Country = [], Status = null }
            },
            nameof(ReviewBodyDto.RegulatoryBodyName),
            SortDirections.Ascending);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<ReviewBodySearchViewModel>();
        model.ShouldNotBeNull();
        model.ReviewBodies.ShouldBeNull();
        model.Pagination.ShouldNotBeNull();
        model.Pagination.TotalCount.ShouldBe(0);

        // Session still not set by GET path
        _http.Session.GetString(SessionKeys.ReviewBodiesSearch).ShouldBeNull();

        // Verify
        Mocker.GetMock<IReviewBodyService>()
            .Verify(s => s.GetAllReviewBodies(It.IsAny<ReviewBodySearchRequest>(), 1, 20,
                    nameof(ReviewBodyDto.RegulatoryBodyName), SortDirections.Ascending),
                Times.Once);
    }

    [Fact]
    public async Task ApplyFilters_ShouldReturnErrorView_WhenServiceFails_OnHttpGet()
    {
        // Arrange
        Mocker.GetMock<IReviewBodyService>()
            .Setup(s => s.GetAllReviewBodies(It.IsAny<ReviewBodySearchRequest>(), 1, 20,
                    nameof(ReviewBodyDto.RegulatoryBodyName), SortDirections.Ascending))
            .ReturnsAsync(new ServiceResponse<AllReviewBodiesResponse>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Simulate HTTP GET
        _http.Request.Method = HttpMethods.Get;

        // Act
        var result = await Sut.ApplyFilters(
            new ReviewBodySearchViewModel
            {
                Search = new ReviewBodySearchModel { SearchQuery = null, Country = [], Status = null }
            },
            nameof(ReviewBodyDto.RegulatoryBodyName),
            SortDirections.Ascending);

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
