using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rsp.IrasPortal.Web.Models;
using Rsp.IrasPortal.Web.Validators;

namespace Rsp.IrasPortal.UnitTests.Web.Validators.AreaOfChangeViewModelValidatorTests;

public class ValidateAsyncTests : TestServiceBase<AreaOfChangeViewModelValidator>
{
    [Fact]
    public async Task Should_Fail_When_AreaOfChangeIdIsNull()
    {
        // Arrange
        var model = new AreaOfChangeViewModel
        {
            AreaOfChangeId = null,
            SpecificChangeId = Guid.NewGuid().ToString(),
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AreaOfChangeId)
              .WithErrorMessage("Select area of change");
    }

    [Fact]
    public async Task Should_Fail_When_SpecificAreaOfChangeIdIsNull()
    {
        // Arrange
        var model = new AreaOfChangeViewModel
        {
            AreaOfChangeId = Guid.NewGuid().ToString(),
            SpecificChangeId = null
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SpecificChangeId)
              .WithErrorMessage("Select specific change");
    }

    [Fact]
    public async Task Should_Fail_When_SpecificChangeId_NotInOptions()
    {
        // Arrange
        var model = new AreaOfChangeViewModel
        {
            AreaOfChangeId = Guid.NewGuid().ToString(),
            SpecificChangeId = Guid.NewGuid().ToString(),
            SpecificChangeOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = Guid.NewGuid().ToString(), Text = "Option 1" }
            }
        };

        // Act
        var result = await Sut.TestValidateAsync(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("Select ‘Apply selection' to confirm the area of change, then select a specific change");
    }
}