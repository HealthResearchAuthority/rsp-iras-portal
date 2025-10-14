﻿using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.SponsorOrganisationsControllerTests;

public class IndexTests : TestServiceBase<SponsorOrganisationsController>
{
    private readonly DefaultHttpContext _http;

    public IndexTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

    [Theory]
    [AutoData]
    public async Task ViewSponsorOrganisations_ShouldReturnViewWithOrderedSponsorOrganisations(
    AllSponsorOrganisationsResponse sponsorOrganisations)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = sponsorOrganisations
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetAllSponsorOrganisations(It.IsAny<SponsorOrganisationSearchRequest>(), 1, 20,
                nameof(SponsorOrganisationDto.SponsorOrganisationName), SortDirections.Ascending))
            .ReturnsAsync(serviceResponse);

        var sponsorOrganisationSearchModel = new SponsorOrganisationSearchModel
        {
            SearchQuery = null,
            Country = [],
            Status = null
        };

        // Persist the search model in Session (controller now reads from session)
        _http.Session.SetString(SessionKeys.SponsorOrganisationsSearch,
            JsonSerializer.Serialize(sponsorOrganisationSearchModel));

        // Act
        var result = await Sut.Index(1, 20, nameof(SponsorOrganisationDto.SponsorOrganisationName),
            SortDirections.Ascending,
            new SponsorOrganisationSearchViewModel
            {
                Search = new SponsorOrganisationSearchModel
                {
                    SearchQuery = null,
                    Country = [],
                    Status = null
                }
            });

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<SponsorOrganisationSearchViewModel>();

        // Verify
        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.GetAllSponsorOrganisations(It.IsAny<SponsorOrganisationSearchRequest>(), 1, 20,
                    nameof(SponsorOrganisationDto.SponsorOrganisationName), SortDirections.Ascending),
                Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task ViewSponsorOrganisations_ShouldReturnViewWithOrderedSponsorOrganisations_ByCountriesDesc(
AllSponsorOrganisationsResponse sponsorOrganisations)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = sponsorOrganisations
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetAllSponsorOrganisations(It.IsAny<SponsorOrganisationSearchRequest>(), 1, 20,
                nameof(SponsorOrganisationDto.Countries), SortDirections.Descending))
            .ReturnsAsync(serviceResponse);

        var sponsorOrganisationSearchModel = new SponsorOrganisationSearchModel
        {
            SearchQuery = null,
            Country = [],
            Status = null
        };

        // Persist the search model in Session (controller now reads from session)
        _http.Session.SetString(SessionKeys.SponsorOrganisationsSearch,
            JsonSerializer.Serialize(sponsorOrganisationSearchModel));

        // Act
        var result = await Sut.Index(1, 20, nameof(SponsorOrganisationDto.Countries),
            SortDirections.Descending,
            new SponsorOrganisationSearchViewModel
            {
                Search = new SponsorOrganisationSearchModel
                {
                    SearchQuery = null,
                    Country = [],
                    Status = null
                }
            });

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<SponsorOrganisationSearchViewModel>();

        // Verify
        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.GetAllSponsorOrganisations(It.IsAny<SponsorOrganisationSearchRequest>(), 1, 20,
                    nameof(SponsorOrganisationDto.Countries), SortDirections.Descending),
                Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task ViewSponsorOrganisations_ShouldReturnViewWithOrderedSponsorOrganisations_ByCountriesAsc(
    AllSponsorOrganisationsResponse sponsorOrganisations)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = sponsorOrganisations
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetAllSponsorOrganisations(It.IsAny<SponsorOrganisationSearchRequest>(), 1, 20,
                nameof(SponsorOrganisationDto.Countries), SortDirections.Descending))
            .ReturnsAsync(serviceResponse);

        var sponsorOrganisationSearchModel = new SponsorOrganisationSearchModel
        {
            SearchQuery = null,
            Country = [],
            Status = null
        };

        // Persist the search model in Session (controller now reads from session)
        _http.Session.SetString(SessionKeys.SponsorOrganisationsSearch,
            JsonSerializer.Serialize(sponsorOrganisationSearchModel));

        // Act
        var result = await Sut.Index(1, 20, nameof(SponsorOrganisationDto.Countries),
            SortDirections.Descending,
            new SponsorOrganisationSearchViewModel
            {
                Search = new SponsorOrganisationSearchModel
                {
                    SearchQuery = null,
                    Country = [],
                    Status = null
                }
            });

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<SponsorOrganisationSearchViewModel>();

        // Verify
        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.GetAllSponsorOrganisations(It.IsAny<SponsorOrganisationSearchRequest>(), 1, 20,
                    nameof(SponsorOrganisationDto.Countries), SortDirections.Descending),
                Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task ViewSponsorOrganisations_ShouldReturnViewWithOrderedSponsorOrganisations_ByStatusDesc(
AllSponsorOrganisationsResponse sponsorOrganisations)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = sponsorOrganisations
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetAllSponsorOrganisations(It.IsAny<SponsorOrganisationSearchRequest>(), 1, 20,
                nameof(SponsorOrganisationDto.IsActive), SortDirections.Descending))
            .ReturnsAsync(serviceResponse);

        var sponsorOrganisationSearchModel = new SponsorOrganisationSearchModel
        {
            SearchQuery = null,
            Country = [],
            Status = null
        };

        // Persist the search model in Session (controller now reads from session)
        _http.Session.SetString(SessionKeys.SponsorOrganisationsSearch,
            JsonSerializer.Serialize(sponsorOrganisationSearchModel));

        // Act
        var result = await Sut.Index(1, 20, nameof(SponsorOrganisationDto.IsActive),
            SortDirections.Descending,
            new SponsorOrganisationSearchViewModel
            {
                Search = new SponsorOrganisationSearchModel
                {
                    SearchQuery = null,
                    Country = [],
                    Status = null
                }
            });

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<SponsorOrganisationSearchViewModel>();

        // Verify
        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.GetAllSponsorOrganisations(It.IsAny<SponsorOrganisationSearchRequest>(), 1, 20,
                    nameof(SponsorOrganisationDto.IsActive), SortDirections.Descending),
                Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task ViewSponsorOrganisations_ShouldReturnViewWithOrderedSponsorOrganisations_ByStatusAsc(
AllSponsorOrganisationsResponse sponsorOrganisations)
    {
        // Arrange
        var serviceResponse = new ServiceResponse<AllSponsorOrganisationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = sponsorOrganisations
        };

        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetAllSponsorOrganisations(It.IsAny<SponsorOrganisationSearchRequest>(), 1, 20,
                nameof(SponsorOrganisationDto.IsActive), SortDirections.Ascending))
            .ReturnsAsync(serviceResponse);

        var sponsorOrganisationSearchModel = new SponsorOrganisationSearchModel
        {
            SearchQuery = null,
            Country = [],
            Status = null
        };

        // Persist the search model in Session (controller now reads from session)
        _http.Session.SetString(SessionKeys.SponsorOrganisationsSearch,
            JsonSerializer.Serialize(sponsorOrganisationSearchModel));

        // Act
        var result = await Sut.Index(1, 20, nameof(SponsorOrganisationDto.IsActive),
            SortDirections.Ascending,
            new SponsorOrganisationSearchViewModel
            {
                Search = new SponsorOrganisationSearchModel
                {
                    SearchQuery = null,
                    Country = [],
                    Status = null
                }
            });

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        var model = viewResult.Model.ShouldBeAssignableTo<SponsorOrganisationSearchViewModel>();

        // Verify
        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.GetAllSponsorOrganisations(It.IsAny<SponsorOrganisationSearchRequest>(), 1, 20,
                    nameof(SponsorOrganisationDto.IsActive), SortDirections.Ascending),
                Times.Once);
    }


    [Fact]
    public async Task ViewSponsorOrganisations_ShouldReturnEmptyView_WhenServiceReturnsNullContent()
    {
        // Arrange
        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetAllSponsorOrganisations(It.IsAny<SponsorOrganisationSearchRequest>(), 1, 20,
                nameof(SponsorOrganisationDto.SponsorOrganisationName), SortDirections.Ascending))
            .ReturnsAsync(new ServiceResponse<AllSponsorOrganisationsResponse>
            {
                StatusCode = HttpStatusCode.OK,
                Content = null
            });

        // Optional: clear any persisted session search
        _http.Session.Remove(SessionKeys.SponsorOrganisationsSearch);

        // Act
        var result = await Sut.Index(1, 20, nameof(SponsorOrganisationDto.SponsorOrganisationName),
            SortDirections.Ascending,
            new SponsorOrganisationSearchViewModel
            {
                Search = new SponsorOrganisationSearchModel
                {
                    SearchQuery = null,
                    Country = [],
                    Status = null
                }
            });

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeAssignableTo<SponsorOrganisationSearchViewModel>();


        // Verify
        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.GetAllSponsorOrganisations(It.IsAny<SponsorOrganisationSearchRequest>(), 1, 20,
                    nameof(SponsorOrganisationDto.SponsorOrganisationName), SortDirections.Ascending),
                Times.Once);
    }

    [Fact]
    public async Task ViewSponsorOrganisations_ShouldReturnErrorView_WhenServiceFails()
    {
        // Arrange
        Mocker.GetMock<ISponsorOrganisationService>()
            .Setup(s => s.GetAllSponsorOrganisations(It.IsAny<SponsorOrganisationSearchRequest>(), 1, 20,
                nameof(SponsorOrganisationDto.SponsorOrganisationName), SortDirections.Ascending))
            .ReturnsAsync(new ServiceResponse<AllSponsorOrganisationsResponse>
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var result = await Sut.Index(1, 20, nameof(SponsorOrganisationDto.SponsorOrganisationName),
            SortDirections.Ascending,
            new SponsorOrganisationSearchViewModel
            {
                Search = new SponsorOrganisationSearchModel
                {
                    SearchQuery = null,
                    Country = [],
                    Status = null
                }
            });

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeAssignableTo<SponsorOrganisationSearchViewModel>();

        // Verify
        Mocker.GetMock<ISponsorOrganisationService>()
            .Verify(s => s.GetAllSponsorOrganisations(It.IsAny<SponsorOrganisationSearchRequest>(), 1, 20,
                    nameof(SponsorOrganisationDto.SponsorOrganisationName), SortDirections.Ascending),
                Times.Once);
    }
}