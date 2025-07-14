using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.DTOs.Requests;
using Rsp.IrasPortal.Application.DTOs.Responses;
using Rsp.IrasPortal.Application.Responses;
using Rsp.IrasPortal.Application.Services;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApprovalsControllerTests;

public class SearchTests : TestServiceBase<ApprovalsController>
{
    private const string TempDataKey = "ApprovalsSearchModel";

    private readonly Mock<IApplicationsService> _applicationsService;
    private readonly Mock<IRtsService> _rtsService;
    private readonly Mock<IValidator<ApprovalsSearchModel>> _validator;

    public SearchTests()
    {
        _applicationsService = Mocker.GetMock<IApplicationsService>();
        _rtsService = Mocker.GetMock<IRtsService>();
        _validator = Mocker.GetMock<IValidator<ApprovalsSearchModel>>();

        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        Sut.TempData = tempData;
    }

    [Fact]
    public async Task Search_ShouldReturnDefaultView_WhenNoTempDataExists()
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
        Sut.TempData[TempDataKey] = JsonSerializer.Serialize(searchModel);

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
        var searchModel = new ApprovalsSearchModel
        {
            ChiefInvestigatorName = "Dr. Test"
        };

        Sut.TempData[TempDataKey] = JsonSerializer.Serialize(searchModel);

        var serviceResponse = new ServiceResponse<GetModificationsResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = mockResponse
        };

        _applicationsService
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

    [Fact]
    public async Task Search_ShouldThrow_WhenTempDataIsInvalidJson()
    {
        // Arrange
        Sut.TempData[TempDataKey] = "Not a JSON string";

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() => Sut.Search());
    }
}