using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.MyTasklistControllerTests;

public class RemoveFiltersTests : TestServiceBase<MyTasklistController>
{
    private readonly DefaultHttpContext _http;

    public RemoveFiltersTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };

        // Default validator OK unless a test overrides it
        SetupValidValidator();
    }

    [Fact]
    public async Task RemoveFilter_ProjectTitle_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel { ShortProjectTitle = "Cancer Study" };
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("shortprojecttitle");

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Index");

        var updated = GetSessionModel();
        updated.ShortProjectTitle.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_FromDate_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel { FromDay = "01", FromMonth = "01", FromYear = "2023" };
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("datesubmitted-from");

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Index");

        var updated = GetSessionModel();
        updated.FromDay.ShouldBeNull();
        updated.FromMonth.ShouldBeNull();
        updated.FromYear.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_ToDate_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel { ToDay = "31", ToMonth = "12", ToYear = "2023" };
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("datesubmitted-to");

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Index");

        var updated = GetSessionModel();
        updated.ToDay.ShouldBeNull();
        updated.ToMonth.ShouldBeNull();
        updated.ToYear.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_FromAndToDate_ShouldBeCleared_AndRedirect()
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
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("datesubmitted");

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Index");

        var updated = GetSessionModel();
        updated.FromDay.ShouldBeNull();
        updated.FromMonth.ShouldBeNull();
        updated.FromYear.ShouldBeNull();
        updated.ToDay.ShouldBeNull();
        updated.ToMonth.ShouldBeNull();
        updated.ToYear.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_DaysSinceSubmissionFrom_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel { FromDaysSinceSubmission = "01" };
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("dayssincesubmission-from");

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Index");

        var updated = GetSessionModel();
        updated.FromDaysSinceSubmission.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_DaysSinceSubmissionTo_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel { ToDaysSinceSubmission = "10" };
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("dayssincesubmission-to");

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Index");

        var updated = GetSessionModel();
        updated.ToDaysSinceSubmission.ShouldBeNull();
    }

    // ----------------------
    // Helpers
    // ----------------------

    private void SetSessionModel(ApprovalsSearchModel model)
    {
        _http.Session.SetString(SessionKeys.MyTasklist, JsonSerializer.Serialize(model));
    }

    private ApprovalsSearchModel GetSessionModel()
    {
        var json = _http.Session.GetString(SessionKeys.MyTasklist);
        json.ShouldNotBeNullOrWhiteSpace();
        return JsonSerializer.Deserialize<ApprovalsSearchModel>(json!)!;
    }

    private void SetupValidValidator()
    {
        var mockValidator = Mocker.GetMock<IValidator<ApprovalsSearchModel>>();
        mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ApprovalsSearchModel>(), default))
            .ReturnsAsync(new ValidationResult()); // Valid
    }
}