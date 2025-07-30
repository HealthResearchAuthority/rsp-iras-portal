using FluentValidation.TestHelper;
using Rsp.IrasPortal.Web.Models;
using Rsp.IrasPortal.Web.Validators;

namespace Rsp.IrasPortal.UnitTests.Web.Validators.AddUpdateReviewBodyModelValidatorTests;

public class ValidateAsyncTests : TestServiceBase<AddUpdateReviewBodyModelValidator>
{
    [Fact]
    public async Task ShouldHaveValidationErrorForEmptyOrganisationName()
    {
        var model = new AddUpdateReviewBodyModel { RegulatoryBodyName = string.Empty };
        var result = await Sut.TestValidateAsync(model);
        result.ShouldHaveValidationErrorFor(x => x.RegulatoryBodyName)
            .WithErrorMessage("Enter an organisation name");
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForEmptyEmailAddress()
    {
        var model = new AddUpdateReviewBodyModel { EmailAddress = string.Empty };
        var result = await Sut.TestValidateAsync(model);
        result.ShouldHaveValidationErrorFor(x => x.EmailAddress)
            .WithErrorMessage("Enter an email address");
    }

    [Theory]
    //[InlineData(".john@example.com")] // NOT WORKING WITH RFC STANDARD 5322
    [InlineData("john.@example.com")]
    [InlineData("john..doe@example.com ")]
    [InlineData("john doe@example.com")]
    [InlineData("john[at]example.com")]
    [InlineData("john<doe>@example.com")]
    [InlineData("john:doe@example.com")]
    [InlineData("john;doe@example.com")]
    [InlineData("john,@example.com")]
    //[InlineData("-john@example.com")] // NOT WORKING WITH RFC STANDARD 5322
    //[InlineData("john@example-.com")] // NOT WORKING WITH RFC STANDARD 5322
    [InlineData("john.@example..com")]
    [InlineData("john!doe@exa!mple.com")]
    [InlineData("\"john.doe\"@example.com")]
    [InlineData(" john.doe@example.com ")]
    [InlineData("averyveryverylongdomainnamewithmanysubdomainsandmorecharacters.com ")]
    [InlineData("john@example..com")]
    [InlineData("mail..sub.example.com")]
    [InlineData("john..doe\"@example..com")]
    [InlineData("john\ud83d\ude42@example.com")]
    [InlineData("john.doe@example.1234")]
    [InlineData("john.doeexample.com")]
    [InlineData("john.doe@localhost")]
    //[InlineData("MoreThan64CharactersForTheLocalAddressBeforeTheAtSymbolInAnEmailA@example.com")] // NOT WORKING WITH RFC 5322 STANDARD
    [InlineData(
        "MoreThan320CharactersForTheWholeEmailAddressonetwothreefourfive@MoreThan320CharactersForTheWholeEmailAddressonetwothreefourfiveMoreThan320CharactersForTheWholeEmailAddressonetwothreefourfiveMoreThan320CharactersForTheWholeEmailAddressonetwothreefourfivesixs.MoreThan320CharactersForTheWholeEmailAddressonetwothreefourfive")]
    public async Task ShouldHaveValidationErrorForInvalidEmailAddress(string email)
    {
        var model = new AddUpdateReviewBodyModel { EmailAddress = email };
        var result = await Sut.TestValidateAsync(model);
        result.ShouldHaveValidationErrorFor(x => x.EmailAddress)
            .WithErrorMessage("Enter an email address in the correct format");
    }

    [Theory]
    //[InlineData("john.doe@example.com ")] // space at end of email address // NOT WORKING WITH RFC 5322 STANDARD
    [InlineData("john.doe@example.com")]
    [InlineData("john_doe@example.com")]
    [InlineData("john-doe@example.com")]
    [InlineData("john+newsletter@example.com")]
    [InlineData("john!doe@example.com")]
    [InlineData("john#profile@example.com")]
    [InlineData("john$invoice@example.com")]
    [InlineData("john%discount@example.com")]
    [InlineData("john&team@example.com")]
    [InlineData("john's@example.com")]
    [InlineData("john*star@example.com")]
    [InlineData("john/everyone@example.com")]
    [InlineData("john=equals@example.com")]
    [InlineData("john?query@example.com")]
    [InlineData("john^caret@example.com")]
    [InlineData("john`code@example.com")]
    [InlineData("john{editor}@example.com")]
    [InlineData("john|vertical@example.com")]
    [InlineData("john~tilde@example.com")]
    [InlineData("johñ.unicode@example.com")]
    [InlineData("john!#$%&'*+-/=?^_`{|}~doe@example.com")] // uses special characters allowed in the local part
    [InlineData("john-_doe@example.com")] // combination of valid special characters
    [InlineData("johñ.döe@example.com")] // Unicode characters are valid if supported by the system
    [InlineData("test@mail.sub.subsub.sub.example.com")] // nested subdomains within valid limits
    [InlineData("john.doe@example.photography")] // unusual but valid TLD
    //[InlineData("john@example.भारत")] // Unicode domain supported if the system allows // NOT WORKING WITH RFC 5322 STANDARD
    [InlineData("Johndoe2@example.com")] // number in domain
    [InlineData("johñ@example.com")]
    [InlineData("john@example.مثال")]
    public async Task ShouldNotHaveValidationErrorForValidEmailAddress(string email)
    {
        var model = new AddUpdateReviewBodyModel { EmailAddress = email };
        var result = await Sut.TestValidateAsync(model);
        result.ShouldNotHaveValidationErrorFor(x => x.EmailAddress);
    }

    [Fact]
    public async Task ShouldHaveValidationErrorForDescriptionExceeding250Words()
    {
        var model = new AddUpdateReviewBodyModel
        {
            Description = string.Join(" ", Enumerable.Repeat("word", 501))
        };
        var result = await Sut.TestValidateAsync(model);
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("The description must be 250 words or less");
    }

    [Fact]
    public async Task ShouldNotHaveValidationErrorForDescription500OrUnderWords()
    {
        var model = new AddUpdateReviewBodyModel
        {
            Description = string.Join(" ", Enumerable.Repeat("word", 250))
        };
        var result = await Sut.TestValidateAsync(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public async Task ShouldNotHaveValidationErrorForEmptyDescription()
    {
        var model = new AddUpdateReviewBodyModel
        {
            Description = string.Empty
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
            .WithErrorMessage("Enter a country");
    }

    [Fact]
    public async Task ShouldNotHaveValidationErrorForNonEmptyCountries()
    {
        var model = new AddUpdateReviewBodyModel { Countries = new List<string> { "England" } };
        var result = await Sut.TestValidateAsync(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Countries);
    }
}