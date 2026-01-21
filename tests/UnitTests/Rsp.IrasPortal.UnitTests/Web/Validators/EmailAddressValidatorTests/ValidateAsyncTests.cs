using System.Net.Mail;
using FluentValidation.TestHelper;
using Rsp.IrasPortal.Web.Models;
using Rsp.IrasPortal.Web.Validators;

namespace Rsp.Portal.UnitTests.Web.Validators.EmailAddressValidatorTests;

public class ValidateAsyncTests : TestServiceBase<EmailAddressValidator>
{
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
        var emailModel = new EmailModel()
        {
            EmailAddress = email
        };

        var result = await Sut.TestValidateAsync(emailModel);
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
        var emailModel = new EmailModel()
        {
            EmailAddress = email
        };

        var result = await Sut.TestValidateAsync(emailModel);
        result.ShouldNotHaveValidationErrorFor(x => x.EmailAddress);
    }
}