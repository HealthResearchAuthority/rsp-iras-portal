﻿using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Rsp.IrasPortal.Application.Constants;
using Rsp.IrasPortal.Web.Features.Modifications.ParticipatingOrganisations.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Features.Modifications.ParticipatingOrganisations;

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
    public void SaveSelection_WithSaveForLaterTrue_RedirectsToPostApproval()
    {
        // Arrange
        const string projectRecordId = "PRJ-123";
        Sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        {
            [TempDataKeys.ProjectRecordId] = projectRecordId
        };

        // Act
        var result = Sut.SaveSelection(saveForLater: true);

        // Assert
        var redirectResult = result.ShouldBeOfType<RedirectToRouteResult>();
        redirectResult.RouteName.ShouldBe("pov:postapproval");
        redirectResult.RouteValues.ShouldNotBeNull();
        redirectResult.RouteValues["projectRecordId"].ShouldBe(projectRecordId);
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
}