using FluentValidation.TestHelper;
using Rsp.IrasPortal.Web.Models;
using Rsp.IrasPortal.Web.Validators;

namespace Rsp.IrasPortal.UnitTests.Web.Validators.AddUpdateReviewBodyModelValidatorTests;

public class ValidateAsyncTests : TestServiceBase<AddUpdateReviewBodyModelValidator>
{
    [Fact]
    public async Task ShouldHaveValidationErrorForEmptyOrganisationName()
    {
        var model = new AddUpdateReviewBodyModel { OrganisationName = string.Empty };
        var result = await Sut.TestValidateAsync(model);
        result.ShouldHaveValidationErrorFor(x => x.OrganisationName)
            .WithErrorMessage("Enter the organisation name");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForEmptyEmailAddress()
    {
        var model = new AddUpdateReviewBodyModel { EmailAddress = string.Empty };
        var result = await Sut.TestValidateAsync(model);
        result.ShouldHaveValidationErrorFor(x => x.EmailAddress)
            .WithErrorMessage("Enter an email address");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForInvalidEmailAddress()
    {
        var model = new AddUpdateReviewBodyModel { EmailAddress = "invalid-email" };
        var result = await Sut.TestValidateAsync(model);
        result.ShouldHaveValidationErrorFor(x => x.EmailAddress)
            .WithErrorMessage("Enter a valid email address");
    }

    [Fact]
    public async Task ShouldNotHaveValidationErrorForValidEmailAddress()
    {
        var model = new AddUpdateReviewBodyModel { EmailAddress = "test@example.com" };
        var result = await Sut.TestValidateAsync(model);
        result.ShouldNotHaveValidationErrorFor(x => x.EmailAddress);
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForDescriptionExceeding250Words()
    {
        var model = new AddUpdateReviewBodyModel
        {
            Description = string.Join(" ", Enumerable.Repeat("word", 251))
        };
        var result = await Sut.TestValidateAsync(model);
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("The description cannot exceed 250 words.");
    }

    [Fact]
    public async Task ShouldNotHaveValidationErrorForDescriptionUnder250Words()
    {
        var model = new AddUpdateReviewBodyModel
        {
            Description = string.Join(" ", Enumerable.Repeat("word", 250))
        };
        var result = await Sut.TestValidateAsync(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForEmptyCountries()
    {
        var model = new AddUpdateReviewBodyModel { Countries = null };
        var result = await Sut.TestValidateAsync(model);
        result.ShouldHaveValidationErrorFor(x => x.Countries)
            .WithErrorMessage("Select at least one country.");
    }

    [Fact]
    public async Task ShouldNotHaveValidationErrorForNonEmptyCountries()
    {
        var model = new AddUpdateReviewBodyModel { Countries = new List<string>() { "England" } };
        var result = await Sut.TestValidateAsync(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Countries);
    }
}