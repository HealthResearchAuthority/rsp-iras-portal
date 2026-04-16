using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Web.Features.MemberManagement.ResearchEthicsCommittees.Controllers;
using Rsp.Portal.Web.Features.MemberManagement.ResearchEthicsCommittees.Models;

namespace Rsp.Portal.UnitTests.Web.Features.MemberManagement.ResearchEthicsCommittees;

public class ResearchEthicsCommitteesControllerTests : TestServiceBase<ResearchEthicsCommitteesController>
{
    private readonly DefaultHttpContext _http;

    public ResearchEthicsCommitteesControllerTests()
    {
        _http = new DefaultHttpContext { Session = new InMemorySession() };
        Sut.ControllerContext = new ControllerContext { HttpContext = _http };
    }

    [Fact]
    public async Task ResearchEthicsCommittees_ShouldReturnView_WithDefaultModel()
    {
        // Act
        var result = await Sut.ResearchEthicsCommittees();

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.Model.ShouldBeOfType<MemberManagementResearchEthicsCommitteesViewModel>();
    }

    [Theory, AutoData]
    public async Task SearchMyOrganisations_ShouldStoreSearchModelInSession_AndRedirectToResearchEthicsCommittees(
        MemberManagementResearchEthicsCommitteesViewModel model,
        string sortField,
        string sortDirection)
    {
        // Arrange
        model.Search ??= new MemberManagementResearchEthicsCommitteesSearchModel();

        // Act
        var result = await Sut.SearchMyOrganisations(model, sortField, sortDirection);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(ResearchEthicsCommitteesController.ResearchEthicsCommittees));
        redirectResult.RouteValues!["sortField"].ShouldBe(sortField);
        redirectResult.RouteValues["sortDirection"].ShouldBe(sortDirection);

        var sessionValue = _http.Session.GetString(SessionKeys.MemberManagementResearchEthicsCommitteesSearch);
        sessionValue.ShouldNotBeNull();

        var storedModel = JsonSerializer.Deserialize<MemberManagementResearchEthicsCommitteesSearchModel>(sessionValue!);
        storedModel.ShouldNotBeNull();
        storedModel.ShouldBeEquivalentTo(model.Search);
    }

    [Fact]
    public async Task SearchMyOrganisations_ShouldStoreEmptySearchModelInSession_WhenSearchIsNull()
    {
        // Arrange
        var model = new MemberManagementResearchEthicsCommitteesViewModel
        {
            Search = null
        };

        // Act
        var result = await Sut.SearchMyOrganisations(model);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe(nameof(ResearchEthicsCommitteesController.ResearchEthicsCommittees));
        redirectResult.RouteValues!["sortField"].ShouldBe("ResearchEthicsCommitteeName");
        redirectResult.RouteValues["sortDirection"].ShouldBe("asc");

        var sessionValue = _http.Session.GetString(SessionKeys.MemberManagementResearchEthicsCommitteesSearch);
        sessionValue.ShouldNotBeNull();

        var storedModel = JsonSerializer.Deserialize<MemberManagementResearchEthicsCommitteesSearchModel>(sessionValue!);
        storedModel.ShouldNotBeNull();
        storedModel.ShouldBeOfType<MemberManagementResearchEthicsCommitteesSearchModel>();
    }
}