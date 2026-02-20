using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.Portal.Application.Constants;
using Rsp.Portal.Web.Features.Modifications.ParticipatingOrganisations.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Features.Modifications.ParticipatingOrganisations;

public class SearchOrganisationTests : TestServiceBase<ParticipatingOrganisationsController>
{
    [Fact]
    public async Task SearchOrganisation_Post_ReturnsView_WithValidationErrors()
    {
        // Arrange
        var model = new SearchOrganisationViewModel
        {
            Search = new OrganisationSearchModel
            {
                SearchNameTerm = "ab"
            }
        };

        Mocker
            .GetMock<IValidator<SearchOrganisationViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SearchOrganisationViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([new ValidationFailure(nameof(model.Search.SearchNameTerm), "Provide 3 or more characters to search")]));

        // Act
        var result = await Sut.SearchOrganisation(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("SearchOrganisation");

        var returnedModel = viewResult.Model.ShouldBeOfType<SearchOrganisationViewModel>();
        returnedModel.Search.SearchNameTerm.ShouldBe("ab");

        Sut.ModelState.ContainsKey(nameof(model.Search.SearchNameTerm)).ShouldBeTrue();
        Sut.ModelState[nameof(model.Search.SearchNameTerm)]!.Errors.ShouldContain(e => e.ErrorMessage == "Provide 3 or more characters to search");
    }

    [Fact]
    public void SaveSelection_WithSaveForLaterTrue_And_ReviseAndAuthorise_RedirectsToSwsModifications()
    {
        // Arrange
        var http = new DefaultHttpContext();
        var sponsorId = Guid.NewGuid();
        Sut.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = "PRJ-123",
            [TempDataKeys.ProjectModification.ProjectModificationStatus] = ModificationStatus.ReviseAndAuthorise,
            [TempDataKeys.RevisionSponsorOrganisationUserId] = sponsorId
        };

        // Act
        var result = Sut.SaveSelection(saveForLater: true);

        // Assert
        // TempData flags
        Sut.TempData[TempDataKeys.ShowNotificationBanner].ShouldBe(true);
        Sut.TempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker].ShouldNotBeNull();
        Sut.TempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker].ShouldBeOfType<Guid>();

        // Route
        var redirect = result.ShouldBeOfType<RedirectToRouteResult>();
        redirect.RouteName.ShouldBe("sws:modifications");
        redirect.RouteValues.ShouldNotBeNull();
        redirect.RouteValues!["sponsorOrganisationUserId"].ShouldBe(sponsorId);
    }

    [Fact]
    public void SaveSelection_WithSaveForLaterTrue_RedirectsToPostApproval_AndSetsTempDataFlags()
    {
        // Arrange
        const string projectRecordId = "PRJ-123";
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = projectRecordId
            // No status in TempData -> not ReviseAndAuthorise
        };

        // Act
        var result = Sut.SaveSelection(saveForLater: true);

        // Assert
        Sut.TempData[TempDataKeys.ShowNotificationBanner].ShouldBe(true);
        Sut.TempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker].ShouldNotBeNull();
        Sut.TempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker].ShouldBeOfType<Guid>();

        var redirectResult = result.ShouldBeOfType<RedirectToRouteResult>();
        redirectResult.RouteName.ShouldBe("pov:postapproval");
        redirectResult.RouteValues.ShouldNotBeNull();
        redirectResult.RouteValues!["projectRecordId"].ShouldBe(projectRecordId);

        // Ensure sponsor key not present on post-approval path
        redirectResult.RouteValues.ContainsKey("sponsorOrganisationUserId").ShouldBeFalse();
    }

    [Fact]
    public void SaveSelection_WithSaveForLaterFalse_RedirectsToParticipatingOrganisation()
    {
        const string projectRecordId = "PRJ-123";
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = projectRecordId
        };

        // Act
        var result = Sut.SaveSelection(saveForLater: false);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToActionResult>();
        redirectResult.ActionName.ShouldBe("ParticipatingOrganisation");
    }

    [Fact]
    public void SaveSelection_WithSaveForLaterTrue_SetsNotificationBannerAndChangeMarker()
    {
        // Arrange
        const string projectRecordId = "PRJ-123";
        var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = projectRecordId
        };

        Sut.TempData = tempData;

        // Act
        var result = Sut.SaveSelection(saveForLater: true);

        // Assert
        tempData[TempDataKeys.ShowNotificationBanner].ShouldBe(true);
        tempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker].ShouldNotBeNull();
        tempData[TempDataKeys.ProjectModification.ProjectModificationChangeMarker].ShouldBeOfType<Guid>();
    }
}