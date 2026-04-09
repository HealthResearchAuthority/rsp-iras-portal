using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Web.Controllers.ProjectOverview;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.ProjectOverviewControllerTests;

public class RemoveFiltersTests : TestServiceBase<ProjectOverviewController>
{
    private readonly Mock<ITempDataDictionary> MockTempData;
    private readonly DefaultHttpContext _http;

    public RemoveFiltersTests()
    {
        MockTempData = new Mock<ITempDataDictionary>();
        _http = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = _http
        };

        SetupValidValidator(new ValidationResult());
    }

    [Fact]
    public async Task RemoveFilter_datesubmitted_From_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel { FromDay = "01", FromMonth = "01", FromYear = "2023" };
        _ = MockTempData.Setup(a => a.Peek(It.IsAny<string>())).Returns("rec-1");
        Sut.TempData = MockTempData.Object;
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("datesubmitted-from");

        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pov:postapproval");

        var updated = GetSessionModel();
        updated.ModificationType.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_datesubmitted_To_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel { FromDay = "01", FromMonth = "01", FromYear = "2023" };
        _ = MockTempData.Setup(a => a.Peek(It.IsAny<string>())).Returns("rec-1");
        Sut.TempData = MockTempData.Object;
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("datesubmitted-to");

        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pov:postapproval");

        var updated = GetSessionModel();
        updated.ModificationType.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_datesubmitted_To_And_From_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel
        {
            FromDay = "01",
            FromMonth = "01",
            FromYear = "2023",
            ToDay = "31",
            ToMonth = "12",
            ToYear = "2026"
        };
        _ = MockTempData.Setup(a => a.Peek(It.IsAny<string>())).Returns("rec-1");
        Sut.TempData = MockTempData.Object;
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("datesubmitted");

        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pov:postapproval");

        var updated = GetSessionModel();
        updated.ModificationType.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_ModificationType_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel { ModificationType = "Minor modification" };
        _ = MockTempData.Setup(a => a.Peek(It.IsAny<string>())).Returns("rec-1");
        Sut.TempData = MockTempData.Object;
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("modificationtype");

        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pov:postapproval");

        var updated = GetSessionModel();
        updated.ModificationType.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_Status_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel { ModificationType = "In draft" };
        _ = MockTempData.Setup(a => a.Peek(It.IsAny<string>())).Returns("rec-1");
        Sut.TempData = MockTempData.Object;
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("status");

        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pov:postapproval");

        var updated = GetSessionModel();
        updated.ModificationType.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_ReviewType_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel { ReviewType = "No review required" };
        _ = MockTempData.Setup(a => a.Peek(It.IsAny<string>())).Returns("rec-1");
        Sut.TempData = MockTempData.Object;
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("reviewtype");

        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pov:postapproval");

        var updated = GetSessionModel();
        updated.ModificationType.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_CategoryType_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel { Category = "A" };
        _ = MockTempData.Setup(a => a.Peek(It.IsAny<string>())).Returns("rec-1");
        Sut.TempData = MockTempData.Object;
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("category");

        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pov:postapproval");

        var updated = GetSessionModel();
        updated.ModificationType.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_WhenValidationFails_ShouldStoreModelStateInTempData_AndRedirect()
    {
        // Arrange
        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure(
                nameof(ApprovalsSearchModel.FromDay),
                "From date is invalid")
        });

        SetupValidValidator(validationResult);

        var httpContext = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };

        var tempData = new TempDataDictionary(
            httpContext,
            Mock.Of<ITempDataProvider>());

        tempData[TempDataKeys.ProjectRecordId] = "rec-1";

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        Sut.TempData = tempData;

        var model = new ApprovalsSearchModel
        {
            FromDay = "99", // invalid on purpose
            FromMonth = "01",
            FromYear = "2023"
        };

        SetSessionModel(model);

        // Act
        var result = await Sut.RemoveFilter("datesubmitted-from");

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("pov:postapproval");

        tempData.ShouldContainKey(TempDataKeys.ModelState);

        var serializedModelState = tempData[TempDataKeys.ModelState]
            .ShouldBeOfType<string>();

        serializedModelState.ShouldContain(
            nameof(ApprovalsSearchModel.FromDay));
    }

    private void SetSessionModel(ApprovalsSearchModel model)
    {
        _http.Session.SetString(SessionKeys.PostApprovalsSearch, JsonSerializer.Serialize(model));
    }

    private ApprovalsSearchModel GetSessionModel()
    {
        var json = _http.Session.GetString(SessionKeys.PostApprovalsSearch);
        json.ShouldNotBeNullOrWhiteSpace();
        return JsonSerializer.Deserialize<ApprovalsSearchModel>(json!)!;
    }

    private void SetupValidValidator(ValidationResult result)
    {
        var mockValidator = Mocker.GetMock<IValidator<ApprovalsSearchModel>>();
        mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ApprovalsSearchModel>(), default))
            .ReturnsAsync(result);
    }
}