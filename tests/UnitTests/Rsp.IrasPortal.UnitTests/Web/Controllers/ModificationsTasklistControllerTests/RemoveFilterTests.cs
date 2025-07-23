using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ModificationsTasklistControllerTests;

public class RemoveFiltersTests : TestServiceBase<ModificationsTasklistController>
{
    [Fact]
    public async Task RemoveFilter_ProjectTitle_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel { ShortProjectTitle = "Cancer Study" };
        SetTempData(new Dictionary<string, object?>
        {
            { TempDataKeys.ApprovalsSearchModel, JsonSerializer.Serialize(model) }
        });
        SetupValidValidator();

        var result = await Sut.RemoveFilter("shortprojecttitle");

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Index");

        var updated =
            JsonSerializer.Deserialize<ApprovalsSearchModel>(
                Sut.TempData[TempDataKeys.ApprovalsSearchModel]!.ToString()!)!;
        updated.ShortProjectTitle.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_FromDate_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel { FromDay = "01", FromMonth = "01", FromYear = "2023" };
        SetTempData(new Dictionary<string, object?>
        {
            { TempDataKeys.ApprovalsSearchModel, JsonSerializer.Serialize(model) }
        });
        SetupValidValidator();

        var result = await Sut.RemoveFilter("datemodificationsubmitted-from");

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Index");

        var updated =
            JsonSerializer.Deserialize<ApprovalsSearchModel>(
                Sut.TempData[TempDataKeys.ApprovalsSearchModel]!.ToString()!)!;
        updated.FromDay.ShouldBeNull();
        updated.FromMonth.ShouldBeNull();
        updated.FromYear.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_ToDate_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel { ToDay = "31", ToMonth = "12", ToYear = "2023" };
        SetTempData(new Dictionary<string, object?>
        {
            { TempDataKeys.ApprovalsSearchModel, JsonSerializer.Serialize(model) }
        });
        SetupValidValidator();

        var result = await Sut.RemoveFilter("datemodificationsubmitted-to");

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Index");

        var updated =
            JsonSerializer.Deserialize<ApprovalsSearchModel>(
                Sut.TempData[TempDataKeys.ApprovalsSearchModel]!.ToString()!)!;
        updated.ToDay.ShouldBeNull();
        updated.ToMonth.ShouldBeNull();
        updated.ToYear.ShouldBeNull();
    }

    private void SetTempData(IDictionary<string, object?> values)
    {
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        foreach (var kvp in values)
        {
            tempData[kvp.Key] = kvp.Value;
        }

        Sut.TempData = tempData;
    }

    private void SetupValidValidator()
    {
        var mockValidator = Mocker.GetMock<IValidator<ApprovalsSearchModel>>();
        mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ApprovalsSearchModel>(), default))
            .ReturnsAsync(new ValidationResult()); // Valid
    }
}