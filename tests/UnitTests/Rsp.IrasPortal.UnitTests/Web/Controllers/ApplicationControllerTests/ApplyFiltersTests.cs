using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Rsp.Portal.Web.Controllers;
using Rsp.Portal.Web.Models;

namespace Rsp.Portal.UnitTests.Web.Controllers.ApplicationControllerTests;

public class ApplyFiltersTests : TestServiceBase<ApplicationController>
{
    [Fact]
    public async Task ApplyFilters_ShouldAddAllValidationErrors_WhenModelIsInvalid()
    {
        // Arrange
        var searchModel = new ApplicationSearchModel { FromYear = "not-a-year" };
        var viewModel = new ApplicationsViewModel { Search = searchModel };

        var validationResult = new ValidationResult(
        [
            new ValidationFailure("FromYear", "Must be numeric")
        ]);

        SetupValidatorResult(validationResult);

        // Act
        var result = await Sut.ApplyFilters(viewModel);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Index");

        Sut.ModelState.IsValid.ShouldBeFalse();
        Sut.ModelState["FromYear"]!.Errors.Single().ErrorMessage.ShouldBe("Must be numeric");
    }

    private void SetupValidatorResult(ValidationResult result)
    {
        var mockValidator = Mocker.GetMock<IValidator<ApplicationSearchModel>>();
        mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ApplicationSearchModel>(), default))
            .ReturnsAsync(result);
    }
}