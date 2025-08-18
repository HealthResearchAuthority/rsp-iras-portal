using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ApprovalsControllerTests;

public class RemoveFiltersTests : TestServiceBase<ApprovalsController>
{
    [Fact]
    public async Task RemoveFilter_ChiefInvestigatorName_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel { ChiefInvestigatorName = "Dr. X" };
        SetTempData(new Dictionary<string, object?>
        {
            { TempDataKeys.ApprovalsSearchModel, JsonSerializer.Serialize(model) }
        });
        SetupValidValidator();

        var result = await Sut.RemoveFilter("chiefinvestigatorname", null);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("Search");

        var updated =
            JsonSerializer.Deserialize<ApprovalsSearchModel>(
                Sut.TempData[TempDataKeys.ApprovalsSearchModel]!.ToString()!)!;
        updated.ChiefInvestigatorName.ShouldBeNull();
    }


    [Fact]
    public async Task RemoveFilter_ProjectTitle_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel { ShortProjectTitle = "Cancer Study" };
        SetTempData(new Dictionary<string, object?>
        {
            { TempDataKeys.ApprovalsSearchModel, JsonSerializer.Serialize(model) }
        });
        SetupValidValidator();

        var result = await Sut.RemoveFilter("shortprojecttitle", null);

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Search");

        var updated =
            JsonSerializer.Deserialize<ApprovalsSearchModel>(
                Sut.TempData[TempDataKeys.ApprovalsSearchModel]!.ToString()!)!;
        updated.ShortProjectTitle.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_SponsorOrganisation_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel
        {
            SponsorOrganisation = "Org X",
            SponsorOrgSearch = new OrganisationSearchViewModel { SearchText = "Search" }
        };
        SetTempData(new Dictionary<string, object?>
        {
            { TempDataKeys.ApprovalsSearchModel, JsonSerializer.Serialize(model) }
        });
        SetupValidValidator();

        var result = await Sut.RemoveFilter("sponsororganisation", null);

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Search");

        var updated =
            JsonSerializer.Deserialize<ApprovalsSearchModel>(
                Sut.TempData[TempDataKeys.ApprovalsSearchModel]!.ToString()!)!;
        updated.SponsorOrganisation.ShouldBeNull();
        updated.SponsorOrgSearch.ShouldNotBeNull();
        updated.SponsorOrgSearch.SearchText.ShouldBeNull();
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

        var result = await Sut.RemoveFilter("datesubmitted-from", null);

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Search");

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

        var result = await Sut.RemoveFilter("datesubmitted-to", null);

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Search");

        var updated =
            JsonSerializer.Deserialize<ApprovalsSearchModel>(
                Sut.TempData[TempDataKeys.ApprovalsSearchModel]!.ToString()!)!;
        updated.ToDay.ShouldBeNull();
        updated.ToMonth.ShouldBeNull();
        updated.ToYear.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_FroAndTomDate_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel { FromDay = "01", FromMonth = "01", FromYear = "2023", ToDay = "31", ToMonth = "12", ToYear = "2026" };
        SetTempData(new Dictionary<string, object?>
        {
            { TempDataKeys.ApprovalsSearchModel, JsonSerializer.Serialize(model) }
        });
        SetupValidValidator();

        var result = await Sut.RemoveFilter("datesubmitted", null);

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Search");

        var updated =
            JsonSerializer.Deserialize<ApprovalsSearchModel>(
                Sut.TempData[TempDataKeys.ApprovalsSearchModel]!.ToString()!)!;
        updated.FromDay.ShouldBeNull();
        updated.FromMonth.ShouldBeNull();
        updated.FromYear.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_LeadNation_ShouldRemoveValue_AndRedirect()
    {
        var model = new ApprovalsSearchModel
        {
            LeadNation = new List<string> { "England", "Wales" }
        };
        SetTempData(new Dictionary<string, object?>
        {
            { TempDataKeys.ApprovalsSearchModel, JsonSerializer.Serialize(model) }
        });
        SetupValidValidator();

        var result = await Sut.RemoveFilter("leadnation", "Wales");

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Search");

        var updated =
            JsonSerializer.Deserialize<ApprovalsSearchModel>(
                Sut.TempData[TempDataKeys.ApprovalsSearchModel]!.ToString()!)!;
        updated.LeadNation.ShouldBe(new List<string> { "England" });
    }

    [Fact]
    public async Task RemoveFilter_ParticipatingNation_ShouldRemoveValue_AndRedirect()
    {
        var model = new ApprovalsSearchModel
        {
            ParticipatingNation = new List<string> { "England", "Wales" }
        };
        SetTempData(new Dictionary<string, object?>
        {
            { TempDataKeys.ApprovalsSearchModel, JsonSerializer.Serialize(model) }
        });
        SetupValidValidator();

        var result = await Sut.RemoveFilter("participatingnation", "Wales");

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Search");

        var updated =
            JsonSerializer.Deserialize<ApprovalsSearchModel>(
                Sut.TempData[TempDataKeys.ApprovalsSearchModel]!.ToString()!)!;
        updated.ParticipatingNation.ShouldBe(new List<string> { "England" });
    }

    [Fact]
    public async Task RemoveFilter_ModificationType_ShouldRemoveValue_AndRedirect()
    {
        var model = new ApprovalsSearchModel
        {
            ModificationTypes = new List<string> { "Type A", "Type B" }
        };
        SetTempData(new Dictionary<string, object?>
        {
            { TempDataKeys.ApprovalsSearchModel, JsonSerializer.Serialize(model) }
        });
        SetupValidValidator();

        var result = await Sut.RemoveFilter("modificationtype", "Type B");

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Search");

        var updated =
            JsonSerializer.Deserialize<ApprovalsSearchModel>(
                Sut.TempData[TempDataKeys.ApprovalsSearchModel]!.ToString()!)!;
        updated.ModificationTypes.ShouldBe(new List<string> { "Type A" });
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