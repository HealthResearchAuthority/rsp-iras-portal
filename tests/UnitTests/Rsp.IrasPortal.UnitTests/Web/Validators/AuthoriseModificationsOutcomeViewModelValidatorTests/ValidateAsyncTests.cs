using Rsp.Portal.Web.Features.SponsorWorkspace.Authorisation.Models;
using Rsp.Portal.Web.Validators;

namespace Rsp.Portal.UnitTests.Web.Validators.AuthoriseModificationsOutcomeViewModelValidatorTests;

public class ValidateAsyncTests : TestServiceBase<AuthoriseModificationsOutcomeViewModelValidator>
{
    private static AuthoriseModificationsOutcomeViewModel Vm(string? desc) =>
        new AuthoriseModificationsOutcomeViewModel { RevisionDescription = desc };

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_WhenRevisionDescription_IsNullOrEmptyOrWhitespace_ShouldHave_MandatoryError(string? value)
    {
        // Arrange
        var model = Vm(value);

        // Act
        var result = await Sut.ValidateAsync(model);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(AuthoriseModificationsOutcomeViewModel.RevisionDescription));
        result.Errors.ShouldContain(e => e.ErrorMessage == "Enter a description of revisions you want to request");
        result.Errors.ShouldNotContain(e => e.PropertyName == "_DescriptionExcessCharacterCount");
    }

    [Fact]
    public async Task ValidateAsync_WhenRevisionDescription_IsExactly1000Chars_ShouldBe_Valid()
    {
        // Arrange
        var exactly1000 = new string('x', 1000);
        var model = Vm(exactly1000);

        // Act
        var result = await Sut.ValidateAsync(model);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.Count.ShouldBe(0);
    }

    [Theory]
    [InlineData(1001, 1)]
    [InlineData(1100, 100)]
    [InlineData(1205, 205)]
    public async Task ValidateAsync_WhenRevisionDescription_ExceedsMax_ShouldHave_MaxLength_And_ExcessCount(int length, int expectedExcess)
    {
        // Arrange
        var tooLong = new string('x', length);
        var model = Vm(tooLong);

        // Act
        var result = await Sut.ValidateAsync(model);

        // Assert
        result.IsValid.ShouldBeFalse();

        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(AuthoriseModificationsOutcomeViewModel.RevisionDescription) &&
            e.ErrorMessage == "The description must be 1000 characters or less");

        var custom = result.Errors.Single(e => e.PropertyName == "_DescriptionExcessCharacterCount");
        custom.ErrorMessage.ShouldBe($"You have exceeded the characters limits by {expectedExcess}");
    }

    [Fact]
    public async Task ValidateAsync_WhenRevisionDescription_IsValidText_ShouldBe_Valid()
    {
        // Arrange
        var model = Vm("Please outline the revisions required in section 2 and 3.");

        // Act
        var result = await Sut.ValidateAsync(model);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }
}