using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.ApprovalsControllerTests;

public class RemoveFiltersTests : TestServiceBase<ApprovalsController>
{
    private readonly DefaultHttpContext _http;

    public RemoveFiltersTests()
    {
        _http = new DefaultHttpContext
        {
            Session = new InMemorySession()
        };

        Sut.ControllerContext = new ControllerContext
        {
            HttpContext = _http
        };

        // Default: validator succeeds unless overridden per-test
        SetupValidValidator();
    }

    [Fact]
    public async Task RemoveFilter_ChiefInvestigatorName_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel { ChiefInvestigatorName = "Dr. X" };
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("chiefinvestigatorname", null);

        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("Index");

        var updated = GetSessionModel();
        updated.ChiefInvestigatorName.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_ProjectTitle_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel { ShortProjectTitle = "Cancer Study" };
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("shortprojecttitle", null);

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Index");

        var updated = GetSessionModel();
        updated.ShortProjectTitle.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_SponsorOrganisation_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel
        {
            SponsorOrganisation = "Org X",
            SponsorOrgSearch = new OrganisationSearchViewModel { SearchText = "Index" }
        };
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("sponsororganisation", null);

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Index");

        var updated = GetSessionModel();
        updated.SponsorOrganisation.ShouldBeNull();
        updated.SponsorOrgSearch.ShouldNotBeNull();
        updated.SponsorOrgSearch.SearchText.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_FromDate_ShouldBeCleared_AndRedirect()
    {
        var model = new ApprovalsSearchModel { FromDay = "01", FromMonth = "01", FromYear = "2023" };
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("datesubmitted-from", null);

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

        var result = await Sut.RemoveFilter("datesubmitted-to", null);

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Index");

        var updated = GetSessionModel();
        updated.ToDay.ShouldBeNull();
        updated.ToMonth.ShouldBeNull();
        updated.ToYear.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_FroAndTomDate_ShouldBeCleared_AndRedirect()
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

        var result = await Sut.RemoveFilter("datesubmitted", null);

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Index");

        var updated = GetSessionModel();
        updated.FromDay.ShouldBeNull();
        updated.FromMonth.ShouldBeNull();
        updated.FromYear.ShouldBeNull();

        // (Controller clears both ranges for 'datesubmitted')
        updated.ToDay.ShouldBeNull();
        updated.ToMonth.ShouldBeNull();
        updated.ToYear.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveFilter_LeadNation_ShouldRemoveValue_AndRedirect()
    {
        var model = new ApprovalsSearchModel
        {
            LeadNation = new List<string> { "England", "Wales" }
        };
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("leadnation", "Wales");

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Index");

        var updated = GetSessionModel();
        updated.LeadNation.ShouldBe(new List<string> { "England" });
    }

    [Fact]
    public async Task RemoveFilter_ParticipatingNation_ShouldRemoveValue_AndRedirect()
    {
        var model = new ApprovalsSearchModel
        {
            ParticipatingNation = new List<string> { "England", "Wales" }
        };
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("participatingnation", "Wales");

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Index");

        var updated = GetSessionModel();
        updated.ParticipatingNation.ShouldBe(new List<string> { "England" });
    }

    [Fact]
    public async Task RemoveFilter_ModificationType_ShouldRemoveValue_AndRedirect()
    {
        var model = new ApprovalsSearchModel
        {
            ModificationTypes = new List<string> { "Type A", "Type B" }
        };
        SetSessionModel(model);

        var result = await Sut.RemoveFilter("modificationtype", "Type B");

        result.ShouldBeOfType<RedirectToActionResult>().ActionName.ShouldBe("Index");

        var updated = GetSessionModel();
        updated.ModificationTypes.ShouldBe(new List<string> { "Type A" });
    }

    // ----------------------
    // Helpers
    // ----------------------

    private void SetSessionModel(ApprovalsSearchModel model)
    {
        _http.Session.SetString(SessionKeys.ApprovalsSearch, JsonSerializer.Serialize(model));
    }

    private ApprovalsSearchModel GetSessionModel()
    {
        var json = _http.Session.GetString(SessionKeys.ApprovalsSearch);
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