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
            Search = new OrganisationSearchModel
            {
                SearchNameTerm = "ab"
            }
        };

        Mocker
            .GetMock<IValidator<SearchOrganisationViewModel>>()
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SearchOrganisationViewModel>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(new[] { new ValidationFailure(nameof(model.Search.SearchNameTerm), "Provide 3 or more characters to search") }));

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
}