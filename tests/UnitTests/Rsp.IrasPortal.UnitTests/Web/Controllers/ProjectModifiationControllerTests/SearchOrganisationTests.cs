using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ProjectModifiationControllerTests;

public class SearchOrganisationTests : TestServiceBase<ProjectModificationController>
{
    [Fact]
    public async Task SearchOrganisation_Post_ReturnsView_WithValidationErrors()
    {
        // Arrange
        var model = new SearchOrganisationViewModel
        {
            SearchTerm = ""
        };

        Mocker
            .GetMock<IValidator<SearchOrganisationViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SearchOrganisationViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(new[] { new ValidationFailure(nameof(model.SearchTerm), "Search term is required") }));

        // Act
        var result = await Sut.SearchOrganisation(model);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("SearchOrganisation");

        var returnedModel = viewResult.Model.ShouldBeOfType<SearchOrganisationViewModel>();
        returnedModel.SearchTerm.ShouldBe("");

        Sut.ModelState.ContainsKey(nameof(model.SearchTerm)).ShouldBeTrue();
        Sut.ModelState[nameof(model.SearchTerm)]!.Errors.ShouldContain(e => e.ErrorMessage == "Search term is required");
    }
}