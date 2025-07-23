using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Rsp.IrasPortal.Web.Controllers;
using Rsp.IrasPortal.Web.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Controllers.ModificationsTasklistControllerTests;

public class ApplyFiltersTests : TestServiceBase<ModificationsTasklistController>
{
    [Fact]
    public async Task ApplyFilters_ShouldAddAllValidationErrors_WhenModelIsInvalid()
    {
        // Arrange
        var searchModel = new ApprovalsSearchModel { FromYear = "not-a-year" };
        var viewModel = new ModificationsTasklistViewModel { Search = searchModel };

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
        var mockValidator = Mocker.GetMock<IValidator<ApprovalsSearchModel>>();
        mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ApprovalsSearchModel>(), default))
            .ReturnsAsync(result);
    }
}