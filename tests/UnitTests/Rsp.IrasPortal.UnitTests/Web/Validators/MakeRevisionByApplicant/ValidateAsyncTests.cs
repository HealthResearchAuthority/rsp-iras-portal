using FluentValidation.TestHelper;
using Rsp.IrasPortal.Web.Validators;
using Rsp.Portal.UnitTests;
using Rsp.Portal.Web.Features.Modifications.Models;

namespace Rsp.IrasPortal.UnitTests.Web.Validators.MakeRevisionByApplicant;

public class ValidateAsyncTests : TestServiceBase<ModificationDetailsViewModelValidator>
{
    private readonly ModificationDetailsViewModelValidator _validator = new();

    private readonly ModificationDetailsViewModelValidator _sut
      = new ModificationDetailsViewModelValidator();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RevisionDescription_RequestRevisions_Required_Error(string value)
    {
        // Arrange
        var model = new ModificationDetailsViewModel { ApplicantRevisionResponse = value };

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.ApplicantRevisionResponse)
            .WithErrorMessage("Enter description of revisions made");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t   ")]
    public void Empty_Or_Whitespace_Is_Invalid_With_Required_Message(string value)
    {
        // Arrange
        var model = new ModificationDetailsViewModel { ApplicantRevisionResponse = value };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(m => m.ApplicantRevisionResponse)
              .WithErrorMessage("Enter description of revisions made");

        // Length rule should NOT be raised because length 0/whitespace <= 500
        // (and our first rule caught the issue already)
        // This assert ensures no duplicate unintended message shows up.
        Assert.DoesNotContain(result.Errors, e =>
            e.PropertyName == nameof(ModificationDetailsViewModel.ApplicantRevisionResponse) &&
            e.ErrorMessage.StartsWith("The description of revisions must be between 1 and"));
    }

    [Fact]
    public void Length_1_Is_Valid()
    {
        // Arrange
        var model = new ModificationDetailsViewModel { ApplicantRevisionResponse = "A" };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Length_500_Is_Valid()
    {
        // Arrange
        var text500 = new string('x', 500);
        var model = new ModificationDetailsViewModel { ApplicantRevisionResponse = text500 };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Length_501_Is_Invalid_With_Length_Message()
    {
        // Arrange
        var text501 = new string('x', 501);
        var model = new ModificationDetailsViewModel { ApplicantRevisionResponse = text501 };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(m => m.ApplicantRevisionResponse)
              .WithErrorMessage("The description of revisions must be between 1 and 500 characters");
    }

    [Theory]
    [InlineData("Some useful description")]
    [InlineData("   padded but not empty   ")]
    public void NonEmpty_NonNull_Under_Or_Equal_500_Is_Valid(string value)
    {
        // Arrange
        var model = new ModificationDetailsViewModel { ApplicantRevisionResponse = value };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}