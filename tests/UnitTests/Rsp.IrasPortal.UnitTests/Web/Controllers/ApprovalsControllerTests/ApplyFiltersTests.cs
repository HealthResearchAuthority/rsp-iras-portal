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

public class ApplyFiltersTests : TestServiceBase<ApprovalsController>
{
    [Fact]
    public async Task ApplyFilters_ShouldRedirectToSearch_WhenModelIsValid()
    {
        // Arrange
        var searchModel = new ApprovalsSearchModel { ChiefInvestigatorName = "Dr. Valid" };
        var viewModel = new ApprovalsSearchViewModel { Search = searchModel };

        SetupValidatorResult(new ValidationResult()); // Valid

        var httpContext = new DefaultHttpContext();
        Sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        // Act
        var result = await Sut.ApplyFilters(viewModel);

        // Assert
        var redirect = result.ShouldBeOfType<RedirectToActionResult>();
        redirect.ActionName.ShouldBe("Search");

        var storedJson = Sut.TempData[TempDataKeys.ApprovalsSearchModel]!.ToString();
        var storedModel = JsonSerializer.Deserialize<ApprovalsSearchModel>(storedJson!);
        storedModel!.ChiefInvestigatorName.ShouldBe("Dr. Valid");
    }

    [Fact]
    public async Task ApplyFilters_ShouldReturnSearchView_WhenModelIsInvalid()
    {
        // Arrange
        var searchModel = new ApprovalsSearchModel { ChiefInvestigatorName = "Invalid" };
        var viewModel = new ApprovalsSearchViewModel { Search = searchModel };

        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure("ChiefInvestigatorName", "Required")
        });

        SetupValidatorResult(validationResult);

        // Act
        var result = await Sut.ApplyFilters(viewModel);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Search");
        viewResult.Model.ShouldBe(viewModel);

        Sut.ModelState.IsValid.ShouldBeFalse();
        Sut.ModelState["ChiefInvestigatorName"]!.Errors.ShouldContain(e => e.ErrorMessage == "Required");
    }

    [Fact]
    public async Task ApplyFilters_ShouldAddAllValidationErrors_WhenModelIsInvalid()
    {
        // Arrange
        var searchModel = new ApprovalsSearchModel { ChiefInvestigatorName = "Bad", FromYear = "not-a-year" };
        var viewModel = new ApprovalsSearchViewModel { Search = searchModel };

        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure("ChiefInvestigatorName", "Required"),
            new ValidationFailure("FromYear", "Must be numeric")
        });

        SetupValidatorResult(validationResult);

        // Act
        var result = await Sut.ApplyFilters(viewModel);

        // Assert
        var viewResult = result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("Search");

        Sut.ModelState.IsValid.ShouldBeFalse();
        Sut.ModelState["ChiefInvestigatorName"]!.Errors.Single().ErrorMessage.ShouldBe("Required");
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