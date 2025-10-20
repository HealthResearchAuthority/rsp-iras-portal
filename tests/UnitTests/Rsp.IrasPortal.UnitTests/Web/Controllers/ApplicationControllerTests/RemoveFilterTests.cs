using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApplicationControllerTests;

public class RemoveFiltersTests : TestServiceBase<ApplicationController>
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
    public async Task RemoveFilter_StatusWithoutValue_ShouldClearAll_AndRedirect()
    {
        var model = new ApplicationSearchModel { Status = ["In draft", "Submitted"] };
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("status", null);

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe(nameof(Sut.Welcome));

        var updated = GetSessionModel();
        updated.Status.ShouldBeEmpty();
    }

    [Fact]
    public async Task RemoveFilter_FromDate_ShouldBeCleared_AndRedirect()
    {
        var model = new ApplicationSearchModel { FromDay = "01", FromMonth = "01", FromYear = "2023" };
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("datecreated-from", null);

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe(nameof(Sut.Welcome));

        var updated = GetSessionModel();
        updated.FromDay.ShouldBeNull();
        updated.FromMonth.ShouldBeNull();
        updated.FromYear.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_ToDate_ShouldBeCleared_AndRedirect()
    {
        var model = new ApplicationSearchModel { ToDay = "31", ToMonth = "12", ToYear = "2023" };
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("datecreated-to", null);

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe(nameof(Sut.Welcome));

        var updated = GetSessionModel();
        updated.ToDay.ShouldBeNull();
        updated.ToMonth.ShouldBeNull();
        updated.ToYear.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_FromAndToDate_ShouldBeCleared_AndRedirect()
    {
        var model = new ApplicationSearchModel
        {
            FromDay = "01",
            FromMonth = "01",
            FromYear = "2023",
            ToDay = "31",
            ToMonth = "12",
            ToYear = "2026"
        };
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("datecreated", null);

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe(nameof(Sut.Welcome));

        var updated = GetSessionModel();
        updated.FromDay.ShouldBeNull();
        updated.FromMonth.ShouldBeNull();
        updated.FromYear.ShouldBeNull();
        updated.ToDay.ShouldBeNull();
        updated.ToMonth.ShouldBeNull();
        updated.ToYear.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_SpecificStatus_ShouldBeRemoved_AndRedirect()
    {
        var model = new ApplicationSearchModel
        {
            Status = ["In draft", "Submitted", "Approved"]
        };
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("status", "Submitted");

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe(nameof(Sut.Welcome));

        var updated = GetSessionModel();
        updated.Status.ShouldContain("In draft");
        updated.Status.ShouldContain("Approved");
        updated.Status.ShouldNotContain("Submitted");
    }

    // ----------------------
    // Helpers
    // ----------------------

    private void SetSessionModel(ApplicationSearchModel model)
    {
        _http.Session.SetString(SessionKeys.ProjectRecordSearch, JsonSerializer.Serialize(model));
    }

    private ApplicationSearchModel GetSessionModel()
    {
        var json = _http.Session.GetString(SessionKeys.ProjectRecordSearch);
        json.ShouldNotBeNullOrWhiteSpace();
        return JsonSerializer.Deserialize<ApplicationSearchModel>(json!)!;
    }

    private void SetupValidValidator()
    {
        var mockValidator = Mocker.GetMock<IValidator<ApplicationSearchModel>>();
        mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ApplicationSearchModel>(), default))
            .ReturnsAsync(new ValidationResult()); // Valid
    }
}