using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApprovalsControllerTests;

public class SearchTests : TestServiceBase<ApprovalsController>
{
    private readonly Mock<IProjectModificationsService> _projectModificationsService;
    private readonly Mock<IRtsService> _rtsService;
    private readonly Mock<IValidator<ApprovalsSearchModel>> _validator;

    private readonly DefaultHttpContext _http;

    public SearchTests()
    {
        _projectModificationsService = Mocker.GetMock<IProjectModificationsService>();
        _rtsService = Mocker.GetMock<IRtsService>();
        _validator = Mocker.GetMock<IValidator<ApprovalsSearchModel>>();

        _http = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = _http
        };
    }

    [Fact]
    public async Task Search_ShouldReturnDefaultView_WhenNoSessionExists()
    {
        // Act
        var result = await Sut.Search();

        // Assert
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ApprovalsSearchViewModel>(view.Model);

        Assert.Empty(model.Modifications);
        Assert.False(model.EmptySearchPerformed);
    }

    [Fact]
    public async Task Search_ShouldReturnEmptySearchPerformed_WhenFiltersAreEmpty()
    {
        // Arrange
        var searchModel = new ApprovalsSearchModel();
        _http.Session.SetString(SessionKeys.ApprovalsSearch, JsonSerializer.Serialize(searchModel)); // <-- fixed

        // Act
        var result = await Sut.Search();

        // Assert
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ApprovalsSearchViewModel>(view.Model);

        Assert.True(model.EmptySearchPerformed);
        Assert.Empty(model.Modifications);
    }

    [Theory, AutoData]
    public async Task Search_ShouldReturnModifications_WhenSearchModelIsValid(GetModificationsResponse mockResponse)
    {
        // Arrange
        var searchModel = new ApprovalsSearchModel { ChiefInvestigatorName = "Dr. Test" };
        _http.Session.SetString(SessionKeys.ApprovalsSearch, JsonSerializer.Serialize(searchModel));

        var serviceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = mockResponse
        };

        _projectModificationsService
            .Setup(s => s.GetModifications(It.IsAny<ModificationSearchRequest>(), 1, 20, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.Search();

        // Assert
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ApprovalsSearchViewModel>(view.Model);
        Assert.NotEmpty(model.Modifications);
        Assert.Equal(mockResponse.Modifications.Count(), model.Modifications.Count());
    }

    [Theory, AutoData]
    public async Task Search_ShouldReturnModifications_WhenShortProjectTitleIsSet(GetModificationsResponse mockResponse)
    {
        // Arrange
        var searchModel = new ApprovalsSearchModel { ShortProjectTitle = "Cancer Research" };
        _http.Session.SetString(SessionKeys.ApprovalsSearch, JsonSerializer.Serialize(searchModel));

        var serviceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = mockResponse
        };

        _projectModificationsService
            .Setup(s => s.GetModifications(It.IsAny<ModificationSearchRequest>(), 1, 20, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.Search();

        // Assert
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ApprovalsSearchViewModel>(view.Model);
        Assert.Equal(mockResponse.Modifications.Count(), model.Modifications.Count());
    }

    [Theory, AutoData]
    public async Task Search_ShouldReturnModifications_WhenSponsorOrganisationIsSet(GetModificationsResponse mockResponse)
    {
        // Arrange
        var searchModel = new ApprovalsSearchModel { SponsorOrganisation = "University College London" };
        _http.Session.SetString(SessionKeys.ApprovalsSearch, JsonSerializer.Serialize(searchModel));

        var serviceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = mockResponse
        };

        _projectModificationsService
            .Setup(s => s.GetModifications(It.IsAny<ModificationSearchRequest>(), 1, 20, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.Search();

        // Assert
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ApprovalsSearchViewModel>(view.Model);
        Assert.Equal(mockResponse.Modifications.Count(), model.Modifications.Count());
    }

    [Theory, AutoData]
    public async Task Search_ShouldReturnModifications_WhenDateRangeIsSet(GetModificationsResponse mockResponse)
    {
        // Arrange
        var searchModel = new ApprovalsSearchModel
        {
            FromDay = "01",
            FromMonth = "01",
            FromYear = "2024",
            ToDay = "31",
            ToMonth = "12",
            ToYear = "2024"
        };
        _http.Session.SetString(SessionKeys.ApprovalsSearch, JsonSerializer.Serialize(searchModel));

        var serviceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = mockResponse
        };

        _projectModificationsService
            .Setup(s => s.GetModifications(It.IsAny<ModificationSearchRequest>(), 1, 20, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.Search();

        // Assert
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ApprovalsSearchViewModel>(view.Model);
        Assert.Equal(mockResponse.Modifications.Count(), model.Modifications.Count());
    }

    [Theory, AutoData]
    public async Task Search_ShouldReturnModifications_WhenCountryFilterIsSet(GetModificationsResponse mockResponse)
    {
        // Arrange
        var searchModel = new ApprovalsSearchModel
        {
            LeadNation = new List<string> { "England", "Wales" }
        };
        _http.Session.SetString(SessionKeys.ApprovalsSearch, JsonSerializer.Serialize(searchModel));

        var serviceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = mockResponse
        };

        _projectModificationsService
            .Setup(s => s.GetModifications(It.IsAny<ModificationSearchRequest>(), 1, 20, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.Search();

        // Assert
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ApprovalsSearchViewModel>(view.Model);
        Assert.Equal(mockResponse.Modifications.Count(), model.Modifications.Count());
    }

    [Theory, AutoData]
    public async Task Search_ShouldReturnModifications_WhenModificationTypeIsSet(GetModificationsResponse mockResponse)
    {
        // Arrange
        var searchModel = new ApprovalsSearchModel
        {
            ModificationTypes = new List<string> { "Substantial", "Non-substantial" }
        };
        _http.Session.SetString(SessionKeys.ApprovalsSearch, JsonSerializer.Serialize(searchModel));

        var serviceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = mockResponse
        };

        _projectModificationsService
            .Setup(s => s.GetModifications(It.IsAny<ModificationSearchRequest>(), 1, 20, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await Sut.Search();

        // Assert
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ApprovalsSearchViewModel>(view.Model);
        Assert.Equal(mockResponse.Modifications.Count(), model.Modifications.Count());
    }

    [Fact]
    public async Task Search_ShouldThrow_WhenSessionJsonIsInvalid()
    {
        // Arrange
        _http.Session.SetString(SessionKeys.ApprovalsSearch, "Not a JSON string");

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() => Sut.Search());
    }
}